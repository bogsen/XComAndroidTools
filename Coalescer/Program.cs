using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Coalescer {
    class Program {

        #region Configuration

        static readonly int PathUps = 2;
        static readonly char PathSeparator = '\\';

        #endregion

        static string ReadString(BinaryReader reader) {
            int len = -reader.ReadInt32();
            string str = "";
            if (len > 0) {
                str = Encoding.Unicode.GetString(reader.ReadBytes((len - 1) * 2));
                reader.ReadBytes(2);
            }
            return str;
        }

        static void WriteString(BinaryWriter writer, string str) {
            if (str.Length > 0) {
                writer.Write(-(str.Length + 1));
                writer.Write(Encoding.Unicode.GetBytes(str));
                writer.Write((short) 0);
            } else {
                writer.Write(0);
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

        static string EscapeIni(string str) {
            return str.Replace('\n', '¶');
        }

        static string UnescapeIni(string str) {
            return str.Replace('¶', '\n');
        }

        static void Unpack(string path) {
            string outDirectory = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(path);
            using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(path))) {
                int numFiles = binaryReader.ReadInt32();
                Directory.CreateDirectory(outDirectory);
                for (int idxFile = 0; idxFile < numFiles; idxFile++) {
                    string filePath = outDirectory + Path.DirectorySeparatorChar + ReadString(binaryReader).Substring(PathUps * 3).Replace(PathSeparator, Path.DirectorySeparatorChar);
                    Console.WriteLine(filePath);
                    int secCount = binaryReader.ReadInt32();
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    using (StreamWriter output = new StreamWriter(File.OpenWrite(filePath))) {
                        for (int idxSec = 0; idxSec < secCount; idxSec++) {
                            string sectionName = ReadString(binaryReader);
                            int recCount = binaryReader.ReadInt32();
                            output.WriteLine("[" + EscapeIni(sectionName) + "]");
                            for (int idxRec = 0; idxRec < recCount; idxRec++) {
                                string key = ReadString(binaryReader);
                                string value = ReadString(binaryReader);
                                output.WriteLine(EscapeIni(key) + "=" + EscapeIni(value));
                            }
                            output.WriteLine();
                        }
                    }
                }
            }
        }

        static void Repack(string dir) {
            dir = dir.TrimEnd(Path.DirectorySeparatorChar);
            string outPath = dir + ".bin";
            File.Delete(outPath);
            using (BinaryWriter binaryWriter = new BinaryWriter(File.OpenWrite(outPath))) {
                List<string> files = GetFiles(dir);
                binaryWriter.Write(files.Count);
                foreach (string path in files) {
                    Console.WriteLine(path);
                    WriteString(binaryWriter, string.Join(PathSeparator.ToString(), Enumerable.Repeat("..", PathUps)) + path.Substring(dir.Length).Replace(Path.DirectorySeparatorChar, PathSeparator));
                    List<KeyValuePair<string, List<KeyValuePair<string, string>>>> ini = new List<KeyValuePair<string, List<KeyValuePair<string, string>>>>();
                    int idx = -1;
                    foreach (string line in File.ReadLines(path)) {
                        if (line.StartsWith("[") && line.EndsWith("]")) {
                            idx = ini.Count;
                            string name = UnescapeIni(line.Substring(1, line.Length - 2));
                            ini.Add(new KeyValuePair<string, List<KeyValuePair<string, string>>>(name, new List<KeyValuePair<string, string>>()));
                        } else if (line.Contains('=') && !line.StartsWith(";")) {
                            int eqPos = line.IndexOf('=');
                            string key = UnescapeIni(line.Substring(0, eqPos));
                            string value = UnescapeIni(line.Substring(eqPos + 1, line.Length - eqPos - 1));
                            ini[idx].Value.Add(new KeyValuePair<string, string>(key, value));
                        }
                    }
                    binaryWriter.Write(ini.Count);
                    foreach (KeyValuePair<string, List<KeyValuePair<string, string>>> section in ini) {
                        WriteString(binaryWriter, section.Key);
                        binaryWriter.Write(section.Value.Count);
                        foreach (KeyValuePair<string, string> item in section.Value) {
                            WriteString(binaryWriter, item.Key);
                            WriteString(binaryWriter, item.Value);
                        }
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
                } else {
                    Console.WriteLine("Path does not exist: " + args[0]);
                }
            }
        }
    }
}
