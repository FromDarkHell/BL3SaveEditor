using BL3Tools.GVAS;
using OakSave;
using IOTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BL3Tools {

    public static partial class Helpers {
        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        #region Reading/Writing UE Strings

        public static string ReadUEString(this IOWrapper io) {
            if (io.PeekChar() < 0) return null;
            int length = io.ReadInt32();
            if (length == 0) return null;
            if (length == 1) return "";
            var valueBytes = io.ReadBytes(length);
            return Utf8.GetString(valueBytes, 0, valueBytes.Length - 1);
        }

        public static void WriteUEString(this IOWrapper io, string str) {
            if (str == null) io.WriteInt32(0);
            else if (string.Empty.Equals(str)) io.WriteInt32(1);
            else {
                byte[] data = Utf8.GetBytes(str + '\0');
                io.WriteInt32(data.Length);
                io.WriteBytes(data);
            }
        }

        #endregion

        #region Reading/Writing GVAS Format

        public static GVASSave ReadGVASSave(IOWrapper io) {
            string header = io.ReadASCII(4);
            if (!header.Equals("GVAS")) return null;
            Console.WriteLine("Header: {0}", header);
            int sgVersion = io.ReadInt32();
            Console.WriteLine("Save Game Version: {0}", sgVersion);
            int pkgVersion = io.ReadInt32();
            Console.WriteLine("Package version: {0}", pkgVersion);
            short major = io.ReadInt16();
            short minor = io.ReadInt16();
            short patch = io.ReadInt16();
            uint engineBuild = io.ReadUInt32();
            Console.WriteLine("Engine version: {0}.{1}.{2}.{3}", major, minor, patch, engineBuild);

            string buildId = ReadUEString(io);
            Console.WriteLine("Build ID: {0}", buildId);

            int fmtVersion = io.ReadInt32();
            Console.WriteLine("Custom Format Version: {0}", fmtVersion);
            int fmtCount = io.ReadInt32();
            Console.WriteLine("Custom Format Data Count: {0}", fmtCount);
            Dictionary<byte[], int> keyValuePairs = new Dictionary<byte[], int>();
            for (int i = 0; i < fmtCount; i++) {
                byte[] guid = io.ReadBytes(16);
                int entry = io.ReadInt32();
                keyValuePairs.Add(guid, entry);
            }

            string sgType = ReadUEString(io);
            Console.WriteLine("Save Game Type: {0}", sgType);
            GVASSave saveData = new GVASSave(sgVersion, pkgVersion, major, minor, patch, engineBuild, buildId, fmtVersion, fmtCount, keyValuePairs, sgType);

            return saveData;
        }

        public static void WriteGVASSave(IOWrapper io, GVASSave saveData) {
            io.WriteASCII("GVAS");
            io.WriteInt32(saveData.sg);
            io.WriteInt32(saveData.pkg);
            io.WriteInt16(saveData.mj);
            io.WriteInt16(saveData.mn);
            io.WriteInt16(saveData.pa);
            io.WriteUInt32(saveData.eng);
            io.WriteUEString(saveData.build);
            io.WriteInt32(saveData.fmt);
            io.WriteInt32(saveData.fmtLength);
            foreach (KeyValuePair<byte[], int> entry in saveData.fmtData) {
                io.WriteBytes(entry.Key);
                io.WriteInt32(entry.Value);
            }
            io.WriteUEString(saveData.sgType);
        }
        #endregion

        public static string GetCharacterString(this Character value) {
            Dictionary<string, PlayerClassSaveGameData> validClasses = BL3Save.ValidClasses;
            PlayerClassSaveGameData val = value.PlayerClassData;
            string result = validClasses.Where(x => x.Value.PlayerClassPath.Equals(val.PlayerClassPath)).First().Key;
            return result;
        }

        public static T[] ConcatArrays<T>(params T[][] list) {
            var result = new T[list.Sum(a => a.Length)];
            int offset = 0;
            for (int x = 0; x < list.Length; x++) {
                list[x].CopyTo(result, offset);
                offset += list[x].Length;
            }
            return result;
        }

        public static T FromByteArray<T>(byte[] rawValue) {
            GCHandle handle = GCHandle.Alloc(rawValue, GCHandleType.Pinned);
            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return structure;
        }

        public static byte[] ToByteArray(object value, int maxLength) {
            int rawsize = Marshal.SizeOf(value);
            byte[] rawdata = new byte[rawsize];
            GCHandle handle =
                GCHandle.Alloc(rawdata,
                GCHandleType.Pinned);
            Marshal.StructureToPtr(value,
                handle.AddrOfPinnedObject(),
                false);
            handle.Free();
            if (maxLength < rawdata.Length) {
                byte[] temp = new byte[maxLength];
                Array.Copy(rawdata, temp, maxLength);
                return temp;
            }
            else {
                return rawdata;
            }
        }
    }

    public enum EndianMode {
        Little = 0x00,
        Big = 0x01
    }
}
