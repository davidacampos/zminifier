using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace ZMinifier
{
    public static class CoreExtensions
    {

        #region Byte[]

        public static void ToFile(this Byte[] bytes, string fileName)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                fileStream.Write(bytes, 0, bytes.Length);
            }
        }

        #endregion

        #region Stream

        public static byte[] ToBytes(this Stream stream)
        {
            int capacity = stream.CanSeek ? (int)stream.Length : 0;
            using (MemoryStream output = new MemoryStream(capacity))
            {
                int readLength;
                byte[] buffer = new byte[4096];

                do
                {
                    readLength = stream.Read(buffer, 0, buffer.Length); // had to change to buffer.Length
                    output.Write(buffer, 0, readLength);
                }
                while (readLength != 0);

                return output.ToArray();
            }
        }

        public static string ToMD5(this Stream stream)
        {
            //MD5 hash provider for computing the hash of the file
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

            //calculate the files hash
            md5.ComputeHash(stream);

            //byte array of files hash
            byte[] hash = md5.Hash;

            //string builder to hold the results
            StringBuilder sb = new StringBuilder(32);

            //loop through each byte in the byte array
            foreach (byte b in hash)
            {
                //format each byte into the proper value and append
                //current value to return value
                sb.Append(string.Format("{0:X2}", b));
            }

            //return the MD5 hash of the stream
            return sb.ToString();
        }

        #endregion

        #region FileInfo

        public static string ToMD5(this FileInfo file)
        {
            //MD5 hash provider for computing the hash of the file
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

            using (Stream stream = file.OpenRead())
            {
                //calculate the files hash
                md5.ComputeHash(stream);
            }

            //byte array of files hash
            byte[] hash = md5.Hash;

            //string builder to hold the results
            StringBuilder sb = new StringBuilder(32);

            //loop through each byte in the byte array
            foreach (byte b in hash)
            {
                //format each byte into the proper value and append
                //current value to return value
                sb.Append(string.Format("{0:X2}", b));
            }

            //return the MD5 hash of the stream
            return sb.ToString();
        }

        #endregion

        #region FileInfo[]

        public static double GetTotalSize(this FileInfo[] files)
        {
            long rtn = 0;
            foreach (FileInfo file in files)
            {
                file.Refresh();
                rtn += file.Length;
            }

            return ((double)rtn) / 1024;
        }


        #endregion

        #region DirectoryInfo

        public static FileInfo[] GetMultipleFiles(this DirectoryInfo di, string[] searchPatterns, SearchOption searchOption)
        {
            List<FileInfo> files = new List<FileInfo>();
            foreach (string searchPattern in searchPatterns)
            {
                files.AddRange(di.GetFiles(searchPattern, searchOption));
            }
            return files.ToArray();
        }

        #endregion

    }
}
