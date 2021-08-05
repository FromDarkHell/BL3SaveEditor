using System;
using System.IO;
using PackageIO;
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

        }

        public static UE3Save LoadFileFromDisk(string filePath, bool bBackup = true) {
            UE3Save saveGame = null;
            Console.WriteLine("Reading new file: \"{0}\"", filePath);

            IO io = new IO(filePath, Endian.Little, 0x0000000, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            try {
                if (bBackup) {
                    // Gonna use this byte array for backing up the save file
                    byte[] originalBytes = io.ReadAll();
                    io.Close();
                    // Backup the file
                    File.WriteAllBytes(filePath + ".bak", originalBytes);

                    // Restore the IO object back
                    io = new IO(filePath, Endian.Little, 0x0000000, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
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
            IO io = new IO(filePath, Endian.Little, 0x0000000, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
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
