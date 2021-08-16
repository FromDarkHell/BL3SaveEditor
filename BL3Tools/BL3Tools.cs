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

        public static UE3Save LoadFileFromDisk(string filePath, bool bBackup = true) {
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
                        ProfileBogoCrypt.Decrypt(buffer, 0, remainingData);
                        saveGame = new BL3Profile(saveData, Serializer.Deserialize<Profile>(new MemoryStream(buffer)));
                        break;
                    // Decrypt a save game
                    case "OakSaveGame":
                        SaveBogoCrypt.Decrypt(buffer, 0, remainingData);
                        saveGame = new BL3Save(saveData, Serializer.Deserialize<Character>(new MemoryStream(buffer)));
                        break;
                    default:
                        throw new BL3Exceptions.InvalidSaveException(saveGameType);
                }

            }
            finally {
                // Close the buffer
                io.Close();
            }
            saveGame.filePath = filePath;
            return saveGame;
        }
    
        public static bool WriteFileToDisk(UE3Save saveGame) {
            return WriteFileToDisk(saveGame.filePath, saveGame);
        }

        public static bool WriteFileToDisk(string filePath, UE3Save ue3Save) {
            Console.WriteLine("Writing file to disk...");
            FileStream fs = new FileStream(filePath, FileMode.Create);
            IOWrapper io = new IOWrapper(fs, Endian.Little, 0x0000000);
            try {
                Helpers.WriteGVASSave(io, ue3Save.GVASData);
                byte[] result;

                Console.WriteLine("Writing profile of type: {0}", ue3Save.GVASData.sgType);

                using (var stream = new MemoryStream()) {
                    switch (ue3Save.GVASData.sgType) {
                        case "BP_DefaultOakProfile_C":
                            // This is probably a little bit unsafe and costly but *ehh*?
                            BL3Profile vx = (BL3Profile)ue3Save;

                            vx.Profile.BankInventoryLists.Clear();
                            // Add back all the items onto the bank
                            vx.Profile.BankInventoryLists.AddRange(vx.BankItems.Select(x => x.EncryptSerialToBytes()));

                            Serializer.Serialize<Profile>(stream, vx.Profile);
                            result = stream.ToArray();
                            ProfileBogoCrypt.Encrypt(result, 0, result.Length);
                            break;
                        case "OakSaveGame":
                            BL3Save save = (BL3Save)ue3Save;
                            // Unlike the profiles, we can't just remove all of the data from the save's inventory and then readd it
                            // Saves store other data in the items as well so we can't do that
                            foreach(Borderlands3Serial serial in save.InventoryItems) {
                                // If we don't have "original data", just simply the item
                                if (serial.OriginalData == null) {
                                    save.Character.InventoryItems.Add(new OakInventoryItemSaveGameData() {
                                        DevelopmentSaveData = null,
                                        Flags = 0x00,
                                        PickupOrderIndex = 8008,
                                        WeaponSkinPath = "",
                                        ItemSerialNumber = serial.EncryptSerialToBytes()
                                    });
                                }
                                else {
                                    foreach(OakInventoryItemSaveGameData item in save.Character.InventoryItems) {
                                        if (item.ItemSerialNumber != serial.OriginalData) continue;
                                        
                                        item.ItemSerialNumber = serial.EncryptSerialToBytes();
                                    }
                                }
                            }


                            Serializer.Serialize<Character>(stream, save.Character);
                            result = stream.ToArray();
                            SaveBogoCrypt.Encrypt(result, 0, result.Length);
                            break;
                        default:
                            throw new BL3Exceptions.InvalidSaveException(ue3Save.GVASData.sgType);
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
