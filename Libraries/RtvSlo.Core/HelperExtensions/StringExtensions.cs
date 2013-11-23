using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using Castle.Core.Logging;
using HtmlAgilityPack;
using RtvSlo.Core.Configuration;
using RtvSlo.Core.Helpers;
using RtvSlo.Core.Infrastructure.Windsor;
using VDS.RDF;

namespace RtvSlo.Core.HelperExtensions
{
    public static class StringExtensions
    {
        private static ILogger logger = DependencyContainer.Instance.Resolve<ILogger>();

        public static void NullOrEmptyCheck(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                //Environment.StackTrace;
                throw new ArgumentNullException();
            }
        }

        public static string GetStartingNumber(this string str)
        {
            Regex regex = new Regex(@"^[\d\.]+", RegexOptions.IgnoreCase);
            Match match = regex.Match(str);

            if (match.Success)
            {
                return match.Value;
            }

            return null;
        }

        public static string ToFullRtvSloUrl(this string path)
        {
            if (path.StartsWith("/"))
            {
                return string.Format("{0}{1}", RtvSloConfig.RtvSloUrl, path);
            }
            else
            {
                return string.Format("{0}/{1}", RtvSloConfig.RtvSloUrl, path);
            }
        }

        public static HtmlNode CreateRootNode(this string html)
        {
            html.NullOrEmptyCheck();

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            HtmlNode rootNode = htmlDocument.DocumentNode;
            rootNode.NullCheck();

            return rootNode;
        }

        public static string SafeEscapeHtml(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            return HttpUtility.HtmlEncode(str);
        }

        public static string SafeTrimAndEscapeHtml(this string str)
        {
            return str.SafeTrim().SafeEscapeHtml();
        }

        public static Uri ToUri(this string str)
        {
            try
            {
                Uri uri = new Uri(str);
                return uri;
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("StringExtensions, ToUri - EXCEPTION: {0}", ex.Message);
                //return null;

                throw;
            }
        }

        public static string ToFullNamespaceUrl(this string str)
        {
            if (RtvSloConfig.UseFullNamespaceUrl)
            {
                /// use complete url
                string[] splitted = str.Split(new char[] { ':' }, 2);
                if (!splitted.IsEmpty() && splitted.Length == 2 && RepositoryHelper.NamespaceDictionary.ContainsKey(splitted[0]))
                {
                    return string.Format("{0}{1}", RepositoryHelper.NamespaceDictionary[splitted[0]].FullPath, splitted[1]);
                }
            }

            return str;
        }

        public static IUriNode ToUriNode(this string str, IGraph g)
        {
            str = str.ToFullNamespaceUrl();

            return g.CreateUriNode(str.ToUri());
        }

        public static ILiteralNode ToLiteralNode(this string str, IGraph g, string language = null, Uri dataType = null)
        {
            if (dataType != null)
            {
                /// disabled - query filtering works without datetype specified
                //return g.CreateLiteralNode(str, dataType);
            }
            else if (!string.IsNullOrEmpty(language))
            {
                return g.CreateLiteralNode(str, language);
            }

            return g.CreateLiteralNode(str);
        }

        #region Parsing

        public static bool TryParseExactLogging(this string s, string format, IFormatProvider provider, DateTimeStyles style, out DateTime result)
        {
            if (s.EndsWith(","))
            {
                s = s.Remove(s.LastIndexOf(","));
            }

            Regex regex = new Regex(@"^\d{2}");
            Match match = regex.Match(s);

            if (!match.Success)
            {
                /// add starting 0 if day has only one digit
                s = s.Insert(0, "0");
            }

            if (DateTime.TryParseExact(@s, format, provider, style, out result))
            {
                return true;
            }
            else
            {
                logger.ErrorFormat("Datetime parse - STRING: {0}, FORMAT: {1}", s, format);
            }

            return false;
        }

        public static bool TryParseLogging(this string str, out DateTime result)
        {
            if (DateTime.TryParse(str, out result))
            {
                return true;
            }
            else
            {
                logger.ErrorFormat("Datetime parse - STRING: {0}", str);
            }

            return false;
        }

        public static bool TryParseLogging(this string s, out decimal result)
        {
            if (decimal.TryParse(s, out result))
            {
                return true;
            }
            else
            {
                logger.ErrorFormat("Decimal parse - STRING: {0}", s);
            }

            return false;
        }

        public static bool TryParseLogging(this string s, NumberStyles style, IFormatProvider provider, out decimal result)
        {
            if (decimal.TryParse(s, style, provider, out result))
            {
                return true;
            }
            else
            {
                logger.ErrorFormat("Decimal parse - STRING: {0}, STYLE: {1}, FORMAT: {2}", s, style.ToString(), provider.SerializeObject());
            }

            return false;
        }

        public static bool TryParseLogging(this string s, out int result)
        {
            if (int.TryParse(s, out result))
            {
                return true;
            }
            else
            {
                logger.ErrorFormat("Integer parse - STRING: {0}", s);
            }

            return false;
        }

        public static bool TryParseLogging(this string s, NumberStyles style, IFormatProvider provider, out int result)
        {
            if (int.TryParse(s, style, provider, out result))
            {
                return true;
            }
            else
            {
                logger.ErrorFormat("Integer parse - STRING: {0}, STYLE: {1}, FORMAT: {2}", s, style.ToString(), provider.SerializeObject());
            }

            return false;
        }

        #endregion Parsing
    }
}
