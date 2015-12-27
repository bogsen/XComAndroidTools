using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sewer {

    class PackedFile {
        public string Path;
        public long Offset;
        public int Length;
    }

    class Program {

        #region Configuration

        static readonly byte[] FileHeader = Encoding.ASCII.GetBytes("XComOBB");
        static readonly int PathUps = 1;
        static readonly char PathSeparator = '\\';

        #endregion

        static string ReadString(BinaryReader reader) {
            int len = reader.ReadInt32();
            string str = Encoding.ASCII.GetString(reader.ReadBytes(len - 1));
            reader.ReadByte();
            return str;
        }

        static void WriteString(BinaryWriter writer, string str) {
            writer.Write(str.Length + 1);
            writer.Write(Encoding.ASCII.GetBytes(str));
            writer.Write((byte)0);
        }

        static void CopyBytes(Stream input, Stream output, int length, int bufferSize = 81920) {
            byte[] buffer = new byte[bufferSize];
            int read;
            while (length > 0 && (read = input.Read(buffer, 0, Math.Min(bufferSize, length))) > 0) {
                output.Write(buffer, 0, read);
                length -= read;
            }
        }

        static List<string> GetFiles(string dir) {
            List<string> files = new List<string>();
            foreach (string subdir in Directory.GetDirectories(dir))
                files.AddRange(GetFiles(subdir));
            foreach (string file in Directory.GetFiles(dir))
                files.Add(file);
            return files;
        }

        static void Unpack(string path) {
            string outDirectory = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(path);
            Directory.CreateDirectory(outDirectory);
            using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(path))) {
                byte[] header = binaryReader.ReadBytes(FileHeader.Length);
                if (!header.SequenceEqual(FileHeader))
                    throw new NotSupportedException("Bad file header");
                int fileCount = binaryReader.ReadInt32();
                List<PackedFile> files = new List<PackedFile>(fileCount);
                for (int idxFile = 0; idxFile < fileCount; idxFile++) {
                    PackedFile file = new PackedFile();
                    file.Path = ReadString(binaryReader).Substring(PathUps * 3).Replace(PathSeparator, Path.DirectorySeparatorChar);
                    file.Offset = binaryReader.ReadInt64();
                    file.Length = binaryReader.ReadInt32();
                    files.Add(file);
                }
                files.Sort((x, y) => x.Offset.CompareTo(y.Offset));
                int maxIdxLength = fileCount.ToString().Length;
                for (int idxFile = 0; idxFile < fileCount; idxFile++) {
                    PackedFile file = files[idxFile];
                    Console.WriteLine("[{0," + maxIdxLength + "}/" + fileCount + "] " + file.Path, idxFile);
                    binaryReader.BaseStream.Seek(file.Offset, SeekOrigin.Begin);
                    string outPath = outDirectory + Path.DirectorySeparatorChar + file.Path;
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                    using (Stream output = File.OpenWrite(outPath)) {
                        CopyBytes(binaryReader.BaseStream, output, file.Length);
                    }
                }
            }
        }

        static void Repack(string dir) {
            dir = dir.TrimEnd(Path.DirectorySeparatorChar);
            string outPath = dir + ".obb";
            File.Delete(outPath);
            using (BinaryWriter binaryWriter = new BinaryWriter(File.OpenWrite(outPath))) {
                binaryWriter.Write(FileHeader);
                List<string> rawFiles = GetFiles(dir);
                List<Tuple<string, string, int>> files = new List<Tuple<string, string, int>>(rawFiles.Count);
                foreach (string file in rawFiles) {
                    string name = string.Join(PathSeparator.ToString(), Enumerable.Repeat("..", PathUps)) + file.Substring(dir.Length).Replace(Path.DirectorySeparatorChar, PathSeparator);
                    int size = (int)new FileInfo(file).Length;
                    files.Add(new Tuple<string, string, int>(file, name, size));
                }
                files.Sort((x, y) => -x.Item3.CompareTo(y.Item3));
                binaryWriter.Write(files.Count);
                long pos = FileHeader.Length + 4;
                foreach (Tuple<string, string, int> file in files)
                    pos += 4 + file.Item2.Length + 1 + 8 + 4;
                foreach (Tuple<string, string, int> file in files) {
                    WriteString(binaryWriter, file.Item2);
                    binaryWriter.Write(pos);
                    binaryWriter.Write(file.Item3);
                    pos += file.Item3;
                }
                binaryWriter.Flush();
                int maxIdxLength = files.Count.ToString().Length;
                for (int i = 0; i < files.Count; i++) {
                    Tuple<string, string, int> file = files[i];
                    Console.WriteLine("[{0," + maxIdxLength + "}/" + files.Count + "] " + file.Item2, i);
                    using (Stream input = File.OpenRead(file.Item1)) {
                        input.CopyTo(binaryWriter.BaseStream);
                    }
                }
            }
        }

        static void Usage() {
            Console.WriteLine("TODO: Usage");
        }

        static void Main(string[] args) {
            if (args.Length != 1) {
                Usage();
            } else {
                if (File.Exists(args[0])) {
                    Unpack(args[0]);
                } else if (Directory.Exists(args[0])) {
                    Repack(args[0]);
                }
            }
        }
    }
}
