using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ffsx.Class
{
    public static class WpfFileIconCache
    {
        #region Shell
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
        public const int MAX_PATH = 256;
        [StructLayout(LayoutKind.Sequential)]
        public struct SHITEMID
        {
            public ushort cb;
            [MarshalAs(UnmanagedType.LPArray)]
            public byte[] abID;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ITEMIDLIST
        {
            public SHITEMID mkid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public IntPtr pszDisplayName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszTitle;
            public uint ulFlags;
            public IntPtr lpfn;
            public int lParam;
            public IntPtr iImage;
        }

        class Win32
        {
            // Browsing for directory.
            public const uint BIF_RETURNONLYFSDIRS = 0x0001;
            public const uint BIF_DONTGOBELOWDOMAIN = 0x0002;
            public const uint BIF_STATUSTEXT = 0x0004;
            public const uint BIF_RETURNFSANCESTORS = 0x0008;
            public const uint BIF_EDITBOX = 0x0010;
            public const uint BIF_VALIDATE = 0x0020;
            public const uint BIF_NEWDIALOGSTYLE = 0x0040;
            public const uint BIF_USENEWUI = (BIF_NEWDIALOGSTYLE | BIF_EDITBOX);
            public const uint BIF_BROWSEINCLUDEURLS = 0x0080;
            public const uint BIF_BROWSEFORCOMPUTER = 0x1000;
            public const uint BIF_BROWSEFORPRINTER = 0x2000;
            public const uint BIF_BROWSEINCLUDEFILES = 0x4000;
            public const uint BIF_SHAREABLE = 0x8000;

            public const uint SHGFI_ICON = 0x000000100;     // get icon
            public const uint SHGFI_DISPLAYNAME = 0x000000200;     // get display name
            public const uint SHGFI_TYPENAME = 0x000000400;     // get type name
            public const uint SHGFI_ATTRIBUTES = 0x000000800;     // get attributes
            public const uint SHGFI_ICONLOCATION = 0x000001000;     // get icon location
            public const uint SHGFI_EXETYPE = 0x000002000;     // return exe type
            public const uint SHGFI_SYSICONINDEX = 0x000004000;     // get system icon index
            public const uint SHGFI_LINKOVERLAY = 0x000008000;     // put a link overlay on icon
            public const uint SHGFI_SELECTED = 0x000010000;     // show icon in selected state
            public const uint SHGFI_ATTR_SPECIFIED = 0x000020000;     // get only specified attributes
            public const uint SHGFI_LARGEICON = 0x000000000;     // get large icon
            public const uint SHGFI_SMALLICON = 0x000000001;     // get small icon
            public const uint SHGFI_OPENICON = 0x000000002;     // get open icon
            public const uint SHGFI_SHELLICONSIZE = 0x000000004;     // get shell size icon
            public const uint SHGFI_PIDL = 0x000000008;     // pszPath is a pidl
            public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;     // use passed dwFileAttribute
            public const uint SHGFI_ADDOVERLAYS = 0x000000020;     // apply the appropriate overlays
            public const uint SHGFI_OVERLAYINDEX = 0x000000040;     // Get the index of the overlay

            public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
            public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

            [DllImport("shell32.dll")]
            public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

            [DllImport("User32.dll")]
            public static extern int DestroyIcon(IntPtr hIcon);
        }
        #endregion

        public const string NO_EXTENSION_KEY = "?none";
        public const string DIRKEY = "?dir";
        public static Dictionary<string, BitmapSource> ICONS = new Dictionary<string, BitmapSource>();
        public static string GetFileIconKey(string file)
        {
            string imgkey = Path.GetExtension(file);
            
            if (imgkey == ".exe" || imgkey == ".lnk" || imgkey == ".ico")
                imgkey = "?" + (imageIndex++).ToString();
            else if (imgkey == "")
                imgkey = NO_EXTENSION_KEY;

            if (!ICONS.ContainsKey(imgkey))
            {
                try
                {
                    var icon = GetShellIconFrom(file, true);
                    ICONS[imgkey] = icon;
                }
                catch { }
            }

            return imgkey;
        }
        public static string GetDirectoryIconKey(string dir, bool uniqueKey = false)
        {
            string imgkey = DIRKEY;
            if (uniqueKey)
                imgkey = DIRKEY + dir;
            if (!ICONS.ContainsKey(imgkey))
            {
                try
                {
                    var icon = GetShellIconFrom(dir, true);
                    ICONS[imgkey] = icon;
                }
                catch { }
            }

            return imgkey;
        }
        public static string GetDriveIconKey(DriveInfo drv)
        {
            string imgkey = DIRKEY + drv.RootDirectory.FullName;
            if (!ICONS.ContainsKey(imgkey))
            {
                try
                {
                    var icon = GetShellIconFrom(drv.RootDirectory.FullName, true);
                    ICONS[imgkey] = icon;
                }
                catch { }
            }

            return imgkey;
        }
        public static string GetSpecialFolderIconKey(Environment.SpecialFolder f)
        {
            string imgkey = string.Format("{0}{1:X4}", DIRKEY, (int)f);
            if (!ICONS.ContainsKey(imgkey))
            {
                try
                {
                    var icon = GetShellIconFrom(Environment.GetFolderPath(f), true);
                    ICONS[imgkey] = icon;
                }
                catch { }
            }

            return imgkey; 
        }
        private static ulong imageIndex = 0;
        private static BitmapSource GetShellIconFrom(string path, bool large)
        {
            BitmapSource rval = null;
            SHFILEINFO shinfo = new SHFILEINFO();
            uint flags = Win32.SHGFI_ICON
                | (large ? Win32.SHGFI_LARGEICON : Win32.SHGFI_SMALLICON);

                    Win32.SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

                    rval = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        shinfo.hIcon,
                        System.Windows.Int32Rect.Empty,
                        null);
                    Win32.DestroyIcon(shinfo.hIcon);

            return rval;
        }
    }
}
