using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RtvSlo.Framework.Infrastructure.Windsor;
using RtvSlo.Framework.Infrastructure.Windsor.Installers;

namespace RtvSlo.Framework
{
    public static class WindsorInstaller
    {
        public static void InstallAll()
        {
            DependencyRegistrar.BootstrapContainer();

            LoggerInstaller loggerInstaller = new LoggerInstaller();
            log4net.Config.XmlConfigurator.Configure();
            //loggerInstaller.Install();
        }
    }
}
