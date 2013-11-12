using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace RtvSlo.Core.HelperExtensions
{
    public static class UriExtensions
    {
        public static Uri AddQueryString(this Uri uri, NameValueCollection collection)
        {
            StringBuilder sb = new StringBuilder(uri.AbsoluteUri);
            sb.Append("?");
            sb.Append(string.Join("&", Array.ConvertAll(collection.AllKeys, key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(collection[key])))));

            uri = new Uri(sb.ToString());
            return uri;
        }
    }
}
