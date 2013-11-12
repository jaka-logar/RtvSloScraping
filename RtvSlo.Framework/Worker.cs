using System;
using System.Collections.Generic;
using System.Threading;
using Castle.Core.Logging;
using RtvSlo.Core.Configuration;
using RtvSlo.Core.Entities.RtvSlo;
using RtvSlo.Core.HelperExtensions;
using RtvSlo.Core.Infrastructure.Windsor;
using RtvSlo.Services.Repository;
using RtvSlo.Services.Scraping;

namespace RtvSlo.Framework
{
    public class Worker
    {
        #region Constants

        private static readonly TimeSpan STEP1_WAIT_TIMEOUT = TimeSpan.FromMinutes(RtvSloConfig.Step1InactiveTimeoutMinutes);
        private static readonly TimeSpan STEP2_WAIT_TIMEOUT = TimeSpan.FromMinutes(RtvSloConfig.Step2InactiveTimeoutMinutes);
        private static readonly TimeSpan STEP3_WAIT_TIMEOUT = TimeSpan.FromMinutes(RtvSloConfig.Step3InactiveTimeoutMinutes);

        #endregion Constants

        #region Fields

        private readonly IScrapingService _scrapingService;
        private readonly IRepositoryService _repositoryService;
        private readonly ILogger _logger;

        private Timer step1Timer;
        private Timer step2Timer;
        private Timer step3Timer;

        private Mutex step1Mutex;
        private Mutex step2Mutex;
        private Mutex step3Mutex;

        #endregion Fields

        #region Ctor

        public Worker()
        {
            this._scrapingService = DependencyContainer.Instance.Resolve<IScrapingService>();
            this._repositoryService = DependencyContainer.Instance.Resolve<IRepositoryService>();
            this._logger = DependencyContainer.Instance.Resolve<ILogger>();

            this.step1Mutex = new Mutex();
            this.step2Mutex = new Mutex();
            this.step3Mutex = new Mutex();
        }

        #endregion Ctor

        #region Public Methods

        #region Debug

        public void RunDebug()
        {
            //this._repositoryService.ClearRepository();
            //this._repositoryService.Initialize();

            //User user1 = this._scrapingService.ScrapeUserPage(new Uri("http://www.rtvslo.si/profil/veselo-na-delo"));
            //if (user1 != null)
            //{
            //    this._logger.DebugFormat("Simulator, RunStep2 - USER: {0}", user1.SerializeObject());
            //    string userGuid = this._repositoryService.SaveUser(user1);
            //}
            //else
            //{
            //    //this._logger.WarnFormat("Simulator, RunStep2, Scrape user unsuccessfull - USER_URL: {0}, USER: {1}", comment.UserUrl, user.SerializeObject());
            //}

            Post post1 = new Post()
            {
                Url = "http://www.rtvslo.si/slovenija/jankovic-cestital-bratuskovi-za-napoved-da-bo-kandidirala-za-predsednico-ps-ja/317891"
            };
            post1 = this._scrapingService.ScrapePostPage(new Uri(post1.Url), post1);

            int startPage = 0;
            string html = this._scrapingService.GetFilteredArchivePage(startPage);

            bool hasNextPage = true;

            while (hasNextPage && !string.IsNullOrEmpty(html))
            {
                /// scrape archive page
                IList<Post> posts = this._scrapingService.ScrapeArhivePage(html, out hasNextPage);

                if (!posts.IsEmpty())
                {
                    foreach (Post post in posts)
                    {
                        /// save post from archive page
                        this._repositoryService.SaveOrUpdatePostOverview(post);

                        /// scrape details page
                        Post newPost = this._scrapingService.ScrapePostPage(new Uri(post.Url), post);
                        this._logger.DebugFormat("Simulator, RunStep2 - POST: {0}", post.SerializeObject());

                        foreach (User author in post.Authors)
                        {
                            string authorGuid = this._repositoryService.SearchUserByName(author.Name);
                            if (string.IsNullOrEmpty(authorGuid))
                            {
                                authorGuid = this._repositoryService.SaveAuthor(author);
                                if (!string.IsNullOrEmpty(authorGuid))
                                {
                                    author.RepositoryGuidUrl = authorGuid;
                                }
                            }
                            else
                            {
                                author.RepositoryGuidUrl = authorGuid;
                            }
                        }

                        /// save details page
                        string postGuid = this._repositoryService.SavePostDetails(post);

                        /// check if save was successsfull
                        if (string.IsNullOrEmpty(postGuid))
                        {
                            this._logger.FatalFormat("Simulator, RunStep2, SavePostDetails unsuccessfull - POST_GUID: {0}, POST: {1}", postGuid, post.SerializeObject());
                            continue;
                        }

                        /// post comments
                        int commentsPage = 0;
                        string commentsHtml = this._scrapingService.GetCommentsPage(post.Id, commentsPage++);

                        while (!string.IsNullOrEmpty(commentsHtml))
                        {
                            IList<Comment> comments = this._scrapingService.ScrapeCommentsPage(commentsHtml, post);
                            if (comments.IsEmpty())
                            {
                                break;
                            }

                            foreach (Comment comment in comments)
                            {
                                comment.PostGuidUrl = postGuid;

                                string userGuid = this._repositoryService.SearchUserById(comment.UserId);
                                if (string.IsNullOrEmpty(userGuid))
                                {
                                    User user = this._scrapingService.ScrapeUserPage(new Uri(comment.UserUrl));
                                    if (user != null)
                                    {
                                        this._logger.DebugFormat("Simulator, RunStep2 - USER: {0}", user.SerializeObject());
                                        userGuid = this._repositoryService.SaveUser(user);
                                    }
                                    else
                                    {
                                        this._logger.WarnFormat("Simulator, RunStep2, Scrape user unsuccessfull - USER_URL: {0}, USER: {1}", comment.UserUrl, user.SerializeObject());
                                        continue;
                                    }
                                }

                                comment.UserGuidUrl = userGuid;

                                this._logger.DebugFormat("Simulator, RunStep2 - COMMENT: {0}", comment.SerializeObject());
                                this._repositoryService.SaveComment(comment);
                            }

                            /// get next page of comments
                            commentsHtml = this._scrapingService.GetCommentsPage(post.Id, commentsPage++);
                        }
                    }
                }

                /// load next page
                html = this._scrapingService.GetFilteredArchivePage(startPage++);
            }
        }

        #endregion Debug

        #region Timers

        /// <summary>
        /// Start timers
        /// </summary>
        public void StartTimers()
        {
            this._logger.DebugFormat("Worker -> StartTimers invoked");

            this.step1Timer = new Timer(
                Step1TimerElapsed,
                null,
                TimeSpan.Zero,
                STEP1_WAIT_TIMEOUT);

            this.step2Timer = new Timer(
                Step2TimerElapsed,
                null,
                STEP2_WAIT_TIMEOUT,
                STEP2_WAIT_TIMEOUT);

            this.step3Timer = new Timer(
                Step3TimerElapsed,
                null,
                STEP3_WAIT_TIMEOUT,
                STEP3_WAIT_TIMEOUT);

            
        }

        private void Step1TimerElapsed(object state)
        {
            if (!step1Mutex.WaitOne(TimeSpan.Zero))
            {
                return;
            }

            try
            {
                this.RunStep1();
            }
            catch (Exception ex)
            {
                this._logger.FatalFormatStack("ERROR during STEP1: {0}", ex.Message);
            }
            finally
            {
                step1Mutex.ReleaseMutex();
            }
        }

        private void Step2TimerElapsed(object state)
        {
            if (!step2Mutex.WaitOne(TimeSpan.Zero))
            {
                return;
            }

            try
            {
                this.RunStep2();
            }
            catch (Exception ex)
            {
                this._logger.FatalFormatStack("ERROR during STEP2: {0}", ex.Message);
            }
            finally
            {
                step2Mutex.ReleaseMutex();
            }
        }

        private void Step3TimerElapsed(object state)
        {
            if (!step3Mutex.WaitOne(TimeSpan.Zero))
            {
                return;
            }

            try
            {
                this.RunStep3();
            }
            catch (Exception ex)
            {
                this._logger.FatalFormatStack("ERROR during STEP3: {0}", ex.Message);
            }
            finally
            {
                step3Mutex.ReleaseMutex();
            }
        }

        #endregion Timers

        /// <summary>
        /// Run Step 1 -> save post overview
        /// </summary>
        public void RunStep1()
        {
            this._logger.DebugFormat("TEST RUN STEP 1");

            /// initialize repository
            //this._repositoryService.ClearRepository();
            this._repositoryService.Initialize();


            int startPage = 0;
            string html = this._scrapingService.GetFilteredArchivePage(startPage++);

            bool hasNextPage = true;

            while (hasNextPage && !string.IsNullOrEmpty(html))
            {
                /// scrape archive page
                IList<Post> posts = this._scrapingService.ScrapeArhivePage(html, out hasNextPage);

                if (!posts.IsEmpty())
                {
                    foreach (Post post in posts)
                    {
                        this._logger.DebugFormat("Simulator, RunStep1 - POST: {0}", post.SerializeObject());
                        this._repositoryService.SaveOrUpdatePostOverview(post);
                    }
                }

                /// load next page
                html = this._scrapingService.GetFilteredArchivePage(startPage++);
            }
        }

        /// <summary>
        /// Run Step 2 -> save post details, comments and comments author
        /// </summary>
        public void RunStep2()
        {
            this._logger.DebugFormat("TEST RUN STEP 2");

            Post post = this._repositoryService.GetNextUnsavedPost();
            while (post != null)
            {
                /// scrape and save post details page
                post = this._scrapingService.ScrapePostPage(new Uri(post.Url), post);
                this._logger.DebugFormat("Simulator, RunStep2 - POST: {0}", post.SerializeObject());

                if (post != null)
                {
                    foreach (User author in post.Authors)
                    {
                        string authorGuid = this._repositoryService.SearchUserByName(author.Name);
                        if (string.IsNullOrEmpty(authorGuid))
                        {
                            authorGuid = this._repositoryService.SaveAuthor(author);
                            if (!string.IsNullOrEmpty(authorGuid))
                            {
                                author.RepositoryGuidUrl = authorGuid;
                            }
                        }
                        else
                        {
                            author.RepositoryGuidUrl = authorGuid;
                        }
                    }

                    string postGuid = this._repositoryService.SavePostDetails(post);

                    /// check if save was successsfull
                    if (string.IsNullOrEmpty(postGuid))
                    {
                        this._logger.FatalFormat("Simulator, RunStep2, SavePostDetails unsuccessfull - POST_GUID: {0}, POST: {1}", postGuid, post.SerializeObject());
                        continue;
                    }

                    /// post comments
                    int commentsPage = 0;
                    string commentsHtml = this._scrapingService.GetCommentsPage(post.Id, commentsPage++);

                    while (!string.IsNullOrEmpty(commentsHtml))
                    {
                        IList<Comment> comments = this._scrapingService.ScrapeCommentsPage(commentsHtml, post);
                        if (comments.IsEmpty())
                        {
                            break;
                        }

                        foreach (Comment comment in comments)
                        {
                            comment.PostGuidUrl = postGuid;

                            string userGuid = this._repositoryService.SearchUserById(comment.UserId);
                            if (string.IsNullOrEmpty(userGuid))
                            {
                                User user = this._scrapingService.ScrapeUserPage(new Uri(comment.UserUrl));
                                if (user != null)
                                {
                                    this._logger.DebugFormat("Simulator, RunStep2 - USER: {0}", user.SerializeObject());
                                    userGuid = this._repositoryService.SaveUser(user);
                                }
                                else
                                {
                                    this._logger.WarnFormat("Simulator, RunStep2, Scrape user unsuccessfull - USER_URL: {0}, USER: {1}", comment.UserUrl, user.SerializeObject());
                                    continue;
                                }
                            }

                            comment.UserGuidUrl = userGuid;

                            this._logger.DebugFormat("Simulator, RunStep2 - COMMENT: {0}", comment.SerializeObject());
                            this._repositoryService.SaveComment(comment);
                        }

                        /// get next page of comments
                        commentsHtml = this._scrapingService.GetCommentsPage(post.Id, commentsPage++);
                    }
                }

                post = this._repositoryService.GetNextUnsavedPost();
            }
        }

        /// <summary>
        /// Run Step 3 -> update triples saved in step 2
        /// </summary>
        public void RunStep3()
        {
            this._logger.DebugFormat("TEST RUN STEP 3");

            Post post = this._repositoryService.GetNextPostToUpdate();
            while (post != null)
            {
                /// scrape and save post details page
                post = this._scrapingService.ScrapePostPage(new Uri(post.Url), post);
                this._logger.DebugFormat("Simulator, RunStep3 - POST: {0}", post.SerializeObject());

                if (post != null)
                {
                    foreach (User author in post.Authors)
                    {
                        string authorGuid = this._repositoryService.SaveAuthor(author, update: true);
                        if (!string.IsNullOrEmpty(authorGuid))
                        {
                            author.RepositoryGuidUrl = authorGuid;
                        }
                    }

                    string postGuid = this._repositoryService.SavePostDetails(post, update: true);

                    /// check if save was successsfull
                    if (string.IsNullOrEmpty(postGuid))
                    {
                        this._logger.FatalFormat("Simulator, RunStep3, SavePostDetails unsuccessfull - POST_GUID: {0}, POST: {1}", postGuid, post.SerializeObject());
                        continue;
                    }

                    /// post comments
                    int commentsPage = 0;
                    string commentsHtml = this._scrapingService.GetCommentsPage(post.Id, commentsPage++);

                    while (!string.IsNullOrEmpty(commentsHtml))
                    {
                        IList<Comment> comments = this._scrapingService.ScrapeCommentsPage(commentsHtml, post);
                        if (comments.IsEmpty())
                        {
                            break;
                        }

                        foreach (Comment comment in comments)
                        {
                            comment.PostGuidUrl = postGuid;

                            string userGuid = this._repositoryService.SearchUserById(comment.UserId, update: true);
                            if (string.IsNullOrEmpty(userGuid))
                            {
                                User user = this._scrapingService.ScrapeUserPage(new Uri(comment.UserUrl));
                                if (user != null)
                                {
                                    this._logger.DebugFormat("Simulator, RunStep3 - USER: {0}", user.SerializeObject());
                                    userGuid = this._repositoryService.SaveUser(user, update: true);
                                }
                                else
                                {
                                    this._logger.WarnFormat("Simulator, RunStep3, Scrape user unsuccessfull - USER_URL: {0}, USER: {1}", comment.UserUrl, user.SerializeObject());
                                    continue;
                                }
                            }

                            comment.UserGuidUrl = userGuid;

                            this._logger.DebugFormat("Simulator, RunStep3 - COMMENT: {0}", comment.SerializeObject());
                            this._repositoryService.SaveComment(comment, update: true);
                        }

                        /// get next page of comments
                        commentsHtml = this._scrapingService.GetCommentsPage(post.Id, commentsPage++);
                    }
                }

                post = this._repositoryService.GetNextPostToUpdate();
            }
        }

        #endregion Public Methods

        #region Private Methods


        #endregion Private Methods
    }
}
