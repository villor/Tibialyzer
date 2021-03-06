﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tibialyzer {
    class ScreenshotManager {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT Rect);

        public static Bitmap takeScreenshot() {
            Process tibia_process = ProcessManager.GetTibiaProcess();
            if (tibia_process == null) return null; //no tibia to take screenshot of

            RECT Rect = new RECT();
            if (!GetWindowRect(tibia_process.MainWindowHandle, ref Rect)) return null;

            Bitmap bitmap = new Bitmap(Rect.right - Rect.left, Rect.bottom - Rect.top);
            using (Graphics gr = Graphics.FromImage(bitmap)) {
                gr.CopyFromScreen(new Point(Rect.left, Rect.top), Point.Empty, bitmap.Size);
            }
            return bitmap;

        }

        public static void saveScreenshot(string name, Bitmap bitmap) {
            if (bitmap == null) return;
            string path = SettingsManager.getSettingString("ScreenshotPath");
            if (path == null) return;

            DateTime dt = DateTime.Now;
            name = String.Format("{0} - {1}-{2}-{3} {4}h{5}m{6}s{7}ms.png", name, dt.Year.ToString("D4"), dt.Month.ToString("D2"), dt.Day.ToString("D2"), dt.Hour.ToString("D2"), dt.Minute.ToString("D2"), dt.Second.ToString("D2"), dt.Millisecond.ToString("D4"));
            path = Path.Combine(path, name);
            bitmap.Save(path, ImageFormat.Png);
            bitmap.Dispose();
            MainForm.mainForm.refreshScreenshots();
        }

    }
}
