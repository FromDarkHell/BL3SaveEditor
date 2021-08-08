using System;
using System.IO;
using IOTools;
using ProtoBuf;
using BL3Tools.GVAS;
using BL3Tools.Decryption;
using OakSave;


namespace BL3Tools {

    public static class BL3Tools {
        public class BL3Exceptions {
            public class InvalidSaveException : Exception {
                public InvalidSaveException() : base("Invalid BL3 Save") { }
                public InvalidSaveException(string saveGameType) : base(String.Format("Invalid BL3 Save Game Type: {0}", saveGameType)) { }
            }


            public class SerialParseException : Exception {
                public SerialParseException() : base("Invalid BL3 Serial...") { }
                public SerialParseException(string serial) : base(String.Format("Invalid Serial: {0}", serial)) { }
                public SerialParseException(string serial, int version) : base(String.Format("Invalid Serial: \"{0}\"; Version: {1}", serial, version)) { }
                public SerialParseException(string serial, int version, uint originalChecksum, uint calculatedChecksum) : base(String.Format("Invalid Serial: \"{0}\"; Serial Version: {1}; Checksum Difference: {2} vs {3}", serial, version, originalChecksum, calculatedChecksum)) { }

                public SerialParseException(string serial, int version, int databaseVersion) : base(String.Format("Invalid Serial: \"{0}\"; Serial Version: {1}; Database Version: {2}", serial, version, databaseVersion)) { }

                public SerialParseException(string serial, int version, int databaseVersion, string oddity) : base(String.Format("Invalid Serial: \"{0}\"; Serial Version: {1}; Database Version: {2}; Error: {3}", serial, version, databaseVersion, oddity)) { }

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
                            Serializer.Serialize<Profile>(stream, ((BL3Profile)ue3Save).Profile);
                            result = stream.ToArray();
                            ProfileBogoCrypt.Encrypt(result, 0, result.Length);
                            break;
                        case "OakSaveGame":
                            Serializer.Serialize<Character>(stream, ((BL3Save)ue3Save).Character);
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
