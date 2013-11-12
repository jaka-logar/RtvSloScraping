using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Castle.Core.Logging;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RtvSlo.Core.Configuration;
using RtvSlo.Core.Entities.RtvSlo;
using RtvSlo.Core.HelperEnums;
using RtvSlo.Core.HelperExtensions;
using RtvSlo.Core.Helpers;
using RtvSlo.Core.XPathHelpers;

namespace RtvSlo.Services.Scraping
{
    public partial class ScrapingService : IScrapingService
    {
        #region Fields

        private readonly ILogger _logger;
        private CultureInfo cultureInfo;

        #endregion Fields

        #region Ctor

        public ScrapingService(
            ILogger logger
            )
        {
            this._logger = logger;
            this.cultureInfo = new CultureInfo(RtvSloConfig.CultureInfo);
        }

        #endregion Ctor

        #region Public Methods

        /// <summary>
        /// Returns response string for request on given absolute path
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        public string GetPage(string absolutePath)
        {
            Uri uri = new Uri(string.Format("{0}{1}", RtvSloConfig.RtvSloUrl, absolutePath));
            return this.GetPage(uri);
        }

        /// <summary>
        /// Returns response string for request on given Url
        /// </summary>
        /// <param name="absoluteUrl"></param>
        /// <returns></returns>
        public string GetPage(Uri absoluteUrl)
        {
            return this.CreateWebRequest(absoluteUrl);
        }


        /// <summary>
        /// Get HTML from www.rtvslo.si/arhiv
        /// Filtering options are in config file
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public string GetFilteredArchivePage(int page)
        {
            NameValueCollection formData = new NameValueCollection();
            formData.Add("date_from", RtvSloConfig.Archive.FromDate.ToString("yyyy-MM-dd"));
            formData.Add("date_to", RtvSloConfig.Archive.ToDate.ToString("yyyy-MM-dd"));
            formData.Add("search_type", RtvSloConfig.Archive.SearchType);
            formData.Add("section", RtvSloConfig.Archive.Sections);
            
            if (page > 0)
            {
                formData.Add("page", page.ToString());
            }


            Uri filteredArchiveUri = new Uri(RtvSloConfig.Archive.ArchiveUrl);
            filteredArchiveUri = filteredArchiveUri.AddQueryString(formData);

            string serverResponse = this.CreateWebRequest(filteredArchiveUri);

            this._logger.DebugFormat("GetFilteredArchivePage - REQUEST: {0}", filteredArchiveUri);

            if (!string.IsNullOrEmpty(serverResponse))
            {
                return serverResponse;
            }

            return null;
        }

        /// <summary>
        /// Returns comments html page for postId
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public string GetCommentsPage(int postId, int page)
        {
            /// www.rtvslo.si/index.php?&c_mod=news&op=comments&func=ajax&id=302242&page=0&hash=0&sort=asc
            
            NameValueCollection formData = new NameValueCollection();
            formData.Add("c_mod", "news");
            formData.Add("op", "comments");
            formData.Add("func", "ajax");
            formData.Add("id", postId.ToString());
            formData.Add("page", page.ToString());
            formData.Add("hash", "0");
            formData.Add("sort", "asc");

            Uri commentsUri = new Uri(RtvSloConfig.RtvSloCommentsUrl);
            commentsUri = commentsUri.AddQueryString(formData);

            string serverResponse = this.CreateWebRequest(commentsUri);

            this._logger.DebugFormat("GetCommentsPage - REQUEST: {0}", commentsUri);

            return serverResponse;
        }


        /// <summary>
        /// Scrape post page
        /// </summary>
        /// <param name="postUrl"></param>
        /// <param name="post"></param>
        /// <returns></returns>
        public Post ScrapePostPage(Uri postUrl, Post post)
        {
            post = post ?? new Post();

            string html = this.CreateWebRequest(postUrl);
            if (!string.IsNullOrEmpty(html))
            {
                HtmlNode rootNode = html.CreateRootNode();

                /// accessed time
                post.AccessedDate = DateTime.UtcNow.ToUniversalTime();

                HtmlNode postContent = rootNode.SelectSingleNode(PostPageXPath.PostContent);
                post = this.ScrapePost(postContent, post);

                post = this.ScrapePostStatistics(rootNode, post);

                return post;
            }
            return null;
        }        

        /// <summary>
        /// Scrape user page
        /// </summary>
        /// <param name="userUrl"></param>
        /// <returns></returns>
        public User ScrapeUserPage(Uri userUrl)
        {
            User user = new User()
            {
                Url = userUrl.AbsoluteUri,
                AccessedDate = DateTime.UtcNow.ToUniversalTime(),
                Id = userUrl.AbsoluteUri.Substring(userUrl.AbsoluteUri.LastIndexOf("/") + 1)
            };

            string html = this.CreateWebRequest(userUrl);
            if (!string.IsNullOrEmpty(html))
            {
                HtmlNode rootNode = html.CreateRootNode();

                /// user rating
                HtmlNode ratingContent = rootNode.SelectSingleNode(UserPageXPath.RatingContent);
                user = this.ScrapeUserRating(ratingContent, user);

                /// user data
                HtmlNode dataContent = rootNode.SelectSingleNode(UserPageXPath.UserContentMedium);
                if (dataContent == null)
                {
                    dataContent = rootNode.SelectSingleNode(UserPageXPath.UserContentBig);
                }

                if (dataContent == null)
                {
                    dataContent = rootNode.SelectSingleNode(UserPageXPath.UserContentSmall);
                }

                //TODO check null
                if (dataContent != null)
                {
                    user = this.ScrapeUserData(dataContent, user);
                }
                else
                {
                    this._logger.FatalFormat("ScrapingService, ScrapeUserPage, dataContent == NULL - REQUEST: {0}", userUrl);
                }

                return user;
            }
            return null;
        }

        /// <summary>
        /// Scrape page of post comments
        /// </summary>
        /// <param name="html"></param>
        /// <param name="newsPost"></param>
        /// <returns></returns>
        public IList<Comment> ScrapeCommentsPage(string html, Post newsPost)
        {
            HtmlNode rootNode = html.CreateRootNode();

            IList<Comment> comments = new List<Comment>();

            HtmlNodeCollection commentsCollection = rootNode.SelectNodes(CommentsPageXPath.CommentContent);
            if (commentsCollection == null || commentsCollection.Count == 0)
            {
                return new List<Comment>();
            }


            IList<HtmlNode> commentNodes = commentsCollection.Where(x => x.SelectSingleNode(CommentsPageXPath.HeaderInfo) != null).ToList();

            if (commentNodes != null && commentNodes.Count > 0)
            {
                foreach (HtmlNode node in commentNodes)
                {
                    Comment comment = this.ScrapeComment(node);
                    if (comment != null)
                    {
                        //comment.PostGuidUrl = newsPost.GuidUrl;
                        //comment.PostUrl = newsPost.Url;
                        comment.PostId = newsPost.Id;

                        comments.Add(comment);
                    }
                }
            }
            else
            {
                this._logger.InfoFormat("ScrapingService, ScrapeCommentsPage, No comments - POST-URL: {0}, HTML: {1}", newsPost.Url, html);
            }

            return comments;
        }

        /// <summary>
        /// Scrape rtvslo.si archive page html
        /// Fill some post properties: Id, Url, Category, CategoryUrl
        /// </summary>
        /// <param name="html"></param>
        /// <param name="hasNextPage"></param>
        /// <returns></returns>
        public IList<Post> ScrapeArhivePage(string html, out bool hasNextPage)
        {
            hasNextPage = false;

            HtmlNode rootNode = html.CreateRootNode();

            IList<Post> result = new List<Post>();

            IList<HtmlNode> posts = rootNode.SelectNodes(ArchivePageXPath.PostTitlesContent).ToList();
            if (posts != null && posts.Count > 0)
            {
                /// posts
                foreach (HtmlNode node in posts)
                {
                    Post post = null;

                    /// <a href="/sport/zimski-sporti/sp-v-alpskem-smucanju-2013/kdo-je-kriv-za-zgodnjo-upokojitev-mateje-robnik/302384" class="title">Kdo je “kriv” za zgodnjo upokojitev Mateje Robnik?</a>
                    HtmlNode linkNode = node.ChildNodes["a"];
                    if (linkNode != null)
                    {
                        string href = string.Empty;
                        string url = string.Empty;
                        Category category = new Category();

                        /// href="/sport/zimski-sporti/sp-v-alpskem-smucanju-2013/kdo-je-kriv-za-zgodnjo-upokojitev-mateje-robnik/302384"
                        if (linkNode.Attributes["href"] != null && linkNode.Attributes["href"].Value.SafeTrim().Length > 5)
                        {
                            href = linkNode.Attributes["href"].Value.SafeTrim();
                            url = href.ToFullRtvSloUrl();

                            /// scrape categories
                            string[] splittedUrl = href.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                            if (splittedUrl.Length > 2)
                            {
                                for (int i = 0; i < splittedUrl.Length - 2; i++)
                                {
                                    if (i == 0)
                                    {
                                        /// top level category
                                        category.Label = splittedUrl[i];
                                        category.Url = string.Format("{0}/", splittedUrl[i].ToFullRtvSloUrl());
                                    }
                                    else
                                    {
                                        category.SaveChildCategory(new Category()
                                        {
                                            Label = splittedUrl[i]
                                        });
                                    }
                                }
                            }
                            else
                            {
                                this._logger.FatalFormat("ScrapingService, ScrapeArhivePage, Splitted URL length - URL: {2}, NODE: {0}, HTML: {1}", linkNode.SerializeHtmlNode(), html, href);
                            }
                        }
                        else
                        {
                            this._logger.FatalFormat("ScrapingService, ScrapeArhivePage, Post link - NODE: {0}, HTML: {1}", linkNode.SerializeHtmlNode(), html);
                        }

                        string title = linkNode.InnerHtml.SafeTrimAndEscapeHtml();

                        post = new Post()
                        {
                            Id = this.GetIdFromUrl(url),
                            Url = url,
                            Title = title,
                            Category = category,
                        };

                        if (post.Id == 0 ||
                            string.IsNullOrEmpty(post.Url) ||
                            string.IsNullOrEmpty(post.Title) ||
                            string.IsNullOrEmpty(post.Category.Label)
                            )
                        {
                            this._logger.FatalFormat("ScrapingService, ScrapeArhivePage, Post is not filled right - POST: {0}, HTML: {1}", post.SerializeObject(), html);
                        }
                    }

                    if (post != null)
                    {
                        result.Add(post);
                    }
                }
            }
            else
            {
                this._logger.FatalFormat("ScrapingService, ScrapeArhivePage, There are no posts - HTML: {0}", html);
            }

            /// pager
            HtmlNode pager = rootNode.SelectSingleNode(ArchivePageXPath.PagerContent);
            if (pager != null)
            {
                /// <a href="/arhiv/?date_from=2013-02-13&amp;date_to=2013-02-13&amp;section=1.2.16.43.4.5.3.8.129.12.9.28.6.24&amp;page=1">2</a>
                HtmlNode nextPage = pager.SelectSingleNode(ArchivePageXPath.PagerNextPage);

                if (nextPage != null && nextPage.Attributes["href"] != null)
                {
                    hasNextPage = true;
                }
            }
            else
            {
                this._logger.FatalFormat("ScrapingService, ScrapeArhivePage, Pager is null - HTML: {0}", html);
            }

            return result;
        }    

        

        #endregion Public Methods

        #region Private Methods

        #region Web Requests

        private string CreateWebRequest(Uri uri)
        {
            string serverResponse = null;

            try
            {
                WebRequest request = WebRequest.Create(uri);

                /// Chrome 30.0.1599.17
                ((HttpWebRequest)request).UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.17 Safari/537.36";

                WebResponse response = request.GetResponse();

                if (response.ResponseUri.AbsolutePath == "/")
                {
                    this._logger.WarnFormat("CreateWebRequest returns REDIRECT to index - REQUEST: {0}", uri.AbsoluteUri);
                    return null;
                }
                
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    serverResponse = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                this._logger.FatalFormat("CreateWebRequest - REQUEST: {0}, RESPONSE: {1}", uri.AbsoluteUri, ex.SerializeObject());
            }

            if (this._logger.IsDebugEnabled)
            {
                //this._logger.DebugFormat("CreateWebRequest - REQUEST: {0}, RESPONSE: {1}", uri.AbsoluteUri, serverResponse.SafeTrim());
                this._logger.InfoFormat("CreateWebRequest - REQUEST: {0}", uri.AbsoluteUri);
            }
            else
            {
                this._logger.InfoFormat("CreateWebRequest - REQUEST: {0}", uri.AbsoluteUri);
            }

            /// 500ms sleep
            Thread.Sleep(RtvSloConfig.RequestSleep);

            return serverResponse;
        }

        #endregion Web Requests

        #region Helpers

        private Post ScrapePost(HtmlNode contentNode, Post post)
        {
            post = post ?? new Post();

            contentNode.NullCheck();

            /// title
            HtmlNode titleNode = contentNode.SelectSingleNode(PostPageXPath.Title);
            if (titleNode != null)
            {
                post.Title = titleNode.InnerText.SafeTrimAndEscapeHtml();
            }
            else
            {
                this._logger.ErrorFormat("ScrapingService, ScrapePost, Title node is null - URL: {0}, NODE: {1}", post.Url, contentNode.SerializeHtmlNode());
            }

            /// subtitle
            HtmlNode subtitleNode = contentNode.SelectSingleNode(PostPageXPath.Subtitle);
            if (subtitleNode != null)
            {
                post.Subtitle = subtitleNode.InnerText.SafeTrimAndEscapeHtml();
            }
            else
            {
                this._logger.ErrorFormat("ScrapingService, ScrapePost, Subtitle node is null - URL: {0}, NODE: {1}", post.Url, contentNode.SerializeHtmlNode());
            }           

            /// author
            HtmlNode authorNode = contentNode.SelectSingleNode(PostPageXPath.Auhtor);
            if (authorNode != null)
            {
                IList<HtmlNode> authorNameNodes = authorNode.ChildNodes.Where(x => x.Name == "b" && x.ChildNodes.Where(t => t.Name == "a").Count() == 0).ToList();
                if (!authorNameNodes.IsEmpty())
                {
                    foreach (HtmlNode author in authorNameNodes)
                    {
                        //TODO http://www.rtvslo.si/mmc-priporoca/dame-niso-sposobne-zmagati-na-dirki-formule-ena/306771
                        User authorUser = new User()
                        {
                            Name = author.InnerText.SafeTrim().Replace(",", string.Empty).Replace("foto:", string.Empty).SafeTrimAndEscapeHtml(),
                            Function = UserFunctionEnum.Journalist
                        };

                        post.Authors.Add(authorUser);
                    }
                }

                //HtmlNode authorName = authorNode.ChildNodes.FindFirst("b");
                //if (authorName != null)
                //{
                //    post.Authors = authorName.InnerText.SafeTrimAndEscapeHtml();
                //}
            }

            if (post.Authors.IsEmpty())
            {
                //this._logger.WarnFormat("ScrapingService, ScrapePost, Author is empty - URL: {0}, NODE: {1}", post.Url, contentNode.SerializeHtmlNode());
                this._logger.WarnFormat("ScrapingService, ScrapePost, Author is empty - URL: {0}", post.Url);
            }

            /// info
            HtmlNode infoNode = contentNode.SelectSingleNode(PostPageXPath.InfoContent);
            if (infoNode != null)
            {
                // <div class="info">16. februar 2013 ob 07:22,<br>zadnji poseg: 16. februar 2013 ob 15:16<br>Schladming - MMC RTV SLO</div>

                IList<HtmlNode> textNodes = infoNode.ChildNodes.Where(x => x.Name == "#text").ToList();
                if (textNodes != null && textNodes.Count > 1)
                {
                    /// Created datetime
                    string createdDateTimeString = textNodes.First().InnerText.SafeTrim();
                    
                    DateTime createdDate;
                    if (createdDateTimeString.TryParseExactLogging(ParsingHelper.LongDateTimeParseExactPattern, this.cultureInfo, DateTimeStyles.None, out createdDate))
                    {
                        post.DateCreated = createdDate.ToUniversalTime();
                        post.LastUpdated = createdDate.ToUniversalTime();
                    }

                    /// Location
                    string locationString = textNodes.Last().InnerText;
                    IList<string> locationList = locationString.Split(new string[]{"-"}, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (locationList != null && locationList.Count > 1)
                    {
                        post.Location = locationList.First().SafeTrim();

                        if (locationList.Last().SafeTrim() != "MMC RTV SLO")
                        {
                            this._logger.DebugFormat("ScrapingService, ScrapePost, InfoNode, Location - URL: {0}, LIST: {1}", post.Url, locationList.SerializeObject());
                        }
                    }
                    else
                    {
                        this._logger.WarnFormat("ScrapingService, ScrapePost, InfoNode, Location - URL: {0}, NODE: {1}", post.Url, infoNode.SerializeHtmlNode());
                    }

                    if (textNodes.Count == 3)
                    {
                        /// Updated datetime
                        string updatedDateTimeString = textNodes[1].InnerText.SafeTrim();

                        Regex dateTimeRegex = new Regex(@"(?<date>[0-9\.]+[\w+\s+]+[0-9\:]+)", RegexOptions.IgnoreCase);

                        //TODO fix regex
                        Match dateTimeMatch = dateTimeRegex.Match(updatedDateTimeString);

                        if (dateTimeMatch.Success)
                        {
                            updatedDateTimeString = dateTimeMatch.Groups["date"].Value;

                            DateTime updatedDate;
                            if (updatedDateTimeString.TryParseExactLogging(ParsingHelper.LongDateTimeParseExactPattern, this.cultureInfo, DateTimeStyles.None, out updatedDate))
                            {
                                post.DateCreated = updatedDate.ToUniversalTime();
                            }
                        }
                    }
                }
                else
                {
                    this._logger.ErrorFormat("ScrapingService, ScrapePost, InfoNode - URL: {0}, NODE: {1}", post.Url, infoNode.SerializeHtmlNode());
                }
            }


            /// Main content
            IList<HtmlNode> contentNodes = new List<HtmlNode>();
            foreach (HtmlNode node in contentNode.ChildNodes)
            {
                /// ends with author
                if (node.Name == "div" && node.Attributes.FirstOrDefault(x => x.Value == "author") != null)
                {
                    break;
                }

                if ((node.Name == "p" || node.Name == "div") && node.FirstChild != null && node.FirstChild.Name != "div" && contentNodes.Count > 0)
                {
                    contentNodes.Add(node);
                }

                /// starts with p tag
                if (node.Name == "p" && node.FirstChild.Name != "div" && contentNodes.Count == 0)
                {
                    contentNodes.Add(node);
                }
            }

            //TODO remove
            string sasas = post.Url;

            if (!contentNodes.IsEmpty())
            {
                /// Abstract - text inside strong tag in first node
                HtmlNode abstractNode = contentNodes.First();
                HtmlNode strongAbstractNode = abstractNode.ChildNodes.First(x => x.Name == "strong");
                post.Abstract = strongAbstractNode.InnerText.SafeTrimAndEscapeHtml();

                /// remove abstract from main content
                abstractNode.ChildNodes.Remove(strongAbstractNode);

                /// Content
                StringBuilder content = new StringBuilder();

                foreach (HtmlNode node in contentNodes)
                {
                    // to get white space after paragraph title
                    foreach (HtmlNode childNode in node.ChildNodes)
                    {
                        string text = childNode.InnerText.SafeTrimAndEscapeHtml();
                        if (text.Length > 0)
                        {
                            content.AppendFormat("{0} ", text);
                        }
                    }
                }

                post.Content = content.ToString().SafeTrim();
            }
            else
            {
                this._logger.ErrorFormat("ScrapingService, ScrapePost - Post content is null - URL: {0}, NODE: {1}", post.Url, contentNode.SerializeHtmlNode());
            }

            return post;
        }

        /// <summary>
        /// Scrape post statistics
        /// Rating, Number of comments, Number of FB likes, Number of tweets
        /// </summary>
        /// <param name="rootNode"></param>
        /// <param name="post"></param>
        /// <returns></returns>
        private Post ScrapePostStatistics(HtmlNode rootNode, Post post)
        {
            post = post ?? new Post();

            /// rating
            HtmlNode ratingNode = rootNode.SelectSingleNode(PostPageXPath.RatingContent);
            if (ratingNode != null)
            {
                string ratingContent = ratingNode.InnerText;
                Regex ratingRegex = new Regex(@"\w+\s+(?<rating>[0-9\,]+)\s+\w+\s+(?<numRatings>[0-9]+)", RegexOptions.IgnoreCase);
                Match ratingMatch = ratingRegex.Match(ratingContent);

                if (ratingMatch.Success)
                {
                    decimal rating;
                    int numRatings;
                    if (ratingMatch.Groups["rating"].Value.TryParseLogging(out rating))
                    {
                        post.AvgRating = rating;
                    }

                    if (ratingMatch.Groups["numRatings"].Value.TryParseLogging(out numRatings))
                    {
                        post.NumOfRatings = numRatings;
                    }
                }
            }
            else
            {
                this._logger.ErrorFormat("ScrapingService, ScrapePostStatistics, Rating node is null - URL: {0}, NODE: {1}", post.Url, rootNode.SerializeHtmlNode());
            }

            /// num of comments
            HtmlNode numOfCommentsNode = rootNode.SelectSingleNode(PostPageXPath.NumOfComments);
            if (numOfCommentsNode != null &&
                !string.IsNullOrEmpty(numOfCommentsNode.InnerText) &&
                numOfCommentsNode.InnerText.StartsWith("(") &&
                numOfCommentsNode.InnerText.EndsWith(")"))
            {
                int numOfComments;
                string numOfCommentsString = numOfCommentsNode.InnerText.Replace("(", string.Empty).Replace(")", string.Empty);
                if (int.TryParse(numOfCommentsString, out numOfComments))
                {
                    post.NumOfComments = numOfComments;
                }
                else
                {
                    this._logger.WarnFormat("ScrapingService, ScrapePostStatistics, NumOfComments parsing: {2} - URL: {0}, NODE: {1}", post.Url, rootNode.SerializeHtmlNode(), numOfCommentsString);
                }
            }
            else
            {
                this._logger.ErrorFormat("ScrapingService, ScrapePostStatistics, NumOfComments - URL: {0}, NODE: {1}", post.Url, rootNode.SerializeHtmlNode());
            }

            /// FB social plugin
            // https://www.facebook.com/plugins/like.php?href=http%3A%2F%2Fwww.rtvslo.si%2Fsport%2Fodbojka%2Fvodebova-in-fabjanova-ubranili-naslov-drzavnih-prvakinj%2F314078&layout=button_count
            
            string fbUrlPattern = "http://www.facebook.com/plugins/like.php?href={0}&layout=button_count";
            string encodedUrl = HttpUtility.UrlEncode(post.Url);

            string fbUrl = string.Format(fbUrlPattern, encodedUrl);
            string fbPluginPage = this.CreateWebRequest(new Uri(fbUrl));

            if (!string.IsNullOrEmpty(fbPluginPage))
            {
                HtmlNode fbRootNode = fbPluginPage.CreateRootNode();
                if (fbRootNode != null)
                {
                    int fbLikes;

                    HtmlNode fbLikesNode = fbRootNode.SelectSingleNode(PostPageXPath.FbLikes);
                    if (fbLikesNode != null && !string.IsNullOrEmpty(fbLikesNode.InnerText) && int.TryParse(fbLikesNode.InnerText, out fbLikes))
                    {
                        post.NumOfFbLikes = fbLikes;
                    }
                    else
                    {
                        this._logger.ErrorFormat("ScrapingService, ScrapePostStatistics, FbLikes - POST URL: {0}, FB URL: {1}, NODE: {2}", post.Url, fbUrl, fbRootNode.SerializeHtmlNode());
                    }
                }
                else
                {
                    this._logger.ErrorFormat("ScrapingService, ScrapePostStatistics, FbLikes Node NULL - POST URL: {0}, FB URL: {1}, NODE: {2}", post.Url, fbUrl, fbRootNode.SerializeHtmlNode());
                }
            }
            

            /// Tweeter social plugin
            // http://platform.twitter.com/widgets/tweet_button.1375828408.html?url=http%3A%2F%2Fwww.rtvslo.si%2Fzabava%2Fiz-sveta-znanih%2Fboy-george-napadel-isinbajevo-zaradi-homofobnih-izjav%2F315495
            // http://cdn.api.twitter.com/1/urls/count.json?url=http%3A%2F%2Fwww.rtvslo.si%2Fzabava%2Fiz-sveta-znanih%2Fboy-george-napadel-isinbajevo-zaradi-homofobnih-izjav%2F315495&callback=twttr.receiveCount

            string twUrlPattern = "http://cdn.api.twitter.com/1/urls/count.json?url={0}";

            string twUrl = string.Format(twUrlPattern, encodedUrl);
            string twJsonPage = this.CreateWebRequest(new Uri(twUrl));

            try
            {
                JObject twJson = JObject.Parse(twJsonPage);
                string countString = (string)twJson["count"];

                int numOfTweets;
                if (!string.IsNullOrEmpty(countString) && int.TryParse(countString, out numOfTweets))
                {
                    post.NumOfTweets = numOfTweets;
                }
                else
                {
                    this._logger.ErrorFormat("ScrapingService, ScrapePostStatistics, NumOfTweets - POST URL: {0}, TW URL: {1}, NODE: {2}", post.Url, twUrl, twJsonPage);
                }
            }
            catch (JsonReaderException ex)
            {
                this._logger.ErrorFormat("ScrapingService, ScrapePostStatistics, NumOfTweets Parse EXCEPTION - POST URL: {0}, TW URL: {1}, NODE: {2}, EX:{3}", post.Url, twUrl, twJsonPage, ex.Message);
            }


            return post;
        }

        private Comment ScrapeComment(HtmlNode commentNode)
        {
            Comment comment = new Comment()
            {
                AccessedDate = DateTime.UtcNow.ToUniversalTime(),
            };

            commentNode.NullCheck();

            HtmlNode headerNode = commentNode.SelectSingleNode(CommentsPageXPath.HeaderInfo);
            IList<HtmlNode> innerHeaderNodes = headerNode.ChildNodes.Where(x => x.Name == "a").ToList();

            /// userUrl, url, username, id
            if (innerHeaderNodes != null && innerHeaderNodes.Count == 2)
            {
                if (innerHeaderNodes[0].Attributes["href"] != null)
                {
                    comment.UserUrl = innerHeaderNodes[0].Attributes["href"].Value.SafeTrim().ToFullRtvSloUrl();
                    comment.UserId = this.GetIdStringFromUrl(comment.UserUrl);
                }
                else
                {
                    this._logger.ErrorFormat("ScrapingService, ScrapeComment - User url is null - NODE: {0}", commentNode.SerializeHtmlNode());
                }

                comment.UserName = innerHeaderNodes[0].InnerText.SafeTrim();

                if (innerHeaderNodes[1].Attributes["href"] != null)
                {
                    comment.Url = innerHeaderNodes[1].Attributes["href"].Value.SafeTrim().ToFullRtvSloUrl();
                    comment.Id = this.GetIdFromUrl(comment.Url);
                }
                else
                {
                    this._logger.ErrorFormat("ScrapingService, ScrapeComment - Comment url is null - NODE: {0}", commentNode.SerializeHtmlNode());
                }
            }

            /// created date time
            string dateCreatedString = headerNode.LastChild.InnerText.SafeTrim();
            
            DateTime created;
            if (dateCreatedString.TryParseExactLogging(ParsingHelper.ShortDateTimeParseExactPattern, this.cultureInfo, DateTimeStyles.None, out created))
            {
                comment.DateCreated = created.ToUniversalTime();
            }

            HtmlNode contentNode = commentNode.SelectSingleNode(CommentsPageXPath.Content);

            if (contentNode != null)
            {
                string content = contentNode.InnerText.SafeTrimAndEscapeHtml();
                comment.Content = content;
            }
            else
            {
                this._logger.ErrorFormat("ScrapingService, ScrapeComment - Comment content is null - URL: {0}", comment.Url);
            }
           

            /// rating
            HtmlNode ratingNode = commentNode.SelectSingleNode(CommentsPageXPath.Rating);

            string plusRatingString = ratingNode.SelectSingleNode(CommentsPageXPath.PlusRating).InnerText.SafeTrim();
            string minusRatingString = ratingNode.SelectSingleNode(CommentsPageXPath.MinusRating).InnerText.SafeTrim();

            int plusRating = this.ScrapeCommentRating(plusRatingString, comment.Url);
            int minusRating = this.ScrapeCommentRating(minusRatingString, comment.Url);

            comment.Rating = plusRating + minusRating;

            return comment;
        }

        private int ScrapeCommentRating(string ratingContent, string commentUrl)
        {
            Regex ratingRegex = new Regex(@"(?<prefix>[\+\-])[\s]*(?<rating>[0-9]+)", RegexOptions.IgnoreCase);
            Match ratingMatch = ratingRegex.Match(ratingContent);

            if (ratingMatch.Success)
            {
                int rating = int.MinValue;
                if (ratingMatch.Groups["rating"].Value.TryParseLogging(out rating))
                {
                    if (ratingMatch.Groups["prefix"].Value == "+")
                    {
                        return rating;
                    }
                    else if (ratingMatch.Groups["prefix"].Value == "-")
                    {
                        return -rating;
                    }
                    else
                    {
                        this._logger.ErrorFormat("ScrapingService, ScrapeComment, Rating prefix - URL: {0}, NODE: {1}", commentUrl, ratingContent);
                    }
                }
            }
            else
            {
                /// comment has no rating
                this._logger.WarnFormat("ScrapingService, ScrapeComment - Comment rating regex fail - URL: {0}", commentUrl);            
            }

            return 0;
        }

        private User ScrapeUserRating(HtmlNode node, User user)
        {
            /// <div id="rate_text" style="font-weight:normal;color:#000000;font:9px Arial;">Ocena <strong>4.5</strong> od <strong>642</strong> glasov</div>

            node.NullCheck();
            user = user ?? new User();

            IList<HtmlNode> nodes = node.ChildNodes.Where(x => x.Name == "strong").ToList();

            if (nodes.Count != 2)
            {
                throw new IndexOutOfRangeException();
            }

            decimal rating = -1;
            int ratings = -1;
            if (!string.IsNullOrEmpty(nodes[0].InnerHtml) && decimal.TryParse(nodes[0].InnerHtml, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out rating))
            {
                user.Rating = rating;
            }

            if (!string.IsNullOrEmpty(nodes[1].InnerHtml) && int.TryParse(nodes[1].InnerHtml, out ratings))
            {
                user.NumOfRatings = ratings;
            }

            return user;
        }

        private User ScrapeUserData(HtmlNode node, User user)
        {
            node.NullCheck();
            user = user ?? new User();

            /// name
            HtmlNode nameNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPageName);
            if (nameNode != null && nameNode.NextSibling != null)
            {
                user.Name = nameNode.NextSibling.InnerText.SafeTrim();
            }

            /// email
            HtmlNode emailNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPageEmail);
            if (emailNode != null && emailNode.NextSibling != null && emailNode.NextSibling.NextSibling != null)
            {
                user.Email = emailNode.NextSibling.NextSibling.InnerText.SafeTrim();
            }

            /// gender
            HtmlNode genderNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPageGender);
            if (genderNode != null && genderNode.NextSibling != null)
            {
                string genderString = genderNode.NextSibling.InnerText.SafeTrim();
                if (genderString == ParsingHelper.GenderMale)
                {
                    user.Gender = UserGenderEnum.Male;
                }
                else if (genderString == ParsingHelper.GenderFemale)
                {
                    user.Gender = UserGenderEnum.Female;
                }
                else
                {
                    user.Gender = UserGenderEnum.NotSet;
                    this._logger.DebugFormat("ScrapeUserData, Gender not set - STRING: {0}", genderString);
                }
            }

            /// birthdate
            HtmlNode birthdateNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPageBirthDate);
            if (birthdateNode != null && birthdateNode.NextSibling != null)
            {
                DateTime birthdate;
                if (birthdateNode.NextSibling.InnerText.TryParseLogging(out birthdate))
                {
                    user.Birthdate = birthdate;
                }
            }

            Regex digitsRegex = new Regex(@"(?<digits>\d+)");

            /// forum posts
            HtmlNode forumPostsNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPageForumPosts);
            if (forumPostsNode != null && forumPostsNode.NextSibling != null)
            {
                Match match = digitsRegex.Match(forumPostsNode.NextSibling.InnerText);
                if (match.Success)
                {
                    user.ForumPosts = int.Parse(match.Groups["digits"].Value);
                }
            }

            /// blog posts
            HtmlNode blogPostsNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPageBlogPosts);
            if (blogPostsNode != null && blogPostsNode.NextSibling != null)
            {
                Match match = digitsRegex.Match(blogPostsNode.NextSibling.InnerText);
                if (match.Success)
                {
                    user.BlogPosts = int.Parse(match.Groups["digits"].Value);
                }
            }

            /// picture posts
            HtmlNode picturePostsNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPagePicturePosts);
            if (picturePostsNode != null && picturePostsNode.NextSibling != null)
            {
                Match match = digitsRegex.Match(picturePostsNode.NextSibling.InnerText);
                if (match.Success)
                {
                    user.PublishedPictures = int.Parse(match.Groups["digits"].Value);
                }
            }

            /// comment posts
            HtmlNode commentPostsNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPageCommentPosts);
            if (commentPostsNode != null && commentPostsNode.NextSibling != null)
            {
                Match match = digitsRegex.Match(commentPostsNode.NextSibling.InnerText);
                if (match.Success)
                {
                    user.PublishedComments = int.Parse(match.Groups["digits"].Value);
                }
            }

            /// video posts
            HtmlNode videoPostsNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPageVideoPosts);
            if (videoPostsNode != null && videoPostsNode.NextSibling != null)
            {
                Match match = digitsRegex.Match(videoPostsNode.NextSibling.InnerText);
                if (match.Success)
                {
                    user.PublishedVideos = int.Parse(match.Groups["digits"].Value);
                }
            }

            /// registered
            HtmlNode registeredNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPageRegistered);
            if (registeredNode != null && registeredNode.NextSibling != null)
            {
                DateTime registered;
                if (registeredNode.NextSibling.InnerText.TryParseLogging(out registered))
                {
                    user.DateCreated = registered;
                }
            }

            /// description
            HtmlNode descriptionNode = node.ChildNodes.FirstOrDefault(x => x.InnerText == ParsingHelper.UserPageDescription);
            if (descriptionNode != null && descriptionNode.NextSibling != null)
            {
                HtmlNode sibling = descriptionNode.NextSibling;
                StringBuilder description = new StringBuilder();

                do
                {
                    string text = sibling.InnerText.SafeTrim();
                    if (text.Length > 0)
                    {
                        description.AppendFormat("{0} ", text);
                    }

                    sibling = sibling.NextSibling;

                } while (sibling.NextSibling != null);

                user.Description = description.ToString().SafeTrim();
            }

            return user;
        }

        /// <summary>
        /// Get ID from url
        /// Example: /novice/komentarji/3923708
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private int GetIdFromUrl(string url)
        {
            url.NullOrEmptyCheck();

            try
            {
                int id = -1;
                if (int.TryParse(url.Substring(url.LastIndexOf("/") + 1), out id) && id != -1)
                {
                    return id;
                }
            }
            catch (Exception ex)
            {            
            }

            throw new ArgumentOutOfRangeException();
        }

        private string GetIdStringFromUrl(string url)
        {
            url.NullOrEmptyCheck();

            try
            {
                return url.Substring(url.LastIndexOf("/") + 1);
            }
            catch (Exception ex)
            {               
            }

            throw new ArgumentOutOfRangeException();
        }

        #endregion Helpers

        #endregion Private Methods
    }
}
