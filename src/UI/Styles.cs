using Microsoft.Win32;
using System;
using System.Threading;
using System.Windows;

namespace TP_Link.Kasa.Bulb.UI
{
    public static class Styles
    {
        //For some reason unknown to me, this event handler just wouldn't work.
        public static event Action themesChanged;

        public static string background { get; private set; } = "#FFFFFFFF";
        public static string foreground { get; private set; } = "#FF000000";
        public static string accent { get; private set; } = "#FF0078D7";
        public static string border { get; private set; } = "#FFDDDDDD";

        static Styles()
        {
            GetStyles();

            //Check for system theme changes every 5 seconds.
            new Timer((s) =>
            {
                string fetchedAccent = SystemParameters.WindowGlassBrush.ToString();
                string fetchedBackground = "#FFFFFFFF";
                try
                {
                    if (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
                        .GetValue("AppsUseLightTheme").ToString() == "0") fetchedBackground = "#FF101011";
                }
                catch {}

                if (background != fetchedBackground || accent != fetchedAccent)
                {
                    GetStyles();
                    //themesChanged?.Invoke();
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private static void GetStyles()
        {
            accent = SystemParameters.WindowGlassBrush.ToString();
            try
            {
                if (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
                    .GetValue("AppsUseLightTheme").ToString() == "0")
                {
                    //Dark theme.
                    background = "#FF101011";
                    foreground = "#FFFFFFFF";
                    border = "#FF383838";
                }
                else
                {
                    //Light theme.
                    background = "#FFFFFFFF";
                    foreground = "#FF000000";
                    border = "#FFDDDDDD";
                }
            }
            catch{}
        }
    }
}
