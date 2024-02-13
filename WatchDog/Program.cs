using System;
using System.Linq;
using System.Windows.Forms;

namespace AppleMusicRPC
{
    internal static class Program
    {

        private const string AppVersion = "v0.4.4";

        [STAThread]
        private static void Main()
        {

            if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1) return;

            new Provider();

            // var window = new Window(AppVersion);
            // window.FormBorderStyle = FormBorderStyle.FixedSingle;
            Application.Run(new ServiceApplicationContext());
        }
    }
}
