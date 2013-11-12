using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Core.Logging;

namespace RtvSlo.Core.HelperExtensions
{
    public static class ILoggerExtensions
    {
        public static void InfoFormatStack(this ILogger logger, string format, params object[] args)
        {
            logger.InfoFormat("{0}, STACK: {1}", string.Format(format, args), Environment.StackTrace);
        }

        public static void DebugFormatStack(this ILogger logger, string format, params object[] args)
        {
            logger.DebugFormat("{0}, STACK: {1}", string.Format(format, args), Environment.StackTrace);
        }

        public static void WarnFormatStack(this ILogger logger, string format, params object[] args)
        {
            logger.WarnFormat("{0}, STACK: {1}", string.Format(format, args), Environment.StackTrace);
        }

        public static void ErrorFormatStack(this ILogger logger, string format, params object[] args)
        {
            logger.ErrorFormat("{0}, STACK: {1}", string.Format(format, args), Environment.StackTrace);
        }

        public static void FatalFormatStack(this ILogger logger, string format, params object[] args)
        {
            logger.FatalFormat("{0}, STACK: {1}", string.Format(format, args), Environment.StackTrace);
        }
    }
}
