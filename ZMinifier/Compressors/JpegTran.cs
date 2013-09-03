using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace ZMinifier.Compressors
{
    class JpegTran : CompressorBase
    {
        public override bool Compress(string filePath)
        {
            try
            {
                string tempFilePath = this.WorkingDir + Guid.NewGuid() + Path.GetExtension(filePath);
                string tempProgressiveFilePath = tempFilePath + ".progressive";
                string tempOptimizeFilePath = tempFilePath + ".optimize";

                this.RunExe("-copy none", "-progressive ", "\"" + filePath + "\"", "\"" + tempProgressiveFilePath + "\"");
                this.RunExe("-copy none", "-optimize ", "\"" + filePath + "\"", "\"" + tempOptimizeFilePath + "\"");

                string tempFinalFilePath;
                if (new FileInfo(tempProgressiveFilePath).Length > new FileInfo(tempOptimizeFilePath).Length)
                {
                    tempFinalFilePath = tempOptimizeFilePath;
                }
                else
                {
                    tempFinalFilePath = tempProgressiveFilePath;
                }

                File.Delete(filePath);
                File.Move(tempFinalFilePath, filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
