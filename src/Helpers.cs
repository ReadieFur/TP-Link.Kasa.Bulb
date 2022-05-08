using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Timers = System.Timers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb
{
    public static class Helpers
    {
        public static void SetTimeout(Action action, int ms)
        {
            Timers.Timer timer = new Timers.Timer(ms);
            timer.AutoReset = false;
            timer.Elapsed += (s, e) =>
            {
                action();
                timer.Dispose();
            };
            timer.Start();
        }

        //https://stackoverflow.com/questions/3423214/convert-hsb-hsv-color-to-hsl
        public static (double, double, double) HsvToHsl(double h, double s, double v)
        {
            s /= 100;
            v /= 100;
            double l = (2 - s) * v / 2;
            if (l != 0)
            {
                if (l == 1) s = 0;
                else if (l < 0.5) s = s * v / (l * 2);
                else s = s * v / (2 - l * 2);
            }
            return (h, s * 100, l * 100);
        }

        //https://stackoverflow.com/questions/36721830/convert-hsl-to-rgb-and-hex
        public static string HslToHex((double, double, double) hsl) { return HslToHex(hsl.Item1, hsl.Item2, hsl.Item3); }
        public static string HslToHex(double h, double s, double l)
        {
            l /= 100;
            double a = s * Math.Min(l, 1 - l) / 100;
            string ToHex(double n)
            {
                double k = (n + h / 30) % 12;
                double colour = l - a * Math.Max(Math.Min(k - 3, Math.Min(9 - k, 1)), -1);
                return ((int)Math.Round(255 * colour)).ToString("X2").PadLeft(2, '0');
            }
            return $"#{ToHex(0)}{ToHex(8)}{ToHex(4)}";
        }

        //http://ariya.blogspot.com/2008/07/converting-between-hsl-and-hsv.html
        public static (double, double, double) HslToHsv((double, double, double) hsv) { return HslToHsv(hsv.Item1, hsv.Item2, hsv.Item3); }
        public static (double, double, double) HslToHsv(double h, double s, double l)
        {
            l *= 2;
            s *= l <= 1 ? l : 2 - l;
            double v = (l + s) / 2;
            s = (2 * s) / (l + s);
            return (h, s, v);
        }

        //https://stackoverflow.com/questions/78536/deep-cloning-objects?page=1&tab=trending#tab-top
        public static T CloneViaJson<T>(this T self)
        {
            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(self, null)) return default;

            // initialize inner objects individually
            // for example in default constructor some list property initialized with some values,
            // but in 'source' these items are cleaned -
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(self), deserializeSettings);
        }

        #region https://stackoverflow.com/questions/357076/best-way-to-hide-a-window-from-the-alt-tab-program-switcher
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);

        public static void HideWindowFromTabManager(this Window self)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(self);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }
        #endregion
    }
}
