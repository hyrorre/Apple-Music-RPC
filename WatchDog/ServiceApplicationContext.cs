using System;
using System.Windows.Forms;
using AppleMusicRPC.Properties;

namespace AppleMusicRPC
{
    internal class ServiceApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;

        public ServiceApplicationContext()
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
