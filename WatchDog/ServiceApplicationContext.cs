using System;
using System.Windows.Forms;
using AppleMusicRPC.Properties;

namespace AppleMusicRPC
{
    internal class ServiceApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private Provider provider;

        public ServiceApplicationContext(Provider provider)
        {
            this.provider = provider;
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.TrayIcon,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items =
                    {
                        new ToolStripMenuItem("NowPlaying", null, NowPlaying),
                        new ToolStripMenuItem("Exit", null, Exit)
                    }
                },
                Visible = true
            };
        }

        void NowPlaying(object sender, EventArgs e) {
            provider.NowPlaying();
        }

        void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
