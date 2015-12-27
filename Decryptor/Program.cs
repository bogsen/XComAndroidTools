using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Decryptor {
    class Program {

        #region Configuration

        static readonly string Key = "dJCWdpqOjDc8m3aio83SXmToo4Rqz422";

        #endregion

        static void Decrypt(string path) {
            Console.WriteLine(path);
            Rijndael crypto = Rijndael.Create();
            crypto.KeySize = 256;
            crypto.Key = Encoding.ASCII.GetBytes(Key);
            crypto.BlockSize = 128;
            crypto.IV = new byte[16];
            crypto.Mode = CipherMode.ECB;
            crypto.Padding = PaddingMode.Zeros;
            ICryptoTransform transform = crypto.CreateDecryptor();
            string tmpPath = path + ".tmp";
            File.Delete(tmpPath);
            using (Stream input = new CryptoStream(File.OpenRead(path), transform, CryptoStreamMode.Read))
            using (Stream output = File.OpenWrite(tmpPath)) {
                input.CopyTo(output);
            }
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
                Decrypt(args[0]);
            }
        }
    }
}
