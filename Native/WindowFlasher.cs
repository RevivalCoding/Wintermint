using System;
using System.Runtime.InteropServices;

namespace WintermintClient.Native
{
    public static class WindowFlasher
    {
        private const uint FLASHW_ALL = 3;

        private const uint FLASHW_CAPTION = 1;

        private const uint FLASHW_STOP = 0;

        private const uint FLASHW_TIMER = 4;

        private const uint FLASHW_TIMERNOFG = 12;

        private const uint FLASHW_TRAY = 2;

        private static uint FLASHWINFO_SIZE;

        static WindowFlasher()
        {
            WindowFlasher.FLASHWINFO_SIZE = (uint)Marshal.SizeOf(typeof(WindowFlasher.FLASHWINFO));
        }

        private static void DoFlash(IntPtr handle, uint flags, uint count)
        {
            if (WindowFlasher.GetForegroundWindow() == handle)
            {
                return;
            }
            WindowFlasher.FLASHWINFO fLASHWINFO = new WindowFlasher.FLASHWINFO()
            {
                cbSize = WindowFlasher.FLASHWINFO_SIZE,
                dwTimeout = 0,
                hwnd = handle,
                dwFlags = flags,
                uCount = count
            };
            WindowFlasher.FLASHWINFO fLASHWINFO1 = fLASHWINFO;
            WindowFlasher.FlashWindowEx(ref fLASHWINFO1);
        }

        public static void Flash(IntPtr handle)
        {
            WindowFlasher.DoFlash(handle, 3, 3);
        }

        public static void Flash(IntPtr handle, int count)
        {
            WindowFlasher.DoFlash(handle, 3, (uint)count);
        }

        [DllImport("user32.dll", CharSet = CharSet.None, ExactSpelling = false)]
        private static extern bool FlashWindowEx(ref WindowFlasher.FLASHWINFO pwfi);

        [DllImport("user32.dll", CharSet = CharSet.None, ExactSpelling = false)]
        private static extern IntPtr GetForegroundWindow();

        public static void Pulse(IntPtr handle)
        {
            WindowFlasher.DoFlash(handle, 15, -1);
        }

        public static void Stop(IntPtr handle)
        {
            WindowFlasher.DoFlash(handle, 0, 0);
        }

        private struct FLASHWINFO
        {
            public uint cbSize;

            public IntPtr hwnd;

            public uint dwFlags;

            public uint uCount;

            public uint dwTimeout;
        }
    }
}