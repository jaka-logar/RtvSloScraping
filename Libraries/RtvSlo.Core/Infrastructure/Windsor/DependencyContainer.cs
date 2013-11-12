using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Windsor;

namespace RtvSlo.Core.Infrastructure.Windsor
{
    public class DependencyContainer
    {
        private static IWindsorContainer instance;

        private DependencyContainer() { }

        public static IWindsorContainer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new WindsorContainer();
                }
                return instance;
            }
        }
    }
}
