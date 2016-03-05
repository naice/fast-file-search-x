using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ffsx.Class
{
    public static class Win32
    {
        #region ShellFolder
        public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        public const uint SHGFI_DISPLAYNAME = 0x000000200;     // get display name

        [DllImport("shell32")]
        public static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes,
            out SHFILEINFO psfi, uint cbFileInfo, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };
        
        public static string GetDisplayName(Environment.SpecialFolder SpecialFolder)
        {
            var x = Environment.GetFolderPath(SpecialFolder);
            return GetDisplayName(x);
        }
        public static string GetDisplayName(string path)
        {
            SHFILEINFO shfi = new SHFILEINFO();
            if (0 != SHGetFileInfo(path, FILE_ATTRIBUTE_NORMAL, out shfi,
                (uint)Marshal.SizeOf(typeof(SHFILEINFO)), SHGFI_DISPLAYNAME))
            {
                return shfi.szDisplayName;
            }
            return null;
        }




        #endregion#

    }
}
