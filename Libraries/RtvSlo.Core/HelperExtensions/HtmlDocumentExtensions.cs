using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace RtvSlo.Core.HelperExtensions
{
    public static class HtmlDocumentExtensions
    {
        public static bool IsValid(this HtmlDocument html, bool throwInvalidException=false)
        {
            if (html.ParseErrors != null && html.ParseErrors.Count() > 0)
            {
                return false;
            }

            return true;
        }

        public static string SerializeHtmlNode(this HtmlNode node)
        {
            if (node == null)
            {
                return "[null]";
            }

            return node.OuterHtml.SafeTrim();
        }

        public static string GetTextContent(this HtmlNode node)
        {
            StringBuilder sb = new StringBuilder();
            if (node.HasChildNodes)
            {
                foreach (HtmlNode childNode in node.ChildNodes)
                {
                    sb.Append(childNode.GetTextContent());
                }
            }

            sb.Append(node.InnerText.SafeTrimAndEscapeHtml());
            sb.Append(" ");

            return sb.ToString();
        }
    }
}
