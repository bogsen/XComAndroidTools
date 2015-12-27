using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TicTOC {
    class Program {

        #region Configuration

        static readonly string TOCRoot = "XComGame";
        static readonly int TOCDepth = 1;
        static readonly char DirSeparator = '\\';

        #endregion

        static List<string> GetFiles(string dir) {
            List<string> files = new List<string>();
            foreach (string subdir in Directory.GetDirectories(dir))
                files.AddRange(GetFiles(subdir));
            foreach (string file in Directory.GetFiles(dir))
                files.Add(file);
            return files;
        }

        static void MakeTOC(string dir) {
            dir = dir.TrimEnd(Path.DirectorySeparatorChar);

            string flavor = null;
            foreach (string c in Directory.GetDirectories(dir + Path.DirectorySeparatorChar + TOCRoot)) {
                string name = Path.GetFileName(c);
                if (name.StartsWith("Cooked")) {
                    flavor = name.Substring(6);
                    break;
                }
            }
            if (flavor == null)
                return;

            string path = dir + Path.DirectorySeparatorChar + TOCRoot + Path.DirectorySeparatorChar + flavor + "TOC.txt";
            using (StreamWriter writer = new StreamWriter(File.OpenWrite(path))) {
                List<string> files = GetFiles(dir);
                string pathBase = string.Join(DirSeparator.ToString(), Enumerable.Repeat("..", TOCDepth));
                foreach (string file in files) {
                    if (file == path) continue;
                    string curPath = pathBase + file.Substring(dir.Length).Replace(Path.DirectorySeparatorChar, DirSeparator);
                    long size = new FileInfo(file).Length;
                    long uncompressedSize = 0;
                    string uncompressedPath = file + ".uncompressed_size";
                    if (File.Exists(uncompressedPath))
                        uncompressedSize = long.Parse(File.ReadAllText(uncompressedPath).Trim());
                    writer.WriteLine(size + " " + uncompressedSize + " " + curPath + " 0");
                }
                writer.WriteLine("0 0 " + pathBase + DirSeparator + flavor + "TOC.txt 0");
            }
        }

        static void Usage() {
            Console.WriteLine("TODO: Usage");
        }

        static void Main(string[] args) {
            if (args.Length != 1) {
                Usage();
            } else {
                if (Directory.Exists(args[0])) {
                    MakeTOC(args[0]);
                }
            }
        }
    }
}
