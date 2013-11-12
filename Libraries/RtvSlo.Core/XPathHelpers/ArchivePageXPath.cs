using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RtvSlo.Core.XPathHelpers
{
    public static class ArchivePageXPath
    {
        public const string PostTitlesContent = "//div[@id='container']//div[@id='contents']//div[@class='contents']/div[@class='body']/div[@id='sectionlist']/div[@class='listbody']";

        public const string PagerContent = "//div[@id='container']//div[@id='contents']//div[@class='contents']/div[@class='body']/div[@class='rpagin']";

        public const string PagerNextPage = "//div[@id='container']//div[@id='contents']//div[@class='contents']/div[@class='body']/div[@class='rpagin']/a[@class='next']";
    }
}
