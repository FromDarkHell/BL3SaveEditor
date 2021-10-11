/* Copyright (c) 2019 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */


using System;
using IOTools;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace BL3Tools.GameData.Items {

    // A few example serials for testing:
    // - BL3(AwAAAABF+IA81ECBEFSGhkMusVzZNu4QAgAAAAAAAAAA) :: AAA (Shock Dahl Pistol)
    // - BL3(AwAAAAD7u4A86PMBE4wakIcjwVhsgKJLUOHyoFJxhxAAAAAAAGCMAQ==) :: Skullmasher (Non-Elemental Mayhem 10 Jakobs Sniper)
    // - BL3(AwAAAADAeoC8y1QBE0QesjkdMfnY4w4hAAAAAAAAGRAA) :: Trickshot (Jakobs Pistol)
    // - BL3(AwAAAACxlIC1y1QBE0QesjkdMfnY444QAAAAAACADAg=)
    // - BL3(AwAAAAA4jIA5lMPiFgAAAA==) :: Customization
    // - BL3(AwAAAAChWIC5UhEHFgAAAA==) :: Customization

    /// <summary>
    /// A class representing a Borderlands 3 Serial Item
    /// </summary>
    public class Borderlands3Serial {
        public byte SerialVersion { get; set; } = 0x03;
        public uint Seed { get; set; } = 0x00;
        public ushort Checksum { get; set; } = 0x00;
        public int SerialDatabaseVersion { get; set; } = (int)InventorySerialDatabase.MaximumVersion;
        public string Balance { get; set; } = "";
        public string ShortNameBalance { get; set; } = "";
        public string UserFriendlyName { get; set; } = "";
        public int Level { get; set; } = PlayerXP._XPMinimumLevel;
        public string InventoryKey { get; set; } = "";
        public string InventoryData { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public List<string> Parts { get; set; } = new List<string>();
        public List<string> GenericParts { get; set; } = new List<string>();
        public List<int> UnkData1 { get; set; } = new List<int>();
        public int AmountRerolled { get; set; } = 0;

        /// <summary>
        /// Used when loading a serial from a save/profile; Do not touch this!!
        /// This data is automatically set when loaded from a save
        /// </summary>
        public OakSave.OakInventoryItemSaveGameData OriginalData { get; set; } = null;

        public string EncryptSerial(uint seed = 0) {
            byte[] data = EncryptSerialToBytes(seed);
            return $"BL3({Convert.ToBase64String(data)})";
        }

        /// <summary>
        /// Encodes a given serial item into the specified format
        /// </summary>
        /// <param name="seed">A seed for which to do the encryption; defaults to 0, be nice to each other instead (:</param>
        /// <returns>A BL3(...) encoded string of the <c>Borderlands3Serial</c></returns>
        public byte[] EncryptSerialToBytes(uint seed = 0) {

            // Avoid deleting items that we don't know how to parse....
            if (InventoryKey == null && OriginalData != null) return OriginalData.ItemSerialNumber;

            ValidateFields();

            byte[] header = Helpers.ConcatArrays(
                new byte[] { SerialVersion },
                Converters.UInt32ToBytes(seed, true)
            );

            BitWriter writer = new BitWriter();
            writer.WriteInt32(128, 8);
            writer.WriteInt32(SerialDatabaseVersion, 7);

            writer.WriteInt32(InventorySerialDatabase.GetIndexByPart("InventoryBalanceData", Balance),
                InventorySerialDatabase.GetBitsToEat("InventoryBalanceData", SerialDatabaseVersion));
            writer.WriteInt32(InventorySerialDatabase.GetIndexByPart("InventoryData", InventoryData),
                InventorySerialDatabase.GetBitsToEat("InventoryData", SerialDatabaseVersion));
            writer.WriteInt32(InventorySerialDatabase.GetIndexByPart("ManufacturerData", Manufacturer),
                InventorySerialDatabase.GetBitsToEat("ManufacturerData", SerialDatabaseVersion));
            writer.WriteInt32(Level, 7);

            writer.WriteInt32(Parts.Count, 6);
            int PartBits = InventorySerialDatabase.GetBitsToEat(InventoryKey, SerialDatabaseVersion);
            foreach (string Part in Parts) {
                int index = InventorySerialDatabase.GetIndexByPart(InventoryKey, Part);
                if (index == -1) throw new BL3Tools.BL3Exceptions.SerialParseException();

                writer.WriteInt32(index, PartBits);
            }


            writer.WriteInt32(GenericParts.Count, 4);
            int GenericBits = InventorySerialDatabase.GetBitsToEat("InventoryGenericPartData", SerialDatabaseVersion);
            foreach (string Generic in GenericParts) {
                int index = InventorySerialDatabase.GetIndexByPart("InventoryGenericPartData", Generic);
                if (index == -1) throw new BL3Tools.BL3Exceptions.SerialParseException();

                writer.WriteInt32(index, GenericBits);
            }

            writer.WriteInt32(UnkData1.Count, 8);
            foreach (int val in UnkData1) writer.WriteInt32(val, 8);

            // This writes out the number of customization data; It's forced to be zero for now (:
            writer.WriteInt32(0, 4);

            // For consideration is just removing this check here for code compatability.
            if (SerialVersion >= 4) writer.WriteInt32(AmountRerolled, 8);

            byte[] buffer = writer.GetBuffer();

            // Calculate
            byte[] checksumBuffer = Helpers.ConcatArrays(header, new byte[] { 0xFF, 0xFF }, buffer);
            var CRC = new IOTools.Algorithms.CRC32();
            CRC.Compute(checksumBuffer);
            var hash = CRC.GetHashUInt32;
            var computedChecksum = (ushort)(((hash) >> 16) ^ ((hash & 0xFFFF) >> 0));

            // Slap the checksum onto the start of the buffer data for encryption.
            buffer = Helpers.ConcatArrays(Converters.UInt16ToBytes(computedChecksum, true), buffer);

            // Encrypt the data
            BogoEncrypt(seed, buffer, 0, buffer.Length);
            
            // Slap the serial version & seed onto the start
            buffer = Helpers.ConcatArrays(header, buffer);

            return buffer;
        }

        /// <summary>
        /// Decodes a BL3(...) encoded item serial into a <c>Borderlands3Serial</c> object
        /// </summary>
        /// <param name="serial">BL3(...) encoded serial</param>
        /// <returns>An object representing the passed in <paramref name="serial"/></returns>
        public static Borderlands3Serial DecryptSerial(string serial) {
            if (serial.ToLower().StartsWith("bl3(") && serial.EndsWith(")"))
                serial = serial.Remove(0, 4).Remove(serial.Length - 5);
            return DecryptSerial(Convert.FromBase64String(serial));
        }

        /// <summary>
        /// Decodes a binary (byte[]) item serial into a <c>Borderlands3Serial</c> object
        /// </summary>
        /// <param name="serial">A byte array of the given serial</param>
        /// <returns>An object representing the passed in <paramref name="serial"/></returns>
        public static Borderlands3Serial DecryptSerial(byte[] serial) {
            byte serialVersion = serial[0];

            // Some simple checks for version numbers and such
            if (serialVersion != 3 && serialVersion != 4) {
                throw new BL3Tools.BL3Exceptions.SerialParseException(Convert.ToBase64String(serial), serialVersion);
            }

            IOWrapper serialIO = new IOWrapper(serial, Endian.Big, 1);
            uint originalSeed = serialIO.ReadUInt32();
            
            // Copy the decrypted serial into a new buffer
            int decryptedSize = (int)(serial.Length - serialIO.Position);
            byte[] decrypted = new byte[decryptedSize];
            Array.Copy(serial, serialIO.Position, decrypted, 0, decryptedSize);

            // Now we decrypt the array
            BogoDecrypt(originalSeed, decrypted, 0, decryptedSize);

            IOWrapper decryptedSerialIO = new IOWrapper(decrypted, Endian.Big, 0);
            
            // Read in the original checksum
            ushort originalChecksum = decryptedSerialIO.ReadUInt16();

            // Flip this data around because we're initially reading in Big endian
            // Note that this intentionally skips over the original checksum, it will be set to FF FF when calculating the checksum later.
            byte[] remaining = decryptedSerialIO.ReadToEnd().Reverse().ToArray();

            byte[] checksumBuffer = Helpers.ConcatArrays(new byte[] {
                serial[0], // Serial Version
                serial[1], serial[2], serial[3], serial[4], // Seed 
                0xFF, 0xFF // When calculating, the preset checksum is 0xFF
            }, remaining); // Append the remaining (unencrypted) data

            decryptedSerialIO.JumpBack();

            var CRC = new IOTools.Algorithms.CRC32();
            CRC.Compute(checksumBuffer);
            var hash = CRC.GetHashUInt32;
            var computedChecksum = (ushort)(((hash) >> 16) ^ ((hash & 0xFFFF) >> 0));

            if(computedChecksum != originalChecksum)
                throw new BL3Tools.BL3Exceptions.SerialParseException(Convert.ToBase64String(serial), serialVersion, originalChecksum, computedChecksum);


            BitReader reader = new BitReader(decryptedSerialIO);

            int randomData = reader.ReadInt32(8);

            // It might not explicitly matter that if this data isn't 128, so for now it stays as a Debug assertion.
            Debug.Assert(randomData == 128);


            int SerialDatabaseVersion = reader.ReadInt32(7);
            if (SerialDatabaseVersion > InventorySerialDatabase.MaximumVersion)
                throw new BL3Tools.BL3Exceptions.SerialParseException(Convert.ToBase64String(serial), serialVersion, SerialDatabaseVersion);

            string balance = EatBitsForCategory(reader, "InventoryBalanceData", SerialDatabaseVersion);
            string inventoryData = EatBitsForCategory(reader, "InventoryData", SerialDatabaseVersion);
            string manufacturer = EatBitsForCategory(reader, "ManufacturerData", SerialDatabaseVersion);
            int level = reader.ReadInt32(7);

            string shortBalance = balance.Split('.').LastOrDefault();
            string inventoryKey = InventoryKeyDB.GetKeyForBalance(balance);

            int amountRerolled = 0;
            List<string> parts = new List<string>();
            List<string> genericParts = new List<string>();
            List<int> additionalData = new List<int>();


            if (inventoryKey != null) {
                // Get the main actual parts on it
                parts = EatBitArrayForCategory(reader, inventoryKey, SerialDatabaseVersion, 6);

                // Both Anointments & Mayhem mode are currently stored in InventoryGenericPartData
                genericParts = EatBitArrayForCategory(reader, "InventoryGenericPartData", SerialDatabaseVersion, 4);
                
                // Some other stuff is apparently probably in here, no idea what or why (:
                int additionalCount = reader.ReadInt32(8);
                additionalData = new List<int>();
                for (int i = 0; i < additionalCount; i++) 
                    additionalData.Add(reader.ReadInt32(8));

                try {
                    // The ""customization"" parts should be fully zero.
                    if (reader.ReadInt32(4) != 0)
                        throw new BL3Tools.BL3Exceptions.SerialParseException(Convert.ToBase64String(serial), serialVersion, SerialDatabaseVersion, "Customization parts included...?");

                    // If the serial version is >4, we've got rerolled data
                    if (serialVersion >= 4)
                        amountRerolled = reader.ReadInt32(8);

                    if (reader.BitsRemaining() > 7)
                        throw new BL3Tools.BL3Exceptions.SerialParseException(Convert.ToBase64String(serial), serialVersion, SerialDatabaseVersion, "Zero-Padding incorrect");
                }
                catch(Exception ex) when (!(ex is BL3Tools.BL3Exceptions.SerialParseException)) {
                    // The data in this isn't *too* important, so as long as it's not a serial parse exception, we can still continue on parsing it.
                }
            }

            // Customizations don't have parts so we do this;
            // For note, you can also use InventoryNameDatabase.GetCustomizationNameForBalance but in this case it will give a faster result if we do this.
            var nameParts = parts.Select(x => (string)x.Clone()).ToList();
            nameParts.Add(balance);
            string userFriendlyName = InventoryNameDatabase.GetNameForParts(nameParts);
            
            // This means we've got some sort of item that we don't know entirely have info on...
            if (string.IsNullOrEmpty(userFriendlyName)) userFriendlyName = shortBalance;

            Borderlands3Serial serialObject = new Borderlands3Serial() {
                SerialVersion = serialVersion,
                Seed = originalSeed,
                Checksum = originalChecksum,
                SerialDatabaseVersion = SerialDatabaseVersion,
                Balance = balance,
                InventoryData = inventoryData,
                Manufacturer = manufacturer,
                Level = level,
                ShortNameBalance = shortBalance,
                UserFriendlyName = userFriendlyName,
                InventoryKey = inventoryKey,
                AmountRerolled = amountRerolled,
                Parts = parts,
                GenericParts = genericParts,
                UnkData1 = additionalData,
                OriginalData = null
            };

            return serialObject;
        }

        /// <summary>
        /// Creates a <c>Borderlands3Serial</c> object based off of the balance data passed in
        /// <para>Use this function when you want to create a new item</para>
        /// </summary>
        /// <param name="balance">Balance of the item to create; can be long or short</param>
        /// <returns>A Borderlands3Serial object representing the balance; completely partless etc</returns>
        public static Borderlands3Serial CreateSerialFromBalanceData(string balance) {
            string longBalance = InventorySerialDatabase.GetBalanceFromShortName(balance);
            if (longBalance == null) longBalance = balance;

            string shortBalance = InventorySerialDatabase.GetShortNameFromBalance(longBalance);
            string inventoryKey = InventoryKeyDB.GetKeyForBalance(longBalance);
            string userFriendlyName = (string)shortBalance.Clone();

            Borderlands3Serial serial = new Borderlands3Serial() {
                Balance = balance,
                ShortNameBalance = shortBalance,
                InventoryKey = inventoryKey,
                UserFriendlyName = userFriendlyName
            };

            return serial;
        }

        private static List<string> EatBitArrayForCategory(BitReader reader, string category, int version, int bitsToEat) {
            int numBitsToEat = InventorySerialDatabase.GetBitsToEat(category, version);
            var parts = new List<string>();

            int numParts = reader.ReadInt32(bitsToEat);
            for(int i = 0; i < numParts; i++) {
                int partIndex = reader.ReadInt32(numBitsToEat);
                string partValue = InventorySerialDatabase.GetPartByIndex(category, partIndex);
                if (partValue == null) parts.Add("<UNKNOWN>");
                else parts.Add(partValue);
            }

            return parts;
        }
        private static string EatBitsForCategory(BitReader reader, string category, int version) {
            int numBitsToEat = InventorySerialDatabase.GetBitsToEat(category, version);
            int partIndex = reader.ReadInt32(numBitsToEat);
            string partValue = InventorySerialDatabase.GetPartByIndex(category, partIndex);

            return partValue;
        }

        public void ValidateFields() {
            string fullName = InventorySerialDatabase.GetBalanceFromShortName(this.Balance);
            if (fullName != null) 
                this.Balance = fullName;

            string invData = InventorySerialDatabase.GetInventoryDatas(false).FirstOrDefault(x => x.EndsWith(InventoryData));
            if (!string.IsNullOrEmpty(invData)) 
                this.InventoryData = invData;
            
            string manu = InventorySerialDatabase.GetManufacturers(false).FirstOrDefault(x => x.EndsWith(Manufacturer));
            if (!string.IsNullOrEmpty(manu))
                this.Manufacturer = manu;
            this.InventoryKey = InventoryKeyDB.GetKeyForBalance(Balance);

            if(InventoryKey != null) {
                var validParts = InventorySerialDatabase.GetPartsForInvKey(InventoryKey);
                this.Parts.RemoveAll(x => !validParts.Contains(x));

                var validGenerics = InventorySerialDatabase.GetPartsForInvKey("InventoryGenericPartData");
                this.GenericParts.RemoveAll(x => !GenericParts.Contains(x));
            }

        }

        // Credits to Rick/Gibbed for the BogoEncrypt functions from their BL2 save editor:
        // https://github.com/gibbed/Gibbed.Borderlands2/blob/master/projects/Gibbed.Borderlands2.FileFormats/Items/PackedDataHelper.cs#L116
        private static void BogoEncrypt(uint seed, byte[] buffer, int offset, int length) {
            if (seed == 0) return;

            var temp = new byte[length];

            var rightHalf = (int)((seed % 32) % length);
            var leftHalf = length - rightHalf;

            Array.Copy(buffer, offset, temp, leftHalf, rightHalf);
            Array.Copy(buffer, offset + rightHalf, temp, 0, leftHalf);

            var xor = (uint)((int)seed >> 5);
            for (int i = 0; i < length; i++) {
                xor = (uint)(((ulong)xor * 0x10A860C1UL) % 0xFFFFFFFBUL);
                temp[i] ^= (byte)(xor & 0xFF);
            }

            Array.Copy(temp, 0, buffer, offset, length);
        }
        private static void BogoDecrypt(uint seed, byte[] buffer, int offset, int length) {
            if (seed == 0) return;

            var temp = new byte[length];
            Array.Copy(buffer, offset, temp, 0, length);

            var xor = (uint)((int)seed >> 5);
            for (int i = 0; i < length; i++) {
                xor = (uint)(((ulong)xor * 0x10A860C1UL) % 0xFFFFFFFBUL);
                temp[i] ^= (byte)(xor & 0xFF);
            }

            var rightHalf = (int)((seed % 32) % length);
            var leftHalf = length - rightHalf;

            Array.Copy(temp, leftHalf, buffer, offset, rightHalf);
            Array.Copy(temp, 0, buffer, offset + rightHalf, leftHalf);
        }
    }
}
