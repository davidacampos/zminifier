using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace ZMinifier.Compressors
{
    class PngCrush : CompressorBase
    {
        public override bool Compress(string filePath)
        {
            try
            {
                string tempFilePath = this.WorkingDir + Guid.NewGuid() + Path.GetExtension(filePath);
                this.RunExe("\"" + filePath + "\"", "\"" + tempFilePath + "\"");
                File.Delete(filePath);
                File.Move(tempFilePath, filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
