using System;
using System.Collections.Generic;
using RtvSlo.Core.Entities.RtvSlo;

namespace RtvSlo.Services.Scraping
{
    public partial interface IScrapingService
    {
        /// <summary>
        /// Returns response string for request on given absolute path
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        string GetPage(string absolutePath);

        /// <summary>
        /// Returns response string for request on given Url
        /// </summary>
        /// <param name="absoluteUrl"></param>
        /// <returns></returns>
        string GetPage(Uri absoluteUrl);


        /// <summary>
        /// Get HTML from www.rtvslo.si/arhiv
        /// Filtering options are in config file
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        string GetFilteredArchivePage(int page);

        /// <summary>
        /// Returns comments html page for postId
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        string GetCommentsPage(int postId, int page);

        /// <summary>
        /// Scrape post page
        /// </summary>
        /// <param name="postUrl"></param>
        /// <param name="post"></param>
        /// <returns></returns>
        Post ScrapePostPage(Uri postUrl, Post post);

        /// <summary>
        /// Scrape user page
        /// </summary>
        /// <param name="userUrl"></param>
        /// <returns></returns>
        User ScrapeUserPage(Uri userUrl);

        /// <summary>
        /// Scrape page of post comments
        /// </summary>
        /// <param name="html"></param>
        /// <param name="newsPost"></param>
        /// <returns></returns>
        IList<Comment> ScrapeCommentsPage(string html, Post newsPost);

        /// <summary>
        /// Scrape rtvslo.si archive page html
        /// </summary>
        /// <param name="html"></param>
        /// <param name="hasNextPage"></param>
        /// <returns></returns>
        IList<Post> ScrapeArhivePage(string html, out bool hasNextPage);
    }
}
