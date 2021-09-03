using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using IOTools;

// A large portion of this code is based off of apocalyptech's OakTMS unpack:
// https://github.com/apocalyptech/pyoaktms

namespace BL3Tools.TMSUnpack {

    /// <summary>
    /// A class that represents an OakTMS file and the contents of it.
    /// </summary>
    public class TMSArchive {

        public struct TMSChunk {
            public ulong CompressedSize { get; set; }
            public ulong DecompressedSize { get; set; }
            public byte[] DecompressedData { get; set; }
            public byte[] CompressedData { get; set; }
        }

        public struct TMSFile {
            public string FileName { get; set; }
            public byte[] Contents { get; set; }
        }

        public string FileName { get; private set; } = null;
        public List<TMSChunk> Chunks { get; private set; } = new List<TMSChunk>();
        public List<string> FooterStrings { get; private set; } = new List<string>();
        public uint[] FooterNumbers { get; private set; } = new uint[2];
        public List<TMSFile> Files { get; private set; } = new List<TMSFile>();

        public TMSArchive(byte[] data, string name = "OakTMS.cfg") {
            FileName = name;
            Process(data);
        }

        public void Process(byte[] data) {
            IOWrapper helper = new IOWrapper(data, Endian.Little);
            Process(helper);
        }

        public void Process(IOWrapper wrapper) {
            if (wrapper.CurrentEndian != Endian.Little) 
                wrapper.CurrentEndian = Endian.Little;

            long totalSize = wrapper.Length;

            uint uncompressedSize = wrapper.ReadUInt32();
            uint fileCount = wrapper.ReadUInt32();

            ulong magic = wrapper.ReadUInt64();
            if (magic != 0x9E2A83C1) throw new Exception("Invalid magic for TMS file...");

            ulong chunkSize = wrapper.ReadUInt64();

            ulong totalCompressedSize = wrapper.ReadUInt64();
            ulong totalUncompressedSize = wrapper.ReadUInt64();
            if (uncompressedSize != totalUncompressedSize) throw new Exception("Varying decompressed sizes...");

            ulong currentCompressedSize = 0;
            ulong currentUncompressedSize = 0;

            while(true) {
                ulong chunkCompressedSize = wrapper.ReadUInt64();
                ulong chunkUncompressedSize = wrapper.ReadUInt64();

                currentCompressedSize += chunkCompressedSize;
                currentUncompressedSize += chunkUncompressedSize;

                Console.WriteLine("Got chunk, Compressed Size: {0}, Uncompressed: {1}", chunkCompressedSize, chunkUncompressedSize);
                Chunks.Add(new TMSChunk {
                    CompressedSize = chunkCompressedSize,
                    DecompressedSize = chunkUncompressedSize,
                    DecompressedData = null,
                    CompressedData = null
                });

                if(currentCompressedSize == totalCompressedSize) {
                    if (currentUncompressedSize != totalUncompressedSize) throw new Exception("Varying decompressed sizes...");
                    break;
                }
            }

            for(int i = 0; i < Chunks.Count; i++) {
                TMSChunk chunk = Chunks[i];
                
                //! BAD DECISION HERE
                chunk.CompressedData = wrapper.ReadBytes((int)chunk.CompressedSize);
                chunk.DecompressedData = new byte[chunk.DecompressedSize + 1];
                using(MemoryStream ms = new MemoryStream(chunk.CompressedData))
                using(InflaterInputStream inflater = new InflaterInputStream(ms)) {
                    int sizeRead = inflater.Read(chunk.DecompressedData, 0, (int)chunk.DecompressedSize);
                    var x = chunk.DecompressedData;
                    Array.Resize(ref x, sizeRead);
                    chunk.DecompressedData = x;
                }

                Chunks[i] = chunk;
            }

            // Remove invalid chunks just to be safe
            Chunks.RemoveAll(x => x.DecompressedData == null);
            ulong decompressedSize = (ulong)Chunks.Sum(x => x.DecompressedData.Length);

            // Make sure that we did decompression properly.
            if (decompressedSize != totalUncompressedSize) 
                throw new Exception("Incorrect decompression sizes...");

            // Read in some footer info here now that we've read in everything else...
            uint numStrs = wrapper.ReadUInt32();
            for(int i = 0; i < numStrs; i++) {
                uint stringLength = wrapper.ReadUInt32() - 1;
                var strBytes = wrapper.ReadBytes((int)stringLength);
                if (wrapper.ReadByte() != 0x00) throw new Exception("Incorrect string length, null byte not found...");
                string str = Encoding.UTF8.GetString(strBytes);
                FooterStrings.Add(str);
            }

            FooterNumbers[0] = wrapper.ReadUInt32();
            FooterNumbers[1] = wrapper.ReadUInt32();

            if (wrapper.Position != totalSize) throw new Exception("File larger than expected...");

            // Now process all of the decompressed data...

            // Join the data into one big byte array
            byte[] data = new byte[0];
            foreach(TMSChunk chunk in Chunks)
                data = Helpers.ConcatArrays(data, chunk.DecompressedData);

            IOWrapper decompData = new IOWrapper(data, Endian.Little);

            for (int i = 0; i < fileCount; i++) {
                uint stringLength = decompData.ReadUInt32() - 1;
                var strBytes = decompData.ReadBytes((int)stringLength);
                if (decompData.ReadByte() != 0x00) throw new Exception("Incorrect string length, null byte not found...");
                string fileName = Encoding.UTF8.GetString(strBytes);

                // Now we read the contents of the file
                int contentsLen = (int)decompData.ReadUInt32();
                byte[] contents = decompData.ReadBytes(contentsLen);
                fileName = fileName.TrimStart();
                while (fileName.StartsWith("../"))
                    fileName = fileName.Substring(3);

                Files.Add(new TMSFile {
                    FileName = fileName,
                    Contents = contents
                });
            }
        }
    }
}
