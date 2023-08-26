using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using log4net;

namespace FishingFunBot.Platform
{
    public static class WowProcess
    {
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        public static ILog logger = LogManager.GetLogger("Fishbot");
        private static ConsoleKey lastKey;
        private static readonly Random random = new Random();

        public static bool IsWowClassic()
        {
            var wowProcess = Get();
            return wowProcess != null ? wowProcess.ProcessName.ToLower().Contains("classic") : false;
        }

        //Get the wow-process, if success returns the process else null
        public static Process? Get(string name = "")
        {
            var names = string.IsNullOrEmpty(name)
                ? new List<string> { "Wow", "WowClassic", "Wow-64" }
                : new List<string> { name };

            var processList = Process.GetProcesses();
            foreach (var p in processList)
                if (names.Contains(p.ProcessName))
                    return p;

            logger.Error($"Failed to find the wow process, tried: {string.Join(", ", names)}");

            return null;
        }

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        private static Process GetActiveProcess()
        {
            var hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            return Process.GetProcessById((int)pid);
        }

        private static void KeyDown(ConsoleKey key)
        {
            lastKey = key;
            var wowProcess = Get();
            if (wowProcess != null) PostMessage(wowProcess.MainWindowHandle, WM_KEYDOWN, (int)key, 0);
        }

        private static void KeyUp()
        {
            KeyUp(lastKey);
        }

        public static void PressKey(ConsoleKey key)
        {
            KeyDown(key);
            Thread.Sleep(50 + random.Next(0, 75));
            KeyUp(key);
        }

        public static void KeyUp(ConsoleKey key)
        {
            var wowProcess = Get();
            if (wowProcess != null) PostMessage(wowProcess.MainWindowHandle, WM_KEYUP, (int)key, 0);
        }

        public static void RightClickMouse(ILog logger, Point position)
        {
            var activeProcess = GetActiveProcess();
            var wowProcess = Get();
            if (wowProcess != null)
            {
                var oldPosition = Cursor.Position;

                Cursor.Position = position;
                PostMessage(wowProcess.MainWindowHandle, Keys.WM_RBUTTONDOWN, Keys.VK_RMB, 0);
                Thread.Sleep(30 + random.Next(0, 47));
                PostMessage(wowProcess.MainWindowHandle, Keys.WM_RBUTTONUP, Keys.VK_RMB, 0);

                RefocusOnOldScreen(logger, activeProcess, wowProcess, oldPosition);
            }
        }

        public static void RightClickMouse()
        {
            var activeProcess = GetActiveProcess();
            var wowProcess = Get();
            if (wowProcess != null)
            {
                var oldPosition = Cursor.Position;
                PostMessage(wowProcess.MainWindowHandle, Keys.WM_RBUTTONDOWN, Keys.VK_RMB, 0);
                Thread.Sleep(30 + random.Next(0, 47));
                PostMessage(wowProcess.MainWindowHandle, Keys.WM_RBUTTONUP, Keys.VK_RMB, 0);
            }
        }

        private static void RefocusOnOldScreen(ILog logger, Process activeProcess, Process wowProcess,
            Point oldPosition)
        {
            try
            {
                if (activeProcess.MainWindowTitle != wowProcess.MainWindowTitle)
                {
                    // get focus back on this screen
                    PostMessage(activeProcess.MainWindowHandle, Keys.WM_RBUTTONDOWN, Keys.VK_RMB, 0);
                    Thread.Sleep(30);
                    PostMessage(activeProcess.MainWindowHandle, Keys.WM_RBUTTONUP, Keys.VK_RMB, 0);

                    KeyDown(ConsoleKey.Escape);
                    Thread.Sleep(30);
                    KeyUp(ConsoleKey.Escape);

                    Cursor.Position = oldPosition;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }
    }
}