using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Security.Cryptography;

using System.Collections;
using System.Globalization;
using System.Resources;

using ZMinifier.Compressors;

namespace ZMinifier
{
    class Program
    {

        private static string systemTempPath = System.IO.Path.GetTempPath();
        private static string appTempPath = systemTempPath + @"ZMinifier\";

        #region External Tools Commands

        private static CompressorBase pngCompressor = CompressorFactory.CreateObject<PngCrush>(appTempPath + "pngcrush.exe");
        private static CompressorBase jpgCompressor = CompressorFactory.CreateObject<JpegTran>(appTempPath + "jpegtran.exe");
        private static CompressorBase gifCompressor = CompressorFactory.CreateObject<Gifsicle>(appTempPath + "gifsicle.exe");

        #endregion

        static void Main(string[] args)
        {

            #region Create Temporal Folders

            if (System.IO.Directory.Exists(appTempPath)){
                System.IO.Directory.Delete(appTempPath, true);
            }
            System.IO.Directory.CreateDirectory(appTempPath);

            #endregion

            #region Load Resources

            ResourceSet resources = ZMinifier.Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);

            #region Load DLLs

            AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs arguments) =>
            {
                string resourceName = arguments.Name.Split(',')[0].Replace(".", "_").Trim() + "_dll";
                resources = ZMinifier.Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
                foreach (DictionaryEntry resource in resources)
                {
                    if (resource.Key.ToString().Equals(resourceName))
                    {
                        return Assembly.Load((byte[])resource.Value);
                    }
                }

                return null;
            }; 

            #endregion

            #region Load EXEs

            foreach (DictionaryEntry resource in resources)
            {
                if (resource.Key.ToString().EndsWith("_exe"))
                {
                    ((byte[])resource.Value).ToFile( appTempPath + resource.Key.ToString().Replace("_exe", ".exe"));
                }
            }
            #endregion

            #endregion

            string dir = (args.Length > 0) ? args[0] : System.IO.Directory.GetCurrentDirectory();
            Console.WriteLine("Directory to minify:");
            Console.WriteLine(dir);

            DirectoryInfo di = new DirectoryInfo(dir);
            
            //JSS & CSS
            FileInfo[] jsFiles = di.GetFiles("*.js", SearchOption.AllDirectories);
            FileInfo[] cssFiles = di.GetFiles("*.css", SearchOption.AllDirectories);
            FileInfo[] htmlFiles = di.GetFiles("*.html", SearchOption.AllDirectories);

            //Images
            FileInfo[] jpegFiles = di.GetFiles("*.jpg", SearchOption.AllDirectories);
            FileInfo[] gifFiles = di.GetFiles("*.gif", SearchOption.AllDirectories);
            FileInfo[] pngFiles = di.GetFiles("*.png", SearchOption.AllDirectories);

            //Text files
            FileInfo[] textFiles = di.GetMultipleFiles(new string[]{"*.txt", "*.html", "*.aspx", "*.ascx", "*.master", "*.cs", "*.cshtml", "*.java", "*.jsp"}, SearchOption.AllDirectories);

            double initialFileSize = jsFiles.GetTotalSize() + cssFiles.GetTotalSize() + htmlFiles.GetTotalSize() + jpegFiles.GetTotalSize() + gifFiles.GetTotalSize() + pngFiles.GetTotalSize();
            Console.WriteLine(string.Empty);
            Console.WriteLine(string.Format("Initial files size: {0:0,0.}KB", initialFileSize));

            int countMinified = _minifyFiles(jsFiles, cssFiles, htmlFiles);
            Console.WriteLine(string.Empty);

            int countCompressed = _compressFiles(jpegFiles, gifFiles, pngFiles);
            Console.WriteLine(string.Empty);

            double finalFileSize = jsFiles.GetTotalSize() + cssFiles.GetTotalSize() + htmlFiles.GetTotalSize() + jpegFiles.GetTotalSize() + gifFiles.GetTotalSize() + pngFiles.GetTotalSize();
            Console.WriteLine(string.Format("Final files size: {0:0,0.}KB", finalFileSize));
            Console.WriteLine(string.Empty);

            Console.WriteLine(string.Format("{0} files minified", countMinified));
            Console.WriteLine(string.Empty);

            Console.WriteLine(string.Format("{0} files compressed", countCompressed));
            Console.WriteLine(string.Empty);
            
            Console.WriteLine(string.Format("KBs saved: {0:0,0.}KB", initialFileSize - finalFileSize));
            Console.WriteLine(string.Empty);

            _addTimeStamps(textFiles.Union(jsFiles).Union(cssFiles).ToArray(), jsFiles.Union(cssFiles).Union(htmlFiles).Union(jpegFiles).Union(gifFiles).Union(pngFiles).ToArray());

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);

            #region Clean Resources

            //Clean resources
            resources.Dispose();

            //Delete the temporal folder
            if (System.IO.Directory.Exists(appTempPath))
            {
                System.IO.Directory.Delete(appTempPath, true);
            }

            #endregion

        }

        #region JS/CSS

        static private int _minifyFiles(FileInfo[] jsFiles, FileInfo[] cssFiles, FileInfo[] htmlFiles)
        {
            int count = 0;
            Console.WriteLine(string.Empty);
            Console.WriteLine("Files (JS/CSS) not minified:");
            Console.WriteLine(string.Empty);

            foreach (FileInfo jsFile in jsFiles)
            {
                if (!_minifyJavaScript(jsFile.FullName))
                {
                    Console.WriteLine(jsFile.FullName);
                }
                else
                {
                    count++;
                }

                System.Threading.Thread.Sleep(50);
            }

            foreach (FileInfo cssFile in cssFiles)
            {
                if (!_minifyCss(cssFile.FullName))
                {
                    Console.WriteLine(cssFile.FullName);
                }
                else
                {
                    count++;
                }

                System.Threading.Thread.Sleep(50);
            }
            
            foreach (FileInfo htmlFile in htmlFiles)
            {
                if (!_minifyHtml(htmlFile.FullName))
                {
                    Console.WriteLine(htmlFile.FullName);
                }
                else
                {
                    count++;
                }

                System.Threading.Thread.Sleep(50);
            }

            return count;
        }

        static private bool _minifyJavaScript(string filePath){
            string source;
            using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
            {
                source = reader.ReadToEnd();
                reader.Close();
            }

            try
            {
            	source = NUglify.Uglify.Js(source).ToString();
            }
            catch (Exception)
            {
                return false;
            }

            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
            {
                writer.Write(source);
                writer.Close();
            }

            return true;
        }

        static private bool _minifyCss(string filePath)
        {
            string source;
            using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
            {
                source = reader.ReadToEnd();
                reader.Close();
            }

            try
            {
            	source = NUglify.Uglify.Css(source).ToString();
            }
            catch (Exception)
            {
                return false;
            }

            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
            {
                writer.Write(source);
                writer.Close();
            }

            return true;
        }
        
        static private bool _minifyHtml(string filePath)
        {
            string source;
            using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
            {
                source = reader.ReadToEnd();
                reader.Close();
            }

            try
            {
            	source = NUglify.Uglify.Html(source).ToString();
            }
            catch (Exception)
            {
                return false;
            }

            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
            {
                writer.Write(source);
                writer.Close();
            }

            return true;
        }

        #endregion

        #region Images

        static private int _compressFiles(FileInfo[] jpegFiles, FileInfo[] gifFiles, FileInfo[] pngFiles)
        {
            int count = 0;
            Console.WriteLine(string.Empty);
            Console.WriteLine("Files (Images) not compressed:");
            Console.WriteLine(string.Empty);

            foreach (FileInfo image in jpegFiles)
            {
                if (!jpgCompressor.Compress(image.FullName))
                {
                    Console.WriteLine(image.FullName);
                }
                else
                {
                    count++;
                }

                System.Threading.Thread.Sleep(50);
            }

            foreach (FileInfo image in gifFiles)
            {
                if (!gifCompressor.Compress(image.FullName))
                {
                    Console.WriteLine(image.FullName);
                }
                else
                {
                    count++;
                }

                System.Threading.Thread.Sleep(50);
            }

            foreach (FileInfo image in pngFiles)
            {
                if (!pngCompressor.Compress(image.FullName))
                {
                    Console.WriteLine(image.FullName);
                }
                else
                {
                    count++;
                }

                System.Threading.Thread.Sleep(50);
            }

            return count;
        }

        #endregion

        static private bool _addTimeStamps(FileInfo[] sourceFiles, FileInfo[] modifiedFiles)
        {
            if (sourceFiles.Count() > 0 && modifiedFiles.Count() > 0)
            {
                Hashtable md5 = new Hashtable();
                foreach (FileInfo modifiedFile in modifiedFiles)
                {
                    if (!md5.ContainsKey(modifiedFile.FullName))
                    {
                        md5.Add(modifiedFile.FullName, modifiedFile.ToMD5());
                    }
                }

                foreach (FileInfo sourceFile in sourceFiles)
                {
                    //We first load the whole text file and remove any previous MD5 value on any reference (to avoid having MD5 concatenated)
                    StringBuilder sb = new StringBuilder(Regex.Replace(File.ReadAllText(sourceFile.FullName), "\\?__md5=.{32}", string.Empty));

                    foreach (FileInfo modifiedFile in modifiedFiles)
                    {
                        sb.Replace(modifiedFile.Name, modifiedFile.Name + "?__md5=" + md5[modifiedFile.FullName]);
                    }

                    File.WriteAllText(sourceFile.FullName, sb.ToString(), Encoding.UTF8);
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
