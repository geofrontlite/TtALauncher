using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Trails_To_Azure_Launcher.Utils
{
    class FileUtils
    {
        private static readonly String[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static void CombineFiles(String inputDirectoryPath, String outputFilePath)//Amending file at D:\Wheat\Documented72_ed.dat
        {
            Debug.WriteLine("Amending file at " + outputFilePath);
            string[] inputFilePaths = Directory.GetFiles(inputDirectoryPath);

            using (var outputStream = File.Create(outputFilePath))
            {
                foreach (var inputFilePath in inputFilePaths)
                {
                    using (var inputStream = File.OpenRead(inputFilePath))
                    {
                        // Buffer size can be passed as the second argument.
                        inputStream.CopyTo(outputStream);
                    }
                }
            }
        }

        public static void DirectoryCopy(String sourceDirName, String destDirName, bool copySubDirs, bool overwrite)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (dir.Exists == false)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (Directory.Exists(destDirName) == false)
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, overwrite);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs == true)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs, overwrite);
                }
            }
        }

        public static void DirectoryMove(String sourceDirName, String destDirName, bool overwrite)
        {
            if (overwrite == false)
            {
                Directory.Move(sourceDirName, destDirName);
                return;
            }

            DirectoryCopy(sourceDirName, destDirName, true, overwrite);
            Directory.Delete(sourceDirName, true);
        }

        public static String SizeSuffix(ulong value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) 
            { 
                throw new ArgumentOutOfRangeException("decimalPlaces"); 
            }
            if (value < 0) 
            {
                throw new ArgumentOutOfRangeException("value");
            }
            if (value == 0) 
            { 
                return string.Format("{0:n" + decimalPlaces + "} bytes", 0); 
            }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", adjustedSize, SizeSuffixes[mag]);
        }
    }
}
