using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using TP_Link.Kasa.Bulb.UI;
using System.Threading;

namespace TP_Link.Kasa.Bulb
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly string mutexID = "TP_Link.Kasa.Bulb";
        private Forms.NotifyIcon notifyIcon = new Forms.NotifyIcon();
        private MainWindow mainWindow = new MainWindow();
        private bool windowMoved = false;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!new Mutex(false, mutexID).WaitOne(0, false))
            {
                MessageBox.Show("Another instance of TP-Link Kasa Bulb is already running.",
                    "TP-Link Kasa Bulb.", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Environment.Exit(-1);
            }

            mainWindow.closebtn.Click += (_s, _e) => _Exit();
            mainWindow.Deactivated += (_s, _e) => { if (!windowMoved) mainWindow.Hide(); };
            mainWindow.menuBar.MouseDown += (_s, _e) =>
            {
                windowMoved = true;
                mainWindow.ShowInTaskbar = true;
            };
            mainWindow.minimisebtn.Click += (_s, _e) =>
            {
                windowMoved = false;
                mainWindow.ShowInTaskbar = false;
            };

            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            notifyIcon.Text = "TP-Link Bulb Control";
            notifyIcon.Click += NotifyIcon_Click;
            notifyIcon.ContextMenu = new Forms.ContextMenu(new Forms.MenuItem[] { new Forms.MenuItem("Exit", (_s, _e) => _Exit())});
            notifyIcon.Visible = true;
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (!mainWindow.IsVisible)
            {
                System.Drawing.Point point = Forms.Control.MousePosition;
                mainWindow.Width = 450;
                mainWindow.Height = 600;
                mainWindow.Show();
                mainWindow.Top = point.Y - mainWindow.Height;
                mainWindow.Left = point.X - mainWindow.Width;
            }
            else if (windowMoved) mainWindow.Activate();
            else mainWindow.Hide();
        }

        protected override void OnExit(ExitEventArgs e = default)
        {
            notifyIcon?.Dispose();
            base.OnExit(e);
        }

        public void _Exit()
        {
            notifyIcon.Dispose();
            Environment.Exit(0);
        }
    }
}
