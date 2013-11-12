
namespace RtvSlo.Core.XPathHelpers
{
    public static class CommentsPageXPath
    {
        public const string CommentContent = "//div[@class='newscomments']/div[@class='com']";


        public const string HeaderInfo = "./dt[@class='ds1']/div[@class='ac']";

        public const string Content = "./dt[@class='ds2']";

        public const string Rating = "./dt[@class='ds3']";

        public const string PlusRating = ".//div[@class='tcu']/span";

        public const string MinusRating = ".//div[@class='tcd']/span";
    }
}
