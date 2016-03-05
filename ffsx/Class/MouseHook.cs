using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows;

namespace ffsx.Class
{

    /// <summary>
    /// Gibt eine Mausaktion an. Beinhaltet Position, Tasten statien und etwaige Verzögerung (Delay)
    /// </summary>
    public class MouseAktion
    {
        /// <summary>
        /// Position der Maus.
        /// </summary>
        public Point Position
        {
            get { return position; }
            set { position = value; }
        }
        Point position;

        /// <summary>
        /// Gedrückte Maustasten.
        /// </summary>
        public MouseButtons MouseButtonsDown
        {
            get { return mouseButtonsDown; }
            set { mouseButtonsDown = value; }
        }
        MouseButtons mouseButtonsDown;

        /// <summary>
        /// Losgelassene gedrückte Maustasten.
        /// </summary>
        public MouseButtons MouseButtonsUp
        {
            get { return mouseButtonsUp; }
            set { mouseButtonsUp = value; }
        }
        MouseButtons mouseButtonsUp;
    }

    public delegate void MouseAktionEventHandler(MouseAktion aktion);

    public static class MouseHook
    {
        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static event MouseAktionEventHandler MouseActing = null;
        public static bool Watching { get; set; }

        public static void BeginHook()
        {
            if (_hookID == IntPtr.Zero)
                _hookID = SetHook(_proc);
        }

        public static void StopHook()
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static Point LastPosition;
        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && Watching && MouseActing != null)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                MouseAktion a = new MouseAktion();

                if (MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
                    a.MouseButtonsDown = System.Windows.Forms.MouseButtons.Left;
                if (MouseMessages.WM_RBUTTONDOWN == (MouseMessages)wParam)
                    a.MouseButtonsDown = System.Windows.Forms.MouseButtons.Right;
                if (MouseMessages.WM_LBUTTONUP == (MouseMessages)wParam)
                    a.MouseButtonsUp = System.Windows.Forms.MouseButtons.Left;
                if (MouseMessages.WM_RBUTTONUP == (MouseMessages)wParam)
                    a.MouseButtonsUp = System.Windows.Forms.MouseButtons.Right;

                if (MouseMessages.WM_MOUSEMOVE == (MouseMessages)wParam)
                    a.Position = new Point(hookStruct.pt.x, hookStruct.pt.y);
                else
                    a.Position = LastPosition;
                LastPosition = a.Position;

                MouseActing(a);

            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}

