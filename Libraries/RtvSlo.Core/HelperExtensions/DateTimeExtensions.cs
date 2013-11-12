using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Castle.Core.Logging;
using RtvSlo.Core.Infrastructure.Windsor;

namespace RtvSlo.Core.HelperExtensions
{
    public static class DateTimeExtensions
    {
        private static ILogger logger = DependencyContainer.Instance.Resolve<ILogger>();

        public static string ToShortDateTimeString(this DateTime dt)
        {
            return dt.ToString("dd.MM.yyyy HH:mm");
        }
    }
}
