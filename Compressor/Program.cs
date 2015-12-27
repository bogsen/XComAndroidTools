using Ionic.Zlib;
using System;
using System.IO;

namespace Compressor {
    class Program {

        #region Configuration

        static readonly uint Magic = 0x9E2A83C1;
        static readonly int MaxBlockSize = 131072;

        #endregion

        static bool IsCompressed(Stream stream) {
            long pos = stream.Position;
            BinaryReader reader = new BinaryReader(stream);
            bool result = false;
            try {
                result  = reader.ReadUInt32() == Magic;
                result &= reader.ReadUInt32() == MaxBlockSize;
            } catch (EndOfStreamException) {
            }
            stream.Position = pos;
            return result;
        }

        static bool IsCompressed(string path) {
            using (Stream stream = File.OpenRead(path)) {
                return IsCompressed(stream);
            }
        }

        static void Decompress(string path) {
            string tmpPath = path + ".tmp";
            File.Delete(tmpPath);
            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            using (Stream output = File.OpenWrite(tmpPath)) {
                while (IsCompressed(reader.BaseStream)) {
                    reader.ReadUInt32(); // magic
                    int blockSize = reader.ReadInt32();
                    int compressedSize = reader.ReadInt32();
                    int uncompressedSize = reader.ReadInt32();
                    int numBlocks = (uncompressedSize + blockSize - 1) / blockSize;
                    int[] sizes = new int[numBlocks * 2];
                    for (int i = 0; i < sizes.Length; i++)
                        sizes[i] = reader.ReadInt32();
                    int maxIdxLength = numBlocks.ToString().Length;
                    int sumBlocks = 0;
                    for (int i = 0; i < numBlocks; i++) {
                        Console.Write("\r[{0," + maxIdxLength + "}/" + numBlocks + "] Decompressing block... (" + sizes[i * 2] + " bytes to " + sizes[i * 2 + 1] + " bytes)", i);
                        byte[] buffer = new byte[sizes[i * 2]];
                        reader.Read(buffer, 0, sizes[i * 2]);
                        using (ZlibStream stream = new ZlibStream(new MemoryStream(buffer, 0, sizes[i * 2]), CompressionMode.Decompress)) {
                            stream.CopyTo(output);
                        }
                        sumBlocks += sizes[i * 2 + 1];
                    }
                    Console.WriteLine("\r[" + numBlocks + "/" + numBlocks + "] Decompressed " + numBlocks + " blocks: " + compressedSize + " bytes to " + uncompressedSize + " bytes");
                }
            }
            string sizePath = path + ".uncompressed_size";
            File.Delete(sizePath);
            File.Delete(path);
            File.Move(tmpPath, path);
        }

        static void Compress(string path) {
            byte[] buffer = new byte[MaxBlockSize];
            string tmpPath = path + ".tmp";
            File.Delete(tmpPath);
            string sizePath = path + ".uncompressed_size";
            File.WriteAllText(sizePath, new FileInfo(path).Length + "\n");
            using (BinaryReader input = new BinaryReader(File.OpenRead(path)))
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(tmpPath))) {
                writer.Write(Magic);
                writer.Write(MaxBlockSize);
                writer.Write(0);
                int size = (int)new FileInfo(path).Length;
                writer.Write(size);
                int numBlocks = (size + MaxBlockSize - 1) / MaxBlockSize;
                for (int i = 0; i < numBlocks * 2; i++)
                    writer.Write(0);
                int[] sizes = new int[numBlocks * 2];
                for (int i = 0; i < numBlocks - 1; i++)
                    sizes[i * 2 + 1] = MaxBlockSize;
                sizes[numBlocks * 2 - 1] = size % MaxBlockSize;
                writer.Flush();
                int compressedSize = 0;
                int uncompressedPos = 0;
                int maxIdxLength = numBlocks.ToString().Length;
                int maxPosLength = size.ToString().Length;
                for (int i = 0; i < numBlocks; i++) {
                    Console.Write("\r[{0," + maxPosLength + "}/" + size + "] Compressing block {1," + maxIdxLength + "}/" + numBlocks + "...", uncompressedPos, i);
                    input.Read(buffer, 0, sizes[i * 2 + 1]);
                    uncompressedPos += sizes[i * 2 + 1];
                    using (ZlibStream stream = new ZlibStream(new MemoryStream(buffer, 0, sizes[i * 2 + 1]), CompressionMode.Compress, CompressionLevel.BestCompression))
                    using (MemoryStream memory = new MemoryStream(MaxBlockSize)) {
                        stream.CopyTo(memory);
                        sizes[i * 2] = (int)memory.Position;
                        compressedSize += sizes[i * 2];
                        memory.Position = 0;
                        memory.SetLength(sizes[i * 2]);
                        memory.CopyTo(writer.BaseStream);
                    }
                }
                writer.Seek(8, SeekOrigin.Begin);
                writer.Write(compressedSize);
                writer.Seek(4, SeekOrigin.Current);
                for (int i = 0; i < sizes.Length; i++)
                    writer.Write(sizes[i]);
            }
            Console.WriteLine();
            File.Delete(path);
            File.Move(tmpPath, path);
        }

        static void Usage() {
            Console.WriteLine("TODO: Usage");
        }

        static void Main(string[] args) {
            if (args.Length != 1) {
                Usage();
            } else {
                if (IsCompressed(args[0])) {
                    Decompress(args[0]);
                } else {
                    Compress(args[0]);
                }
            }
        }
    }
}
