using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RtvSlo.Core.Infrastructure.Windsor;
using Castle.Windsor.Installer;
using Castle.MicroKernel.Registration;
using RtvSlo.Services.Repository;
using RtvSlo.Services.Scraping;

namespace RtvSlo.Framework.Infrastructure.Windsor
{
    public static class DependencyRegistrar
    {
        public static void BootstrapContainer()
        {
            DependencyContainer.Instance.Install(FromAssembly.This());

            DependencyContainer.Instance.Register(Component.For<IRepositoryService>().ImplementedBy<RepositoryService>());
            DependencyContainer.Instance.Register(Component.For<IScrapingService>().ImplementedBy<ScrapingService>());
        }
    }
}
