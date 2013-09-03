using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace ZMinifier.Compressors
{
    public abstract class CompressorBase
    {
        public string Exe { get; private set; }
        public string WorkingDir { get; private set; }

        private const int oneMinute = 60000;

        public void Initialize(string exe)
        {
            this.Exe = exe;
            this.WorkingDir = System.IO.Directory.GetParent(this.Exe).FullName + @"\";
        }

        public void RunExe(params string[] args)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    FileName = this.Exe,
                    WorkingDirectory = this.WorkingDir,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = false
                }
            };

            process.Start();
            process.WaitForExit(oneMinute);
        }

        public byte[] Compress(Stream stream)
        {
            string tmpFileName = Path.GetTempFileName();
            using (FileStream fileStream = File.Create(tmpFileName, (int)stream.Length))
            {
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, (int)bytesInStream.Length);
                fileStream.Write(bytesInStream, 0, (int)bytesInStream.Length);
            }

            Compress(tmpFileName);

            byte[] rtn;
            using (Stream fsAsset = new FileStream(tmpFileName, FileMode.Open, FileAccess.Read))
            {
                int bytesInFile = (int)fsAsset.Length;
                rtn = new Byte[bytesInFile];
                long bytesRead = fsAsset.Read(rtn, 0, bytesInFile);
            }
            return rtn;
        }

        public abstract bool Compress(string filePath);
    }
}
