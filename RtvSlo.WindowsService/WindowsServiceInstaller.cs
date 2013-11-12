using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace RtvSlo.WindowsService
{
    [RunInstaller(true)]
    public partial class WindowsServiceInstaller : Installer
    {
        public WindowsServiceInstaller()
        {
            InitializeComponent();
            this.AfterInstall += new InstallEventHandler(serviceInstaller_AfterInstall);
        }

        private void serviceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            ServiceController sc = new ServiceController("RtvSlo Service");
            sc.Start();
        }
    }
}
