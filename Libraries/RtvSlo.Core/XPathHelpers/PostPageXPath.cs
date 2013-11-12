
namespace RtvSlo.Core.XPathHelpers
{
    public static class PostPageXPath
    {
        public const string PostContent = "//div[@id='container']//div[@class='contents']/div[@class='body']/div[@id='newsbody']";

        public const string CommentsContent = "//div[@id='container']//div[@class='contents']/div[@class='cbox']/div[@class='contents']/div[@class='body']";

        public const string NumOfComments = "//div[@id='container']//div[@id='contents']/div[@class='cbox']/div[@class='header']//a[@id='scc']/div";


        public const string Title = "./h1";

        public const string Subtitle = "./div[@class='sub']";

        public const string InfoContent = "./div[@class='info']";

        public const string RatingContent = ".//span[@id='rate_results']";

        public const string Auhtor = "./div[@id='author']";

        /* social plugins */
        public const string FbLikesIframe = "//div[@id='newsblocks']/div[@id='share' and @class='image-box']//td[@id='fb_social']//iframe";

        public const string TweetsIframe = "//div[@id='newsblocks']/div[@id='share' and @class='image-box']//td[@id='tw_social']//iframe";

        public const string FbLikes = "//span[@class='pluginCountTextDisconnected']";

        public const string NumOfTweets = "//a[@id='count']";
    }
}
