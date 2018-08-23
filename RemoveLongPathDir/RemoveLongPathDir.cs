using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RemoveLongPathDir
{
    public class RemoveLongPathDir
    {
        internal static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        internal static int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        internal const int MAX_PATH = 260;

        #region dllimports
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteFile(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveDirectory(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindClose(IntPtr hFindFile);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WIN32_FIND_DATA
        {
            internal FileAttributes dwFileAttributes;
            internal FILETIME ftCreationTime;
            internal FILETIME ftLastAccessTime;
            internal FILETIME ftLastWriteTime;
            internal int nFileSizeHigh;
            internal int nFileSizeLow;
            internal int dwReserved0;
            internal int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            internal string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            internal string cAlternate;
        }
        #endregion

        /// <summary>
        /// Remove file with length > 260
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool RemoveLongPathFile(string path)
        {
            return DeleteFile($@"\\?\{path}");
        }

        /// <summary>
        /// Remove the directory with length > 260, including sub-directories and files inside.
        /// It is designed to do best effort in removing the directory; when any DeleteFile fails, it will still continue to do RemoveDirectory.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool RemoveLongPathDirAndFiles(string path)
        {
            var fileAndDir = FindFilesAndDirs($@"\\?\{path}");
            fileAndDir.files.ForEach(f => DeleteFile(f));
            foreach (var item in fileAndDir.dirs.OrderByDescending(d => d.Length))
            {
                RemoveDirectory(item);
            }
            RemoveDirectory($@"\\?\{path}");
            return !Directory.Exists(path);
        }

        /// <summary>
        /// Derived from https://blogs.msdn.microsoft.com/bclteam/2007/03/26/long-paths-in-net-part-2-of-3-long-path-workarounds-kim-hamilton/
        /// Find Files And Dirs in dirName, dirName should be of format $@"\\?\{original_path}";
        /// and put the result in separated List for later removal of dirName.
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        public static (List<string> files, List<string> dirs) FindFilesAndDirs(string dirName)
        {
            List<string> resultFiles = new List<string>();
            List<string> resultDirs = new List<string>();
            WIN32_FIND_DATA findData;
            IntPtr findHandle = FindFirstFile(dirName + @"\*", out findData);

            if (findHandle != INVALID_HANDLE_VALUE)
            {
                bool found;
                do
                {
                    string currentFileName = findData.cFileName;
                    if (((int)findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
                    {
                        if (currentFileName != "." && currentFileName != "..")
                        {
                            resultDirs.Add(Path.Combine(dirName, currentFileName));
                            var childResults = FindFilesAndDirs(Path.Combine(dirName, currentFileName));
                            resultFiles.AddRange(childResults.files);
                            resultDirs.AddRange(childResults.dirs);
                        }
                    }
                    else
                    {
                        resultFiles.Add(Path.Combine(dirName, currentFileName));
                    }
                    found = FindNextFile(findHandle, out findData);
                }
                while (found);
            }
            FindClose(findHandle);
            return (files: resultFiles, dirs: resultDirs);
        }
    }
}
