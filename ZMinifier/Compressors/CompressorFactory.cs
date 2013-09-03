using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZMinifier.Compressors
{
    public class CompressorFactory
    {
        public static CompressorBase CreateObject<T>(string exe) where T : new()
        {
            T compressor = new T();
            ((CompressorBase)(object)compressor).Initialize(exe);

            return (CompressorBase)(object)compressor;
        }
    }
}
