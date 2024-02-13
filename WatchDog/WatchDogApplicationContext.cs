using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WatchDog.Properties;

namespace WatchDog
{
    internal class WatchDogApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;

        public WatchDogApplicationContext()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.TrayIcon,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items = { new ToolStripMenuItem("Exit", null, Exit) }
                },
                Visible = true
            };
        }

        void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
