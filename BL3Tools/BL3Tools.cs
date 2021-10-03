using System;
using System.IO;
using IOTools;
using ProtoBuf;
using BL3Tools.GVAS;
using BL3Tools.Decryption;
using OakSave;
using System.Linq;
using BL3Tools.GameData.Items;

namespace BL3Tools {

    public static class BL3Tools {
        public class BL3Exceptions {
            public class InvalidSaveException : Exception {
                public InvalidSaveException() : base("Invalid BL3 Save") { }
                public InvalidSaveException(string saveGameType) : base(String.Format("Invalid BL3 Save Game Type: {0}", saveGameType)) { }
                public InvalidSaveException(Platform platform) : base(String.Format("Incorrectly decrypted save game using the {0} platform; Are you sure you're using the right one?", platform)) { }
            }


            public class SerialParseException : Exception {
                public bool knowCause = false;

                public SerialParseException() : base("Invalid BL3 Serial...") { }
                public SerialParseException(string serial) : base(String.Format("Invalid Serial: {0}", serial)) { }
                public SerialParseException(string serial, int version) : base(String.Format("Invalid Serial: \"{0}\"; Version: {1}", serial, version)) { knowCause = true; }
                public SerialParseException(string serial, int version, uint originalChecksum, uint calculatedChecksum) : base(String.Format("Invalid Serial: \"{0}\"; Serial Version: {1}; Checksum Difference: {2} vs {3}", serial, version, originalChecksum, calculatedChecksum)) { knowCause = true; }

                public SerialParseException(string serial, int version, int databaseVersion) : base(String.Format("Invalid Serial: \"{0}\"; Serial Version: {1}; Database Version: {2}", serial, version, databaseVersion)) { knowCause = true;  }

                public SerialParseException(string serial, int version, int databaseVersion, string oddity) : base(String.Format("Invalid Serial: \"{0}\"; Serial Version: {1}; Database Version: {2}; Error: {3}", serial, version, databaseVersion, oddity)) { knowCause = true; }

            }
        }

        /// <summary>
        /// This function writes a <c>UE3Save</c> instance to the drive, deserializes it to the respective classes of <c>BL3Profile</c> or <c>BL3Save</c>
        /// </summary>
        /// <param name="filePath">A file path for which to load the file from</param>
        /// <param name="bBackup">Whether or not to backup the save on reading (Default: False)</param>
        /// <returns>An instance of the respective type, all subclassed by a <c>UE3Save</c> instance</returns>
        public static UE3Save LoadFileFromDisk(string filePath, Platform platform = Platform.PC, bool bBackup = false) {
            UE3Save saveGame = null;
            Console.WriteLine("Reading new file: \"{0}\"", filePath);
            FileStream fs = new FileStream(filePath, FileMode.Open);

            IOWrapper io = new IOWrapper(fs, Endian.Little, 0x0000000);
            try {
                if (bBackup) {
                    // Gonna use this byte array for backing up the save file
                    byte[] originalBytes = io.ReadAll();
                    io.Seek(0);

                    // Backup the file
                    File.WriteAllBytes(filePath + ".bak", originalBytes);
                }

                GVASSave saveData = Helpers.ReadGVASSave(io);

                // Throw an exception if the save is null somehow
                if(saveData == null) {
                    throw new BL3Exceptions.InvalidSaveException();
                }

                // Read in the save data itself now
                string saveGameType = saveData.sgType;
                int remainingData = io.ReadInt32();
                Console.WriteLine("Length of data: {0}", remainingData);
                byte[] buffer = io.ReadBytes(remainingData);

                switch(saveGameType) {
                    // Decrypt a profile
                    case "BP_DefaultOakProfile_C":
                        ProfileBogoCrypt.Decrypt(buffer, 0, remainingData, platform);
                        saveGame = new BL3Profile(saveData, Serializer.Deserialize<Profile>(new MemoryStream(buffer)));
                        (saveGame as BL3Profile).Platform = platform;
                        break;
                    // Decrypt a save game
                    case "OakSaveGame":
                        SaveBogoCrypt.Decrypt(buffer, 0, remainingData, platform);
                        saveGame = new BL3Save(saveData, Serializer.Deserialize<Character>(new MemoryStream(buffer)));
                        (saveGame as BL3Save).Platform = platform;
                        break;
                    default:
                        throw new BL3Exceptions.InvalidSaveException(saveGameType);
                }
            }
            catch(ProtoBuf.ProtoException ex) {
                // Typically this exception means that the user didn't properly give in the platform for their save
                if(ex.Message.StartsWith("Invalid wire-type (7);")) {
                    throw new BL3Exceptions.InvalidSaveException(platform);
                }
                
                // Raise all other exceptions
                throw ex;
            }
            finally {
                // Close the buffer
                io.Close();
            }
            saveGame.filePath = filePath;
            return saveGame;
        }
    
        /// <summary>
        /// Writes a <c>UE3Save</c> instance to disk, serializing it to the respective protobuf type.
        /// </summary>
        /// <param name="saveGame">An instance of a UE3Save for which to write out</param>
        /// <param name="bBackup">Whether or not to backup on writing (Default: True)</param>
        /// <returns>Whether or not the file writing succeeded</returns>
        public static bool WriteFileToDisk(UE3Save saveGame, bool bBackup = true) {
            return WriteFileToDisk(saveGame.filePath, saveGame, bBackup);
        }

        /// <summary>
        /// Writes a <c>UE3Save</c> instance to disk, serializing it to the respective protobuf type.
        /// </summary>
        /// <param name="filePath">Filepath for which to write the <paramref name="saveGame"/> out to</param>
        /// <param name="saveGame">An instance of a UE3Save for which to write out</param>
        /// <param name="bBackup">Whether or not to backup on writing (Default: True)</param>
        /// <returns>Whether or not the file writing succeeded</returns>

        public static bool WriteFileToDisk(string filePath, UE3Save saveGame, bool bBackup = true) {
            Console.WriteLine("Writing file to disk...");
            FileStream fs = new FileStream(filePath, FileMode.Create);
            IOWrapper io = new IOWrapper(fs, Endian.Little, 0x0000000);
            try {
                Helpers.WriteGVASSave(io, saveGame.GVASData);
                byte[] result;

                Console.WriteLine("Writing profile of type: {0}", saveGame.GVASData.sgType);

                using (var stream = new MemoryStream()) {
                    switch (saveGame.GVASData.sgType) {
                        case "BP_DefaultOakProfile_C":
                            // This is probably a little bit unsafe and costly but *ehh*?
                            BL3Profile vx = (BL3Profile)saveGame;

                            vx.Profile.BankInventoryLists.Clear();
                            vx.Profile.BankInventoryLists.AddRange(vx.BankItems.Select(x => x.EncryptSerialToBytes()));

                            vx.Profile.LostLootInventoryLists.Clear();
                            vx.Profile.LostLootInventoryLists.AddRange(vx.LostLootItems.Select(x => x.EncryptSerialToBytes()));

                            Serializer.Serialize(stream, vx.Profile);
                            result = stream.ToArray();
                            ProfileBogoCrypt.Encrypt(result, 0, result.Length, vx.Platform);
                            break;
                        case "OakSaveGame":
                            BL3Save save = (BL3Save)saveGame;
                            // Now we've got to update the underlying protobuf data's serial...
                            foreach(Borderlands3Serial serial in save.InventoryItems) {
                                var protobufItem = save.Character.InventoryItems.FirstOrDefault(x => ReferenceEquals(x, serial.OriginalData));
                                if(protobufItem == default) {
                                    throw new BL3Exceptions.SerialParseException(serial.EncryptSerial(), serial.SerialVersion, serial.SerialDatabaseVersion);
                                }
                                protobufItem.ItemSerialNumber = serial.EncryptSerialToBytes();
                            }

                            Serializer.Serialize(stream, save.Character);
                            result = stream.ToArray();
                            SaveBogoCrypt.Encrypt(result, 0, result.Length, save.Platform);
                            break;
                        default:
                            throw new BL3Exceptions.InvalidSaveException(saveGame.GVASData.sgType);
                    }
                }

                io.WriteInt32(result.Length);
                io.WriteBytes(result);
            }
            finally {
                io.Close();
            }

            Console.WriteLine("Completed writing file...");
            return true;
        }
    }

}
