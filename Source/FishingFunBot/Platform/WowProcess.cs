using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

#nullable enable
namespace FishingFun
{
    public static class WowProcess
    {
        public static ILog logger = LogManager.GetLogger("Fishbot");

        private const UInt32 WM_KEYDOWN = 0x0100;
        private const UInt32 WM_KEYUP = 0x0101;
        private static ConsoleKey lastKey;
        private static Random random = new Random();
        public enum keyState
        {
            KEYDOWN = 0,
            EXTENDEDKEY = 1,
            KEYUP = 2
        };

        public static bool IsWowClassic()
        {
            var wowProcess = Get();
            return wowProcess != null ? wowProcess.ProcessName.ToLower().Contains("classic") : false; ;
        }

        //Get the wow-process, if success returns the process else null
        public static Process? Get(string name = "")
        {
            var names = string.IsNullOrEmpty(name) ? new List<string> { "Wow", "WowClassic", "Wow-64" } : new List<string> { name };

            var processList = Process.GetProcesses();
            foreach (var p in processList)
            {
                if (names.Contains(p.ProcessName))
                {
                    return p;
                }
            }

            logger.Error($"Failed to find the wow process, tried: {string.Join(", ", names)}");

            return null;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        // Re-write to a different API 

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);


        // Simulate keyboard clicks        
        [DllImport("user32.dll")]
        private static extern bool keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        // Send keyboard actions
        public static bool SendKeyboardAction(ConsoleKey key, keyState state)
        {
            return SendKeyboardAction((byte)key.GetHashCode(), state);
        }

        public static bool SendKeyboardAction(byte key, keyState state)
        {
            return keybd_event(key, 0, (uint)state, (UIntPtr)0);
        }

        public static void PressKeboyardKey(ConsoleKey key)
        {
            SendKeyboardAction(key, keyState.KEYDOWN);
            Thread.Sleep(50 + random.Next(0, 125));
            SendKeyboardAction(key, keyState.KEYUP);
        }

        // This function is tend to be used only with the WOW Process 
        public static void PressKey(ConsoleKey key)
        {
            var activeProcess = GetActiveProcess();
            var oldPosition = System.Windows.Forms.Cursor.Position;

            SetWowWindowActive();
            PressKeboyardKey(key);

            SetOldWindowActive(activeProcess, oldPosition);

        }

        static void SetWowWindowActive()
        {
            var wowProcess = WowProcess.Get();
            var activeProcess = GetActiveProcess();
            if (wowProcess != null && wowProcess != activeProcess)
            {
                SetForegroundWindow(wowProcess.MainWindowHandle);
            }

        }

        static void SetOldWindowActive(Process oldProcess, System.Drawing.Point oldPosition)
        {

            var activeProcess = GetActiveProcess();
            if (activeProcess != oldProcess)
            {
                logger.Info("Reseting the window:");
                SetForegroundWindow(oldProcess.MainWindowHandle);
                System.Windows.Forms.Cursor.Position = oldPosition;
            }           
        }


        static void MouseRightClick(System.Drawing.Point position)
        {

            const int MOUSEEVENTF_RIGHTDOWN = 0x08;
            const int MOUSEEVENTF_RIGHTUP = 0x10;

            var activeProcess = GetActiveProcess();
            var oldPosition = System.Windows.Forms.Cursor.Position;
            SetWowWindowActive();

            // Move mouse to the position we want
            System.Windows.Forms.Cursor.Position = position;
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);


            SetOldWindowActive(activeProcess, oldPosition);

        }
        // Re-write END

        static Process GetActiveProcess()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            return Process.GetProcessById((int)pid);
        }

        public static void RightClickMouse(ILog logger, System.Drawing.Point position)
        {
            MouseRightClick(position);
        }

    }
}