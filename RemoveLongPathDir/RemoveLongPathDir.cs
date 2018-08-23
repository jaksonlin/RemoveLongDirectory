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
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteFile(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveDirectory(string lpFileName);

        public static bool RemoveLongPathFile(string path)
        {
            return DeleteFile($@"\\?\{path}");
        }

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

        internal static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        internal static int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        internal const int MAX_PATH = 260;
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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindClose(IntPtr hFindFile);

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
