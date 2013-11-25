using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Castle.Core.Logging;
using RtvSlo.Core.Configuration;
using RtvSlo.Core.Entities.RtvSlo;
using RtvSlo.Core.HelperEnums;
using RtvSlo.Core.HelperExtensions;
using RtvSlo.Core.HelperModels;
using RtvSlo.Core.Helpers;
using RtvSlo.Core.RdfPredicate;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Storage;

namespace RtvSlo.Services.Repository
{
    public partial class RepositoryService : IRepositoryService
    {
        #region Fields

        private readonly ILogger _logger;

        #endregion Fields

        #region Ctor

        public RepositoryService(
            ILogger logger
            )
        {
            this._logger = logger;
        }

        #endregion Ctor

        #region Public Methods

        #region Initialize repository

        /// <summary>
        /// Initialize repository
        /// Save site triples, role triples, new schema definition
        /// </summary>
        public void Initialize()
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT *
                    /// WHERE {
                    ///     ?s ?p sioc:Site
                    /// }    
                    SparqlResultSet queryResult = connector.QueryFormat("SELECT * WHERE {{ ?s ?p {0} }}", Predicate.SiocSite); //TODO optimize query syntax
                    if (queryResult == null)
                    {
                        return;
                    }
                    else if (!queryResult.Results.IsEmpty())
                    {
                        /// sioc:Site is already in repository
                        /// we assumed that repository is initialized
                        return;
                    }

                    /// clear repository
                    this.ClearRepository();

                    /// initialize repository
                    using (Graph g = new Graph())
                    {
                        g.BaseUri = RepositoryHelper.BaseUrl.ToUri();
                        
                        /// Namespaces
                        //foreach (Namespace value in RepositoryHelper.NamespaceDictionary.Values
                        //    .Where(x => x.Status == NamespaceStatusEnum.Internal))
                        //{
                        //    Uri uri = UriFactory.Create(string.Format("{0}", value.FullPath));
                        //    g.NamespaceMap.AddNamespace(value.Prefix, UriFactory.Create(value.FullPath));
                        //}

                        /// Site
                        g.Assert(RepositoryHelper.SiteUrl.ToUriNode(g), Predicate.RdfType.ToUriNode(g), Predicate.SiocSite.ToUriNode(g));
                        g.Assert(RepositoryHelper.SiteUrl.ToUriNode(g), Predicate.RdfsLabel.ToUriNode(g), RtvSloConfig.RtvSloName.ToLiteralNode(g));

                        /// Role
                        g.Assert(RepositoryHelper.JournalistRoleUrl.ToUriNode(g), Predicate.RdfType.ToUriNode(g), Predicate.SiocRole.ToUriNode(g));
                        g.Assert(RepositoryHelper.JournalistRoleUrl.ToUriNode(g), Predicate.NewsHasScope.ToUriNode(g), RepositoryHelper.SiteUrl.ToUriNode(g));

                        g.Assert(RepositoryHelper.ReaderRoleUrl.ToUriNode(g), Predicate.RdfType.ToUriNode(g), Predicate.SiocRole.ToUriNode(g));
                        g.Assert(RepositoryHelper.ReaderRoleUrl.ToUriNode(g), Predicate.NewsHasScope.ToUriNode(g), RepositoryHelper.SiteUrl.ToUriNode(g));

                        /// Gender
                        g.Assert(RepositoryHelper.CreateTypeTriple(g, Predicate.MmcGender, Predicate.RdfsClass));
                        g.Assert(RepositoryHelper.CreateDomainTriple(g, Predicate.NewsHasScope, Predicate.MmcGender));

                        g.Assert(RepositoryHelper.CreateTypeTriple(g, RepositoryHelper.GenderMaleUrl, Predicate.MmcGender));
                        g.Assert(RepositoryHelper.GenderMaleUrl.ToUriNode(g), Predicate.NewsHasScope.ToUriNode(g), RepositoryHelper.SiteUrl.ToUriNode(g));

                        g.Assert(RepositoryHelper.CreateTypeTriple(g, RepositoryHelper.GenderFemaleUrl, Predicate.MmcGender));
                        g.Assert(RepositoryHelper.GenderFemaleUrl.ToUriNode(g), Predicate.NewsHasScope.ToUriNode(g), RepositoryHelper.SiteUrl.ToUriNode(g));

                        /// Category
                        g.Assert(RepositoryHelper.CreateDomainTriple(g, Predicate.RdfsSeeAlso, Predicate.SiocCategory));

                        /// Post
                        g.Assert(RepositoryHelper.CreateTypeDomainTriples(g, Predicate.MmcSubtitle, Predicate.OwlDatatypeProperty, Predicate.SiocPost));
                        g.Assert(Predicate.MmcSubtitle.ToUriNode(g), Predicate.RdfsSubPropertyOf.ToUriNode(g), Predicate.DctTitle.ToUriNode(g));

                        g.Assert(RepositoryHelper.CreateTypeDomainRangeTriples(g, Predicate.MmcLastUpdated, Predicate.OwlDatatypeProperty, Predicate.SiocPost, Predicate.XsdDateTime));

                        /// Comment
                        g.Assert(RepositoryHelper.CreateTypeDomainRangeTriples(g, Predicate.MmcRating, Predicate.OwlDatatypeProperty, Predicate.NewsComment, Predicate.XsdInteger));

                        g.Assert(RepositoryHelper.CreateDomainTriple(g, Predicate.NewsId, Predicate.NewsComment));
                        g.Assert(RepositoryHelper.CreateDomainTriple(g, Predicate.RdfsSeeAlso, Predicate.NewsComment));
                        g.Assert(RepositoryHelper.CreateDomainTriple(g, Predicate.NewsAccessed, Predicate.NewsComment));

                        /// User
                        g.Assert(RepositoryHelper.CreateDomainTriple(g, Predicate.NewsId, Predicate.SiocUserAccount));
                        g.Assert(RepositoryHelper.CreateDomainTriple(g, Predicate.RdfsSeeAlso, Predicate.SiocUserAccount));
                        g.Assert(RepositoryHelper.CreateDomainTriple(g, Predicate.NewsAccessed, Predicate.SiocUserAccount));

                        g.Assert(RepositoryHelper.CreateTypeDomainTriples(g, Predicate.MmcEmail, Predicate.OwlDatatypeProperty, Predicate.SiocUserAccount));

                        g.Assert(RepositoryHelper.CreateTypeDomainRangeTriples(g, Predicate.MmcBirthDate, Predicate.OwlDatatypeProperty, Predicate.SiocUserAccount, Predicate.XsdDate));

                        g.Assert(RepositoryHelper.CreateTypeDomainTriples(g, Predicate.MmcAbout, Predicate.OwlDatatypeProperty, Predicate.SiocUserAccount));

                        g.Assert(RepositoryHelper.CreateTypeDomainRangeTriples(g, Predicate.MmcUserStatistics, Predicate.OwlObjectProperty, Predicate.SiocUserAccount, Predicate.MmcStatistics));

                        g.Assert(RepositoryHelper.CreateTypeDomainRangeTriples(g, Predicate.MmcHasGender, Predicate.OwlObjectProperty, Predicate.SiocUserAccount, Predicate.MmcGender));

                        /// Mmc Statistics
                        g.Assert(RepositoryHelper.CreateTypeTriple(g, Predicate.MmcStatistics, Predicate.RdfsClass));

                        g.Assert(RepositoryHelper.CreateDomainTriple(g, Predicate.NewsAvgRating, Predicate.MmcStatistics));
                        g.Assert(RepositoryHelper.CreateDomainTriple(g, Predicate.NewsNRatings, Predicate.MmcStatistics));

                        g.Assert(RepositoryHelper.CreateTypeDomainRangeTriples(g, Predicate.MmcNumOfForumPosts, Predicate.OwlDatatypeProperty, Predicate.MmcStatistics, Predicate.XsdInteger));

                        g.Assert(RepositoryHelper.CreateTypeDomainRangeTriples(g, Predicate.MmcNumOfBlogPosts, Predicate.OwlDatatypeProperty, Predicate.MmcStatistics, Predicate.XsdInteger));

                        g.Assert(RepositoryHelper.CreateTypeDomainRangeTriples(g, Predicate.MmcNumOfPublishedPictures, Predicate.OwlDatatypeProperty, Predicate.MmcStatistics, Predicate.XsdInteger));

                        g.Assert(RepositoryHelper.CreateTypeDomainRangeTriples(g, Predicate.MmcNumOfPublishedComments, Predicate.OwlDatatypeProperty, Predicate.MmcStatistics, Predicate.XsdInteger));

                        g.Assert(RepositoryHelper.CreateTypeDomainRangeTriples(g, Predicate.MmcNumOfPublishedVideos, Predicate.OwlDatatypeProperty, Predicate.MmcStatistics, Predicate.XsdInteger));

                        /// Save graph
                        connector.SaveGraph(g);
                    }
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, Initialize, SesameHttpProtocolConnector is not ready");
                }
            }
        }

        /// <summary>
        /// Clear repository - delete graph
        /// </summary>
        public void ClearRepository()
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    connector.DeleteGraph(RepositoryHelper.BaseUrl.ToUri());
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, Clear, SesameHttpProtocolConnector is not ready");
                }
            }
        }

        #endregion Initialize repository

        #region Post

        /// <summary>
        /// Get next post which hasn't been scraped and saved
        /// </summary>
        /// <returns>Post (url, id)</returns>
        public Post GetNextUnsavedPost()
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?url ?id
                    /// WHERE {
                    ///     ?post rdf:type sioc:Post .
                    ///     ?post rdfs:seeAlso ?url .
                    ///     ?post news:ID ?id
                    ///     MINUS { ?post dct:created ?date . }
                    /// }
                    /// LIMIT 1
                    SparqlResultSet queryResult = connector.QueryFormat("SELECT ?url ?id WHERE {{ ?post {4} {0} . ?post {1} ?url . ?post {3} ?id MINUS {{ ?post {2} ?date . }} }} LIMIT 1", 
                        Predicate.SiocPost, Predicate.RdfsSeeAlso, Predicate.DctCreated, Predicate.NewsId, Predicate.RdfType);

                    if (queryResult != null && !queryResult.Results.IsEmpty())
                    {
                        string url = queryResult.Results.First().Value("url").ToSafeString();
                        string idString = queryResult.Results.First().Value("id").ToSafeString();

                        this._logger.InfoFormat("RepositoryService, GetNextUnsavedPost - URL: {0}", url);

                        int id;
                        if (!int.TryParse(idString, out id))
                        {
                            this._logger.FatalFormat("RepositoryService, GetNextUnsavedPost, can't parse id - ID: {0}", idString);
                            return null;
                        }

                        return new Post()
                        {
                            Id = id,
                            Url = url
                        };
                    }
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, GetNextUnsavedPost, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        /// <summary>
        /// Get post which needs to be updated
        /// </summary>
        /// <returns></returns>
        public Post GetNextPostToUpdate()
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// filter post older than 2 weeks, sort filtered
                    DateTime olderThanDays = DateTime.Today.AddDays(-RtvSloConfig.Step3UpdateOlderThanDays);

                    /// SELECT ?url ?id ?date
                    /// WHERE {
                    ///     ?post rdf:type sioc:Post .
                    ///     ?post rdfs:seeAlso ?url .
                    ///     ?post news:ID ?id .
                    ///     ?post news:accessed ?date .
                    ///     FILTER ( ?date <= "2013-10-01 00:00:00Z")
                    /// }
                    /// ORDER BY ?date
                    /// LIMIT 1
                    string query = string.Format(
                        "SELECT ?url ?id ?date " +
                        "WHERE {{ ?post {4} {0} . " +
                        "?post {1} ?url . " +
                        "?post {3} ?id . " +
                        "?post {5} ?date " +
                        "FILTER (?date <= \"{6}\") " +
                        "}} " +
                        "ORDER BY ?date " +
                        "LIMIT 1",
                        Predicate.SiocPost, Predicate.RdfsSeeAlso, Predicate.DctCreated, Predicate.NewsId, Predicate.RdfType,
                        Predicate.NewsAccessed, olderThanDays.ToString(RepositoryHelper.DateTimeFormat), Predicate.XsdDateTime);

                    SparqlResultSet queryResult = connector.QueryFormat(query);

                    if (queryResult != null && !queryResult.Results.IsEmpty())
                    {
                        string url = queryResult.Results.First().Value("url").ToSafeString();
                        string idString = queryResult.Results.First().Value("id").ToSafeString();

                        this._logger.InfoFormat("RepositoryService, GetNextPostToUpdate - URL: {0}", url);

                        int id;
                        if (!int.TryParse(idString, out id))
                        {
                            this._logger.FatalFormat("RepositoryService, GetNextPostToUpdate, can't parse id - ID: {0}", idString);
                            return null;
                        }

                        return new Post()
                        {
                            Id = id,
                            Url = url
                        };
                    }
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, GetNextPostToUpdate, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        /// <summary>
        /// Save only post data scraped from archive page in RDF format if don't already exist
        /// ID, Title, Url, Category
        /// </summary>
        /// <param name="post"></param>
        /// <returns>Guid url</returns>
        public string SaveOrUpdatePostOverview(Post post)
        {
            /// save category
            string categoryUrl = this.CheckAndSaveCategories(post.Category);

            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?guidUrl ?predicate ?object
                    /// WHERE { 
                    ///     ?guidUrl rdf:type sioc:Post
                    ///     ; news:ID "id"
                    ///     ; ?predicate ?object
                    /// }
                    SparqlResultSet queryResult = connector.QueryFormat("SELECT ?guidUrl ?predicate ?object WHERE {{ ?guidUrl {0} {1} ; {2} \"{3}\" ; ?predicate ?object }}", 
                                                            Predicate.RdfType, Predicate.SiocPost, Predicate.NewsId, post.Id.ToString(), Predicate.SiocTopic);
                    /// update existing
                    if (queryResult != null && !queryResult.Results.IsEmpty())
                    {
                        using (IGraph g = new Graph())
                        {
                            g.BaseUri = RepositoryHelper.BaseUrl.ToUri();
                            IList<Triple> newTriples = new List<Triple>();

                            INode postGuid = queryResult.Results.First().Value("guidUrl").CopyNode(g);
                            post.RepositoryGuidUrl = ((UriNode)postGuid).Uri.AbsoluteUri;

                            /// select categories
                            IEnumerable<SparqlResult> categories = queryResult.Results
                                .Where(x => x.Value("predicate").ToSafeString() == Predicate.SiocTopic.ToFullNamespaceUrl());

                            SparqlResult categoryResult = categories.FirstOrDefault(x => x.Value("object").ToSafeString() == categoryUrl);
                            if (categoryResult == null)
                            {
                                newTriples.Add(new Triple(postGuid, Predicate.SiocTopic.ToUriNode(g), categoryUrl.ToUriNode(g)));
                            }

                            /// select url
                            IEnumerable<SparqlResult> postUrls = queryResult.Results
                                .Where(x => x.Value("predicate").ToSafeString() == Predicate.RdfsSeeAlso.ToFullNamespaceUrl());

                            SparqlResult postUrlResult = postUrls.FirstOrDefault(x => x.Value("object").ToSafeString() == post.Url);
                            if (postUrlResult == null)
                            {
                                newTriples.Add(new Triple(postGuid, Predicate.RdfsSeeAlso.ToUriNode(g), post.Url.ToUriNode(g)));
                            }

                            connector.UpdateGraph(g.BaseUri, newTriples, new List<Triple>());                         
                        }
                    }
                    /// save new
                    else
                    {
                        using (IGraph g = new Graph())
                        {
                            g.BaseUri = RepositoryHelper.BaseUrl.ToUri();
                            IList<Triple> newTriples = new List<Triple>();

                            string guidUrl = string.Format(RepositoryHelper.PostUrlPattern, Guid.NewGuid().ToString());
                            INode guidNode = guidUrl.ToUriNode(g);
                            post.RepositoryGuidUrl = guidUrl;

                            /// define post
                            newTriples.Add(new Triple(guidNode, Predicate.RdfType.ToUriNode(g), Predicate.SiocPost.ToUriNode(g)));
                            /// ID
                            newTriples.Add(new Triple(guidNode, Predicate.NewsId.ToUriNode(g), 
                                post.Id.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                            /// title
                            newTriples.Add(new Triple(guidNode, Predicate.DctTitle.ToUriNode(g), post.Title.ToLiteralNode(g)));
                            /// post url
                            newTriples.Add(new Triple(guidNode, Predicate.RdfsSeeAlso.ToUriNode(g), post.Url.ToUriNode(g)));

                            /// category
                            if (!string.IsNullOrEmpty(categoryUrl))
                            {
                                newTriples.Add(new Triple(guidNode, Predicate.SiocTopic.ToUriNode(g), categoryUrl.ToUriNode(g)));
                            }

                            connector.UpdateGraph(g.BaseUri, newTriples, new List<Triple>());
                        }
                    }

                    return post.RepositoryGuidUrl;
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, SavePostOverview, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        /// <summary>
        /// Save post details page in RDF format
        /// Updates post title
        /// </summary>
        /// <param name="post"></param>
        /// <param name="update"></param>
        /// <returns>Guid url</returns>
        public string SavePostDetails(Post post, bool update = false)
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?guidUrl ?predicate ?object
                    /// WHERE { 
                    ///     ?guidUrl rdf:type sioc:Post
                    ///     ; news:ID "id"
                    ///     ; ?predicate ?object
                    /// }
                    string query = string.Format("SELECT ?guidUrl ?predicate ?object WHERE {{ ?guidUrl {0} {1} ; {2} \"{3}\" ; ?predicate ?object }}",
                                                            Predicate.RdfType, Predicate.SiocPost, Predicate.NewsId, post.Id.ToString(), Predicate.SiocTopic);
                    SparqlResultSet queryResult = connector.QueryFormat(query);

                    if (queryResult == null || queryResult.Results.IsEmpty())
                    {
                        this._logger.FatalFormat("RepositoryService, SavePostDetails, Query result has no results - QUERY: {0}", query);
                        return null; ;
                    }

                    /// save
                    using (IGraph g = new Graph())
                    {
                        g.BaseUri = RepositoryHelper.BaseUrl.ToUri();
                        IList<Triple> newTriples = new List<Triple>();
                        IList<Triple> removeTriples = new List<Triple>();

                        INode postGuid = queryResult.Results.First().Value("guidUrl").CopyNode(g);
                        post.RepositoryGuidUrl = ((UriNode)postGuid).Uri.AbsoluteUri;

                        #region Post Content

                        /// remove old title
                        this.RemoveTriples(removeTriples, queryResult, g, postGuid, new string[] { Predicate.DctTitle });

                        if (!update)
                        {
                            /// published at
                            newTriples.Add(new Triple(postGuid, Predicate.NewsPublishedAt.ToUriNode(g), RepositoryHelper.SiteUrl.ToUriNode(g)));
                        }

                        /// date created
                        newTriples.Add(new Triple(postGuid, Predicate.DctCreated.ToUriNode(g), 
                            post.DateCreated.Value.ToString(RepositoryHelper.DateTimeFormat).ToLiteralNode(g, dataType: RepositoryHelper.DateTimeDataType)));

                        /// accessed date
                        newTriples.Add(new Triple(postGuid, Predicate.NewsAccessed.ToUriNode(g), 
                            post.AccessedDate.ToString(RepositoryHelper.DateTimeFormat).ToLiteralNode(g, dataType: RepositoryHelper.DateTimeDataType)));

                        /// title
                        newTriples.Add(new Triple(postGuid, Predicate.DctTitle.ToUriNode(g), post.Title.ToLiteralNode(g)));

                        /// subtitle
                        if (!string.IsNullOrEmpty(post.Subtitle))
                        {
                            newTriples.Add(new Triple(postGuid, Predicate.MmcSubtitle.ToUriNode(g), post.Subtitle.ToLiteralNode(g)));
                        }

                        /// abstract
                        if (!string.IsNullOrEmpty(post.Abstract))
                        {
                            newTriples.Add(new Triple(postGuid, Predicate.DctAbstract.ToUriNode(g), post.Abstract.ToLiteralNode(g)));
                        }

                        /// last updated
                        newTriples.Add(new Triple(postGuid, Predicate.MmcLastUpdated.ToUriNode(g), 
                            post.LastUpdated.ToString(RepositoryHelper.DateTimeFormat).ToLiteralNode(g, dataType: RepositoryHelper.DateTimeDataType)));

                        /// location
                        if (!string.IsNullOrEmpty(post.Location))
                        {
                            newTriples.Add(new Triple(postGuid, Predicate.NewsLocation.ToUriNode(g), 
                                post.Location.ToLiteralNode(g, language: RepositoryHelper.LanguageEnglish))); /// hack to get posts from region
                        }

                        /// content
                        if (!string.IsNullOrEmpty(post.Content))
                        {
                            newTriples.Add(new Triple(postGuid, Predicate.SiocContent.ToUriNode(g), post.Content.ToLiteralNode(g)));
                        }

                        /// authors
                        if (!post.Authors.IsEmpty())
                        {
                            foreach (User author in post.Authors)
                            {
                                if (!string.IsNullOrEmpty(author.RepositoryGuidUrl))
                                {
                                    newTriples.Add(new Triple(postGuid, Predicate.SiocHasCreator.ToUriNode(g), author.RepositoryGuidUrl.ToUriNode(g)));
                                }
                            }
                        }

                        if (update)
                        {
                            /// remove old triples
                            this.RemoveTriples(removeTriples, queryResult, g, postGuid,
                                new string[]{ Predicate.DctCreated, Predicate.NewsAccessed, Predicate.MmcSubtitle, Predicate.DctAbstract, Predicate.MmcLastUpdated,
                                   Predicate.NewsLocation, Predicate.SiocContent, Predicate.SiocHasCreator });
                        }

                        #endregion Post Content

                        #region Statistics

                        string statsGuid = Guid.NewGuid().ToString();
                        string statsGuidUrl = string.Format(RepositoryHelper.StatisticsUrlPattern, statsGuid);
                        UriNode statsGuidNode = null;

                        /// read existing statsGuidUrl
                        if (update)
                        {
                            statsGuidNode = queryResult.Results
                                .First(x => x.Value("predicate").ToSafeString() == Predicate.NewsStatistics.ToFullNamespaceUrl())
                                .Value("object") as UriNode;

                            statsGuidUrl = statsGuidNode.Uri.AbsoluteUri;

                            /// SELECT ?predicate ?object
                            /// WHERE {
                            ///     <guid> rdf:type news:Stat
                            ///     ; ?predicate ?object
                            /// }
                            query = string.Format("SELECT ?predicate ?object WHERE {{ <{0}> {1} {2} ; ?predicate ?object }}",
                                        statsGuidUrl, Predicate.RdfType, Predicate.NewsStat);

                            queryResult = connector.QueryFormat(query);

                            if (queryResult == null || queryResult.Results.IsEmpty())
                            {
                                this._logger.FatalFormat("RepositoryService, SavePostDetails, Update statistics ERROR - QUERY: {0}", query);
                            }
                        }

                        INode statsSubject = statsGuidNode != null ? statsGuidNode.CopyNode(g) : statsGuidUrl.ToUriNode(g);
                        if (!update)
                        {
                            /// initialize
                            newTriples.Add(new Triple(statsSubject, Predicate.RdfType.ToUriNode(g), Predicate.NewsStat.ToUriNode(g)));
                            newTriples.Add(new Triple(postGuid, Predicate.NewsStatistics.ToUriNode(g), statsSubject));
                        }

                        /// number of comments
                        if (post.NumOfComments > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.NewsNComments.ToUriNode(g), 
                                post.NumOfComments.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                        }

                        /// avgerage rating
                        if (post.AvgRating > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.NewsAvgRating.ToUriNode(g), 
                                post.AvgRating.ToString().ToLiteralNode(g, dataType: RepositoryHelper.DecimalDataType)));
                        }

                        /// number of ratings
                        if (post.NumOfRatings > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.NewsNRatings.ToUriNode(g), 
                                post.NumOfRatings.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                        }

                        /// number of FB likes
                        if (post.NumOfFbLikes > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.NewsNFBLikes.ToUriNode(g),
                                post.NumOfFbLikes.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                        }

                        /// number of tweets
                        if (post.NumOfTweets > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.NewsNTweets.ToUriNode(g),
                                post.NumOfTweets.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                        }

                        if (update)
                        {
                            /// remove old triples
                            this.RemoveTriples(removeTriples, queryResult, g, statsSubject,
                                new string[] { Predicate.NewsNComments, Predicate.NewsAvgRating, Predicate.NewsNRatings, Predicate.NewsNFBLikes, Predicate.NewsNTweets });
                        }

                        #endregion Statistics


                        connector.UpdateGraph(g.BaseUri, newTriples, removeTriples);
                        return post.RepositoryGuidUrl;
                    }
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, SavePostDetails, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        #endregion Post

        #region Comment

        /// <summary>
        /// Save new comment in RDF format
        /// </summary>
        /// <param name="comment"></param>
        /// <param name="update"></param>
        /// <returns>Guid url</returns>
        public string SaveComment(Comment comment, bool update = false)
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?guidUrl
                    /// WHERE {
                    ///     ?guidUrl rdf:type news:Comment
                    ///     ; news:ID "id"
                    /// } 
                    string query = string.Format("SELECT ?guidUrl WHERE {{ ?guidUrl {0} {1} ; {2} \"{3}\" }}",
                                                                    Predicate.RdfType, Predicate.NewsComment, Predicate.NewsId, comment.Id.ToSafeString());

                    if (update)
                    {
                        /// SELECT ?guidUrl ?predicate ?object
                        /// WHERE {
                        ///     ?guidUrl rdf:type news:Comment
                        ///     ; news:ID "id"
                        ///     ; ?predicate ?object
                        /// }
                        query = string.Format("SELECT ?guidUrl ?predicate ?object WHERE {{ ?guidUrl {0} {1} ; {2} \"{3}\" ; ?predicate ?object }}",
                                                                    Predicate.RdfType, Predicate.NewsComment, Predicate.NewsId, comment.Id.ToSafeString());
                    }

                    SparqlResultSet queryResult = connector.QueryFormat(query);

                    string guidUrl = null;
                    INode guidUrlNode = null;
                    if (queryResult != null && !queryResult.Results.IsEmpty())
                    {
                        /// comment already exist
                        guidUrlNode = queryResult.Results.First().Value("guidUrl");
                        guidUrl = ((UriNode)guidUrlNode).Uri.AbsoluteUri;
                        if (!update)
                        {
                            return guidUrl;
                        }
                    }

                    /// save new or update
                    using (IGraph g = new Graph())
                    {
                        g.BaseUri = RepositoryHelper.BaseUrl.ToUri();
                        IList<Triple> newTriples = new List<Triple>();
                        IList<Triple> removeTriples = new List<Triple>();

                        if (string.IsNullOrEmpty(guidUrl))
                        {
                            update = false;
                            guidUrl = string.Format(RepositoryHelper.CommentUrlPattern, Guid.NewGuid().ToString());
                        }

                        INode subject = guidUrlNode != null ? guidUrlNode.CopyNode(g) : guidUrl.ToUriNode(g);
                        comment.RepositoryGuidUrl = guidUrl;

                        /// save new
                        if (!update)
                        {
                            /// initialize
                            newTriples.Add(new Triple(subject, Predicate.RdfType.ToUriNode(g), Predicate.NewsComment.ToUriNode(g)));

                            /// ID
                            newTriples.Add(new Triple(subject, Predicate.NewsId.ToUriNode(g), 
                                comment.Id.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));

                            /// see also
                            newTriples.Add(new Triple(subject, Predicate.RdfsSeeAlso.ToUriNode(g), comment.Url.ToUriNode(g)));

                            /// link with post
                            if (!string.IsNullOrEmpty(comment.PostGuidUrl))
                            {
                                newTriples.Add(new Triple(comment.PostGuidUrl.ToUriNode(g), Predicate.SiocHasReply.ToUriNode(g), subject));
                            }
                            else
                            {
                                this._logger.FatalFormat("RepositoryService, SaveComment, PostGuidUrl IS NULL - COMMENT: {0}", comment.Url);
                            }

                            /// has creator
                            if (!string.IsNullOrEmpty(comment.UserGuidUrl))
                            {
                                newTriples.Add(new Triple(subject, Predicate.SiocHasCreator.ToUriNode(g), comment.UserGuidUrl.ToUriNode(g)));
                            }
                            else
                            {
                                this._logger.FatalFormat("RepositoryService, SaveComment, UserGuidUrl IS NULL - COMMENT: {0}", comment.Url);
                            }                      
                        }

                        /// accessed date
                        newTriples.Add(new Triple(subject, Predicate.NewsAccessed.ToUriNode(g), 
                            comment.AccessedDate.ToString(RepositoryHelper.DateTimeFormat).ToLiteralNode(g, dataType: RepositoryHelper.DateTimeDataType)));

                        /// content
                        newTriples.Add(new Triple(subject, Predicate.SiocContent.ToUriNode(g), comment.Content.ToLiteralNode(g)));

                        /// date created
                        newTriples.Add(new Triple(subject, Predicate.DctCreated.ToUriNode(g), 
                            comment.DateCreated.Value.ToString(RepositoryHelper.DateTimeFormat).ToLiteralNode(g, dataType: RepositoryHelper.DateTimeDataType)));

                        /// rating
                        newTriples.Add(new Triple(subject, Predicate.MmcRating.ToUriNode(g), 
                            comment.Rating.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));


                        /// remove old triples
                        if (update)
                        {
                            this.RemoveTriples(removeTriples, queryResult, g, subject,
                                new string[] { Predicate.NewsAccessed, Predicate.SiocContent, Predicate.DctCreated, Predicate.MmcRating });
                        }

                        /// save
                        connector.UpdateGraph(g.BaseUri, newTriples, removeTriples);
                        return guidUrl;
                    }
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, SaveComment, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        #endregion Comment

        #region User

        /// <summary>
        /// Search user by id (id is part of url)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="update"></param>
        /// <returns>Guid url</returns>
        public string SearchUserById(string id, bool update = false)
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?guidUrl
                    /// WHERE {
                    ///     ?guidUrl rdf:type sioc:UserAccount
                    ///     ; news:ID "id"
                    /// } 
                    string query = string.Format("SELECT ?guidUrl WHERE {{ ?guidUrl {0} {1} ; {2} \"{3}\" }}",
                                                                    Predicate.RdfType, Predicate.SiocUserAccount, Predicate.NewsId, id);

                    if (update)
                    {
                        /// filter users accessed older than 2 weeks
                        DateTime olderThanDays = DateTime.Today.AddDays(-RtvSloConfig.Step3UpdateOlderThanDays);

                        /// SELECT ?guidUrl
                        /// WHERE {
                        ///     ?guidUrl rdf:type sioc:UserAccount
                        ///     ; news:ID "id"
                        ///     ; news:accessed ?date
                        ///     FILTER ( ?date < "2013-10-01")
                        /// } 
                        query = string.Format(
                            "SELECT ?guidUrl " +
                            "WHERE {{ " +
                            "?guidUrl {0} {1} " +
                            "; {2} \"{3}\" " +
                            "; {4} ?date " +
                            "FILTER (?date < \"{5}\") " +
                            "}}",
                            Predicate.RdfType, Predicate.SiocUserAccount, Predicate.NewsId, id,
                            Predicate.NewsAccessed, olderThanDays.ToString(RepositoryHelper.DateTimeFormat));
                    }

                    SparqlResultSet queryResult = connector.QueryFormat(query);

                    if (queryResult == null)
                    {
                        this._logger.FatalFormat("RepositoryService, SearchUserById, Query result is null - QUERY: {0}", query);
                        return null; ;
                    }
                    else if (queryResult.Results.IsEmpty())
                    {
                        return null;
                    }

                    UriNode guidUrlNode = queryResult.Results.First().Value("guidUrl") as UriNode;
                    return guidUrlNode.Uri.AbsoluteUri;
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, SearchUserById, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        /// <summary>
        /// Search user by name (for posts author)
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Guid url</returns>
        public string SearchUserByName(string name)
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?guidUrl
                    /// WHERE {
                    ///     ?guidUrl rdf:type sioc:UserAccount
                    ///     ; news:nickname "name"
                    /// } 
                    string query = string.Format("SELECT ?guidUrl WHERE {{ ?guidUrl {0} {1} ; {2} \"{3}\" }}",
                                                                    Predicate.RdfType, Predicate.SiocUserAccount, Predicate.NewsNickname, name);
                    SparqlResultSet queryResult = connector.QueryFormat(query);

                    if (queryResult == null)
                    {
                        this._logger.FatalFormat("RepositoryService, SearchUserByName, Query result is null - QUERY: {0}", query);
                        return null; ;
                    }
                    else if (queryResult.Results.IsEmpty())
                    {
                        return null;
                    }

                    UriNode guidUrlNode = queryResult.Results.First().Value("guidUrl") as UriNode;
                    return guidUrlNode.Uri.AbsoluteUri;
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, SearchUserByName, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        /// <summary>
        /// Save new user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="update"></param>
        /// <returns>User guid url</returns>
        public string SaveUser(User user, bool update = false)
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?guidUrl
                    /// WHERE {
                    ///     ?guidUrl rdf:type sioc:UserAccount
                    ///     ; news:ID "id"
                    /// } 
                    string query = string.Format("SELECT ?guidUrl WHERE {{ ?guidUrl {0} {1} ; {2} \"{3}\" }}",
                                                Predicate.RdfType, Predicate.SiocUserAccount, Predicate.NewsId, user.Id);

                    if (update)
                    {
                        /// SELECT ?guidUrl ?predicate ?object
                        /// WHERE {
                        ///     ?guidUrl rdf:type sioc:UserAccount
                        ///     ; news:ID "id"
                        ///     ; ?predicate ?object
                        /// } 
                        query = string.Format("SELECT ?guidUrl ?predicate ?object WHERE {{ ?guidUrl {0} {1} ; {2} \"{3}\" ; ?predicate ?object }}",
                                                Predicate.RdfType, Predicate.SiocUserAccount, Predicate.NewsId, user.Id);
                    }

                    SparqlResultSet queryResult = connector.QueryFormat(query);

                    string guidUrl = null;
                    UriNode guidUrlNode = null;
                    if (queryResult != null && !queryResult.Results.IsEmpty())
                    {
                        /// user already exist
                        guidUrlNode = queryResult.Results.First().Value("guidUrl") as UriNode;
                        guidUrl = guidUrlNode.Uri.AbsoluteUri;
                        if (!update)
                        {
                            return guidUrl;
                        }
                    }

                    /// save new or update
                    using (IGraph g = new Graph())
                    {
                        g.BaseUri = RepositoryHelper.BaseUrl.ToUri();
                        IList<Triple> newTriples = new List<Triple>();
                        IList<Triple> removeTriples = new List<Triple>();

                        if (string.IsNullOrEmpty(guidUrl))
                        {
                            update = false;
                            guidUrl = string.Format(RepositoryHelper.UserUrlPattern, Guid.NewGuid().ToString());
                        }
                        INode subject = guidUrlNode != null ? guidUrlNode.CopyNode(g) : guidUrl.ToUriNode(g);
                        user.RepositoryGuidUrl = guidUrl;

                        #region User

                        if (!update)
                        {
                            /// initialize
                            newTriples.Add(new Triple(subject, Predicate.RdfType.ToUriNode(g), Predicate.SiocUserAccount.ToUriNode(g)));

                            /// user function
                            if (user.Function == UserFunctionEnum.Reader)
                            {
                                newTriples.Add(new Triple(subject, Predicate.SiocHasFunction.ToUriNode(g), RepositoryHelper.ReaderRoleUrl.ToUriNode(g)));
                            }
                            else if (user.Function == UserFunctionEnum.Journalist)
                            {
                                newTriples.Add(new Triple(subject, Predicate.SiocHasFunction.ToUriNode(g), RepositoryHelper.JournalistRoleUrl.ToUriNode(g)));
                            }
                        }

                        /// accessed date
                        newTriples.Add(new Triple(subject, Predicate.NewsAccessed.ToUriNode(g), 
                            user.AccessedDate.ToString(RepositoryHelper.DateTimeFormat).ToLiteralNode(g, dataType: RepositoryHelper.DateTimeDataType)));

                        /// ID
                        newTriples.Add(new Triple(subject, Predicate.NewsId.ToUriNode(g), user.Id.ToLiteralNode(g)));

                        /// Url
                        newTriples.Add(new Triple(subject, Predicate.RdfsSeeAlso.ToUriNode(g), user.Url.ToUriNode(g)));

                        /// gender
                        if (user.Gender == UserGenderEnum.Male)
                        {
                            newTriples.Add(new Triple(subject, Predicate.MmcHasGender.ToUriNode(g), RepositoryHelper.GenderMaleUrl.ToUriNode(g)));
                        }
                        else if (user.Gender == UserGenderEnum.Female)
                        {
                            newTriples.Add(new Triple(subject, Predicate.MmcHasGender.ToUriNode(g), RepositoryHelper.GenderFemaleUrl.ToUriNode(g)));
                        }

                        /// registered date
                        if (user.DateCreated.HasValue)
                        {
                            newTriples.Add(new Triple(subject, Predicate.DctCreated.ToUriNode(g), 
                                user.DateCreated.Value.ToString(RepositoryHelper.DateFormat).ToLiteralNode(g)));
                        }

                        /// nickname
                        if (!string.IsNullOrEmpty(user.Name))
                        {
                            newTriples.Add(new Triple(subject, Predicate.NewsNickname.ToUriNode(g), user.Name.ToLiteralNode(g)));
                        }

                        /// email
                        if (!string.IsNullOrEmpty(user.Email))
                        {
                            newTriples.Add(new Triple(subject, Predicate.MmcEmail.ToUriNode(g), user.Email.ToLiteralNode(g)));
                        }

                        /// birthdate
                        if (user.Birthdate.HasValue)
                        {
                            newTriples.Add(new Triple(subject, Predicate.MmcBirthDate.ToUriNode(g), 
                                user.Birthdate.Value.ToString(RepositoryHelper.DateFormat).ToLiteralNode(g, dataType: RepositoryHelper.DateDataType)));
                        }

                        /// about
                        if (!string.IsNullOrEmpty(user.Description))
                        {
                            newTriples.Add(new Triple(subject, Predicate.MmcAbout.ToUriNode(g), user.Description.ToLiteralNode(g)));
                        }

                        /// remove old triples
                        if (update)
                        {
                            this.RemoveTriples(removeTriples, queryResult, g, subject,
                                new string[] { Predicate.NewsAccessed, Predicate.NewsId, Predicate.RdfsSeeAlso, Predicate.MmcHasGender, Predicate.DctCreated, 
                                                Predicate.NewsNickname, Predicate.MmcEmail, Predicate.MmcBirthDate, Predicate.MmcAbout });
                        }

                        #endregion User

                        #region UserStatistics

                        string statsGuid = Guid.NewGuid().ToString();
                        string statsGuidUrl = string.Format(RepositoryHelper.UserStatisticsUrlPattern, statsGuid);
                        UriNode statsGuidNode = null;

                        /// read existing statsGuidUrl
                        if (update)
                        {
                            statsGuidNode = queryResult.Results
                                .First(x => x.Value("predicate").ToSafeString() == Predicate.MmcUserStatistics.ToFullNamespaceUrl())
                                .Value("object") as UriNode;

                            statsGuidUrl = statsGuidNode.Uri.AbsoluteUri;

                            /// SELECT ?predicate ?object
                            /// WHERE {
                            ///     <guid> rdf:type mmc:Statistics
                            ///     ; ?predicate ?object
                            /// }

                            query = string.Format("SELECT ?predicate ?object WHERE {{ <{0}> {1} {2} ; ?predicate ?object }}", 
                                        statsGuidUrl, Predicate.RdfType, Predicate.MmcStatistics);

                            queryResult = connector.QueryFormat(query);

                            if (queryResult == null || queryResult.Results.IsEmpty())
                            {
                                this._logger.FatalFormat("RepositoryService, SaveUser, Update statistics ERROR - QUERY: {0}", query);
                            }
                        }

                        INode statsSubject = statsGuidNode != null ? statsGuidNode.CopyNode(g) : statsGuidUrl.ToUriNode(g);

                        if(!update)
                        {
                            /// initialize
                            newTriples.Add(new Triple(statsSubject, Predicate.RdfType.ToUriNode(g), Predicate.MmcStatistics.ToUriNode(g)));
                            newTriples.Add(new Triple(subject, Predicate.MmcUserStatistics.ToUriNode(g), statsSubject));
                        }

                        /// rating
                        if (user.Rating > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.NewsAvgRating.ToUriNode(g),
                                user.Rating.ToString().ToLiteralNode(g, dataType: RepositoryHelper.DecimalDataType)));
                        }

                        /// num of ratings
                        if (user.NumOfRatings > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.NewsNRatings.ToUriNode(g),
                                user.NumOfRatings.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                        }

                        /// num of forum posts
                        if (user.ForumPosts > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.MmcNumOfForumPosts.ToUriNode(g),
                                user.ForumPosts.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                        }

                        /// num of blog posts
                        if (user.BlogPosts > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.MmcNumOfBlogPosts.ToUriNode(g),
                                user.BlogPosts.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                        }

                        /// num of published pictures
                        if (user.ForumPosts > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.MmcNumOfPublishedPictures.ToUriNode(g),
                                user.PublishedPictures.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                        }

                        /// num of published comments
                        if (user.PublishedComments > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.MmcNumOfPublishedComments.ToUriNode(g),
                                user.PublishedComments.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                        }

                        /// num of published videos
                        if (user.PublishedVideos > -1)
                        {
                            newTriples.Add(new Triple(statsSubject, Predicate.MmcNumOfPublishedVideos.ToUriNode(g),
                                user.PublishedVideos.ToString().ToLiteralNode(g, dataType: RepositoryHelper.IntegerDataType)));
                        }

                        /// remove old triples
                        if (update)
                        {
                            this.RemoveTriples(removeTriples, queryResult, g, statsSubject,
                                new string[] { Predicate.NewsAvgRating, Predicate.NewsNRatings, Predicate.MmcNumOfForumPosts, Predicate.MmcNumOfBlogPosts,
                                        Predicate.MmcNumOfPublishedPictures, Predicate.MmcNumOfPublishedComments, Predicate.MmcNumOfPublishedVideos });
                        }

                        #endregion UserStatistics


                        connector.UpdateGraph(g.BaseUri, newTriples, removeTriples);
                        return guidUrl;
                    }
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, SaveUser, SesameHttpProtocolConnector is not ready");
                }
            }
            
            return null;
        }

        /// <summary>
        /// Save post author to repository
        /// </summary>
        /// <param name="user"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public string SaveAuthor(User user, bool update = false)
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?guidUrl
                    /// WHERE {
                    ///     ?guidUrl rdf:type sioc:UserAccount
                    ///     ; news:nickname "name"
                    /// } 
                    string query = string.Format("SELECT ?guidUrl WHERE {{ ?guidUrl {0} {1} ; {2} \"{3}\" }}",
                                                                    Predicate.RdfType, Predicate.SiocUserAccount, Predicate.NewsNickname, user.Name);
                    
                    if (update)
                    {
                        /// SELECT ?guidUrl ?predicate ?object
                        /// WHERE {
                        ///     ?guidUrl rdf:type sioc:UserAccount
                        ///     ; news:nickname "name"
                        ///     ; ?predicate ?object
                        /// } 
                        query = string.Format("SELECT ?guidUrl ?predicate ?object WHERE {{ ?guidUrl {0} {1} ; {2} \"{3}\" ; ?predicate ?object }}",
                                                                    Predicate.RdfType, Predicate.SiocUserAccount, Predicate.NewsNickname, user.Name);
                    }

                    SparqlResultSet queryResult = connector.QueryFormat(query);

                    string guidUrl = null;
                    INode guidUrlNode = null;
                    if (queryResult != null && !queryResult.Results.IsEmpty())
                    {
                        /// user already exist
                        guidUrlNode = queryResult.Results.First().Value("guidUrl");
                        guidUrl = ((UriNode)guidUrlNode).Uri.AbsoluteUri;
                        if (!update)
                        {
                            return guidUrl;
                        }
                    }

                    /// save new or update
                    using (IGraph g = new Graph())
                    {
                        g.BaseUri = RepositoryHelper.BaseUrl.ToUri();
                        IList<Triple> newTriples = new List<Triple>();
                        IList<Triple> removeTriples = new List<Triple>();

                        if (string.IsNullOrEmpty(guidUrl))
                        {
                            update = false;
                            guidUrl = string.Format(RepositoryHelper.UserUrlPattern, Guid.NewGuid().ToString());
                        }

                        INode subject = guidUrlNode != null ? guidUrlNode.CopyNode(g) : guidUrl.ToUriNode(g);
                        user.RepositoryGuidUrl = guidUrl;

                        if (!update)
                        {
                            /// initialize
                            newTriples.Add(new Triple(subject, Predicate.RdfType.ToUriNode(g), Predicate.SiocUserAccount.ToUriNode(g)));

                            /// user function
                            if (user.Function == UserFunctionEnum.Reader)
                            {
                                newTriples.Add(new Triple(subject, Predicate.SiocHasFunction.ToUriNode(g), RepositoryHelper.ReaderRoleUrl.ToUriNode(g)));
                            }
                            else if (user.Function == UserFunctionEnum.Journalist)
                            {
                                newTriples.Add(new Triple(subject, Predicate.SiocHasFunction.ToUriNode(g), RepositoryHelper.JournalistRoleUrl.ToUriNode(g)));
                            }
                        }

                        /// accessed date
                        newTriples.Add(new Triple(subject, Predicate.NewsAccessed.ToUriNode(g),
                            user.AccessedDate.ToString(RepositoryHelper.DateTimeFormat).ToLiteralNode(g, dataType: RepositoryHelper.DateTimeDataType)));

                        /// nickname
                        newTriples.Add(new Triple(subject, Predicate.NewsNickname.ToUriNode(g), user.Name.ToLiteralNode(g)));


                        /// remove old triples
                        if (update)
                        {
                            this.RemoveTriples(removeTriples, queryResult, g, subject,
                                new string[] { Predicate.NewsAccessed, Predicate.NewsNickname });
                        }

                        connector.UpdateGraph(g.BaseUri, newTriples, removeTriples);
                        return guidUrl;
                    }
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, SaveAuthor, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        #endregion User

        #region Vizualization - Triple Story Query

        /// <summary>
        /// Get name of top categories by posts and number of posts in these categories
        /// </summary>
        /// <param name="fromDate">From date filtering</param>
        /// <param name="toDate">To date filtering</param>
        /// <param name="limit">Number of categories returned</param>
        /// <returns></returns>
        public IList<CategoryPostCount> GetTopCategoriesPostCount(DateTime? fromDate = null, DateTime? toDate = null, int limit = 10)
        {
            IList<CategoryPostCount> result = new List<CategoryPostCount>();

            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?category (COUNT(?post) as ?count)
                    /// WHERE {
                    ///     ?post a sioc:Post .
                    ///     ?post sioc:topic ?categoryGuid .
                    ///     ?categoryGuid rdfs:label ?category .
                    ///     ?post dct:created ?date .
                    ///     FILTER(?date >= "fromDate" && ?date <= "toDate")
                    /// }
                    /// GROUP BY ?category
                    /// ORDER BY DESC(?count)
                    /// LIMIT 10
                    StringBuilder query = new StringBuilder();
                    query.AppendFormat(
                        "SELECT ?category (COUNT(?post) as ?count) " +
                        "WHERE {{ " +
                        "?post rdf:type sioc:Post . " +
                        "?post sioc:topic ?categoryGuid . " +
                        "?categoryGuid rdfs:label ?category . ");

                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        query.AppendFormat(
                            "?post {0} ?date " +
                            "FILTER(?date >= \"{1}\" && ?date <= \"{2}\") ",
                            Predicate.DctCreated,
                            fromDate.Value.ToString(RepositoryHelper.DateTimeFormat),
                            toDate.Value.ToString(RepositoryHelper.DateTimeFormat));
                    }

                    query.AppendFormat(
                        "}} " +
                        "GROUP BY ?category " +
                        "ORDER BY DESC(?count) " +
                        "LIMIT {0}", limit);

                    SparqlResultSet queryResult = connector.QueryFormat(query.ToString());

                    if (queryResult == null)
                    {
                        this._logger.FatalFormat("RepositoryService, GetTopCategoriesPostCount, Query result is null - QUERY: {0}", query);
                        return null;
                    }
                    else if (queryResult.Results.IsEmpty())
                    {
                        return result;
                    }

                    foreach (SparqlResult sparqlResult in queryResult.Results)
                    {
                        CategoryPostCount model = new CategoryPostCount()
                        {
                            Category = sparqlResult.Value("category").ToSafeString()
                        };

                        LiteralNode ln = sparqlResult.Value("count") as LiteralNode;
                        if (ln != null)
                        {
                            model.PostCount = int.Parse(ln.Value);
                        }


                        result.Add(model);
                    }

                    return result;
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, GetTopCategoriesPostCount, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        /// <summary>
        /// Get top locations by post counts
        /// From DBpedia get abstract and coordinates
        /// </summary>
        /// <param name="fromDate">From date filtering</param>
        /// <param name="toDate">To date filtering</param>
        /// <param name="limit"></param>
        /// <param name="includeDBpedia"></param>
        /// <returns></returns>
        public IList<LocationInfo> GetTopLocations(DateTime? fromDate = null, DateTime? toDate = null, int limit = 10, bool includeDBpedia = true)
        {
            IList<LocationInfo> result = new List<LocationInfo>();

            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?location (COUNT(?post) AS ?count)
                    /// WHERE {
                    ///     ?post a sioc:Post .
                    ///     ?post news:location ?location .
                    ///     ?post dct:created ?date .
                    ///     FILTER(?date >= "fromDate" && ?date <= "toDate")
                    /// }
                    /// GROUP BY ?location
                    /// ORDER BY DESC(?count)
                    /// LIMIT 10
                    StringBuilder query = new StringBuilder();
                    query.AppendFormat(
                        "SELECT ?location (COUNT(?post) as ?count) " +
                        "WHERE {{ " +
                        "?post rdf:type sioc:Post . " +
                        "?post news:location ?location . ");

                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        query.AppendFormat(
                            "?post {0} ?date " +
                            "FILTER(?date >= \"{1}\" && ?date <= \"{2}\") ",
                            Predicate.DctCreated,
                            fromDate.Value.ToString(RepositoryHelper.DateTimeFormat),
                            toDate.Value.ToString(RepositoryHelper.DateTimeFormat));
                    }

                    query.AppendFormat(
                        "}} " +
                        "GROUP BY ?location " +
                        "ORDER BY DESC(?count) " +
                        "LIMIT {0}", limit);

                    SparqlResultSet queryResult = connector.QueryFormat(query.ToString());

                    if (queryResult == null)
                    {
                        this._logger.FatalFormat("RepositoryService, GetTopLocations, Query result is null - QUERY: {0}", query);
                        return null;
                    }
                    else if (queryResult.Results.IsEmpty())
                    {
                        return result;
                    }


                    foreach (SparqlResult sparqlResult in queryResult.Results)
                    {
                        LiteralNode ln;
                        LocationInfo model = new LocationInfo();

                        ln = sparqlResult.Value("location") as LiteralNode;
                        if (ln != null)
                        {
                            model.Name = ln.Value;
                        }

                        ln = sparqlResult.Value("count") as LiteralNode;
                        if (ln != null)
                        {
                            model.PostCount = int.Parse(ln.Value);
                        }

                        /// DBpedia
                        if (includeDBpedia)
                        {
                            /// SELECT ?desc ?lat ?long
                            /// WHERE {
                            ///     SERVICE <http://dbpedia.org/sparql> { 
                            ///         ?city rdf:type dbpedia-owl:Place .
                            ///         ?city foaf:name "Celje"@en .
                            ///         ?city dbpedia-owl:abstract ?desc .
                            ///         ?city geo:lat ?lat .
                            ///         ?city geo:long ?long
                            ///         FILTER(langMatches(lang(?desc ), "EN"))
                            ///     } 
                            /// }
                            query = new StringBuilder();
                            query.Append(
                                @"SELECT ?desc ?lat ?long " +
                                "WHERE { " +
                                "SERVICE <http://dbpedia.org/sparql> { " +
                                "?city rdf:type dbpedia-owl:Place . " +
                                "?city foaf:name \"" + model.Name + "\"@EN . " +
                                "?city dbpedia-owl:abstract ?desc . " +
                                "?city geo:lat ?lat . " +
                                "?city geo:long ?long " +
                                "FILTER(langMatches(lang(?desc ), \"EN\")) " +
                                "} " +
                                "}");

                            SparqlResultSet queryResultDBpedia = connector.QueryFormat(query.ToString());

                            if (queryResultDBpedia == null)
                            {
                                this._logger.FatalFormat("RepositoryService, GetTopLocations, DBpedia Query result is null - QUERY: {0}", query);
                            }
                            else if (!queryResultDBpedia.Results.IsEmpty())
                            {
                                SparqlResult dbpediaResult = queryResultDBpedia.Results.First();

                                ln = dbpediaResult.Value("desc") as LiteralNode;
                                if (ln != null)
                                {
                                    model.Description = ln.Value.ToSafeString();
                                }

                                ln = dbpediaResult.Value("lat") as LiteralNode;
                                if (ln != null)
                                {
                                    model.Latitude = float.Parse(ln.Value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat);
                                }

                                ln = dbpediaResult.Value("long") as LiteralNode;
                                if (ln != null)
                                {
                                    model.Longitude = float.Parse(ln.Value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat);
                                }
                            }
                        }

                        result.Add(model);
                    }

                    return result;
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, GetTopLocations, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        /// <summary>
        /// Get number of users by gender
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        public IList<UsersGenderCount> GetUsersGenderCount(DateTime? fromDate = null, DateTime? toDate = null)
        {
            IList<UsersGenderCount> result = new List<UsersGenderCount>();

            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?gender (COUNT(?user) AS ?count)
                    /// WHERE {
                    ///     ?user rdf:type sioc:UserAccount .
                    ///     ?user sioc:has_function mmc:roles/readerAtRtvslo .
                    ///     ?user mmc:has_gender ?gender .
                    ///     ?user dct:created ?date .
                    ///     FILTER(?date >= "fromDate" && ?date <= "toDate")
                    /// }
                    /// GROUP BY ?gender
                    StringBuilder query = new StringBuilder();
                    query.AppendFormat(
                        "SELECT ?gender (COUNT(?user) AS ?count) " +
                        "WHERE {{ " +
                        "?user rdf:type sioc:UserAccount . " +
                        "?user sioc:has_function <{0}> . " +
                        "?user mmc:has_gender ?gender . ",
                        RepositoryHelper.ReaderRoleUrl.ToFullNamespaceUrl());

                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        query.AppendFormat(
                            "?user {0} ?date . " +
                            "FILTER(?date >= \"{1}\" && ?date <= \"{2}\") ",
                            Predicate.DctCreated,
                            fromDate.Value.ToString(RepositoryHelper.DateTimeFormat),
                            toDate.Value.ToString(RepositoryHelper.DateTimeFormat));
                    }

                    query.AppendFormat(
                        "}} " +
                        "GROUP BY ?gender");
                    SparqlResultSet queryResult = connector.QueryFormat(query.ToString());

                    if (queryResult == null)
                    {
                        this._logger.FatalFormat("RepositoryService, GetUsersGenderCount, Query result is null - QUERY: {0}", query);
                    }
                    else if (!queryResult.Results.IsEmpty())
                    {
                        LiteralNode liCount;
                        UriNode liGender;
                        foreach (SparqlResult res in queryResult.Results)
                        {
                            liGender = res.Value("gender") as UriNode;
                            liCount = res.Value("count") as LiteralNode;
                            if (liGender != null && liCount != null)
                            {
                                result.Add(new UsersGenderCount()
                                {
                                    Gender = liGender.Uri.ToSafeString(),
                                    Count = int.Parse(liCount.Value)
                                });
                            }

                        }
                    }


                    /// SELECT (COUNT(?user) AS ?count)
                    /// WHERE {
                    ///     ?user rdf:type sioc:UserAccount .
                    ///     ?user sioc:has_function mmc:roles/readerAtRtvslo
                    ///     MINUS { ?user mmc:has_gender ?gender }
                    ///     
                    ///     ?user dct:created ?date .
                    ///     FILTER(?date >= "fromDate" && ?date <= "toDate")
                    /// }
                    query = new StringBuilder();
                    query.AppendFormat(
                        "SELECT (COUNT(?user) AS ?count) " +
                        "WHERE {{ " +
                        "?user rdf:type sioc:UserAccount . " +
                        "?user sioc:has_function <{0}> . " +
                        "MINUS {{ ?user mmc:has_gender ?gender }} ",
                        RepositoryHelper.ReaderRoleUrl.ToFullNamespaceUrl());

                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        query.AppendFormat(
                            "?user {0} ?date . " +
                            "FILTER(?date >= \"{1}\" && ?date <= \"{2}\") ",
                            Predicate.DctCreated,
                            fromDate.Value.ToString(RepositoryHelper.DateTimeFormat),
                            toDate.Value.ToString(RepositoryHelper.DateTimeFormat));
                    }

                    query.AppendFormat(
                        "}} ");
                    queryResult = connector.QueryFormat(query.ToString());

                    if (queryResult == null)
                    {
                        this._logger.FatalFormat("RepositoryService, GetUsersGenderCount, Query result is null - QUERY: {0}", query);
                    }
                    else if (!queryResult.Results.IsEmpty())
                    {
                        LiteralNode liCount;
                        SparqlResult res = queryResult.Results.First();
                        liCount = res.Value("count") as LiteralNode;
                        if (liCount != null)
                        {
                            result.Add(new UsersGenderCount()
                            {
                                Gender = "no_gender",
                                Count = int.Parse(liCount.Value)
                            });
                        }
                    }

                    return result;
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, GetUsersGenderCount, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        /// <summary>
        /// Get all statistical regions in Slovenia
        /// Gorenjska, Primorska ...
        /// </summary>
        /// <returns></returns>
        public IList<String> GetAllSlovenianRegions()
        {
            IList<String> result = new List<String>();

            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT ?label
                    /// WHERE {
                    ///	    SERVICE <http://dbpedia.org/sparql> {
                    ///		    ?region dbpedia-owl:type dbpedia:Statistical_regions_of_Slovenia .
                    ///		    ?region rdfs:label ?label .
                    ///		    FILTER(langMatches(lang(?label ), "NL"))
                    ///	    }
                    /// }

                    string query =
                        "SELECT ?label " +
                        "WHERE {{ " +
                        "SERVICE <http://dbpedia.org/sparql> {{ " +
                        "?region dbpedia-owl:type dbpedia:Statistical_regions_of_Slovenia . " +
                        "?region rdfs:label ?label . " +
                        "FILTER(langMatches(lang(?label ), \"NL\")) " +
                        "}} " +
                        "}}";

                    SparqlResultSet queryResult = connector.QueryFormat(query);

                    if (queryResult == null)
                    {
                        this._logger.FatalFormat("RepositoryService, GetAllSlovenianRegions, Query result is null - QUERY: {0}", query);
                    }
                    else if (!queryResult.Results.IsEmpty())
                    {
                        LiteralNode literalNode;
                        foreach (SparqlResult res in queryResult.Results)
                        {
                            literalNode = res.Value("label") as LiteralNode;
                            if (literalNode != null)
                            {
                                result.Add(literalNode.Value);
                            }

                        }
                    }

                    return result;
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, GetAllSlovenianRegions, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        /// <summary>
        /// Get posts from selected region in Slovenia
        /// </summary>
        /// <param name="region"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        public IList<Post> GetPostsFromRegion(string region, DateTime? fromDate = null, DateTime? toDate = null)
        {
            region.NullCheck();

            IList<Post> result = new List<Post>();

            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    /// SELECT DISTINCT ?post ?locationName ?seeAlso
                    /// WHERE {
	                ///    ?post a sioc:Post .
	                ///    ?post news:location ?locationName .
                    ///    ?post rdfs:seeAlso ?seeAlso .
                    ///    ?post dct:created ?date .
	                ///
	                ///    SERVICE <http://dbpedia.org/sparql> {
		            ///        ?region dbpedia-owl:type dbpedia:Statistical_regions_of_Slovenia .
		            ///        ?region rdfs:label "Gorenjska"@NL .
		            ///
		            ///        { ?city ?x ?region } UNION { ?region ?z ?city }
		            ///        ?city ?y ?locationName
	                ///    }
                    ///    FILTER(langMatches(lang(?locationName ), "EN") && ?date >= "fromDate" && ?date <= "toDate")
                    /// }

                    StringBuilder query = new StringBuilder();
                    query.AppendFormat(
                        "SELECT DISTINCT ?post ?locationName ?seeAlso " +
                        "WHERE {{ " +
                        "?post rdf:type sioc:Post . " +
                        "?post news:location ?locationName . " +
                        "?post rdfs:seeAlso ?seeAlso . ");

                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        query.AppendFormat(
                            "?post {0} ?date . ",
                            Predicate.DctCreated);
                    }

                    query.AppendFormat(
                        "SERVICE <http://dbpedia.org/sparql> {{ " +
                        "?region dbpedia-owl:type dbpedia:Statistical_regions_of_Slovenia . " +
                        "?region rdfs:label \"{0}\"@NL . " +
                        "{{ ?city ?x ?region }} UNION {{ ?region ?z ?city }} " +
                        "?city ?y ?locationName " +
                        "}} ",
                        region);

                    query.AppendFormat(
                        "FILTER(langMatches(lang(?locationName ), \"EN\")");

                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        query.AppendFormat(
                            " && ?date >= \"{0}\" && ?date <= \"{1}\"",
                            fromDate.Value.ToString(RepositoryHelper.DateTimeFormat),
                            toDate.Value.ToString(RepositoryHelper.DateTimeFormat));
                    }

                    query.AppendFormat(
                        ") }}");

                    SparqlResultSet queryResult = connector.QueryFormat(query.ToString());

                    if (queryResult == null)
                    {
                        this._logger.FatalFormat("RepositoryService, GetAllSlovenianRegions, Query result is null - QUERY: {0}", query);
                    }
                    else if (!queryResult.Results.IsEmpty())
                    {
                        LiteralNode literalNode;
                        UriNode uriNode;
                        foreach (SparqlResult res in queryResult.Results)
                        {
                            Post p = new Post();

                            uriNode = res.Value("post") as UriNode;
                            if (uriNode != null)
                            {
                                p.RepositoryGuidUrl = uriNode.Uri.AbsoluteUri;
                            }

                            uriNode = res.Value("seeAlso") as UriNode;
                            if (uriNode != null)
                            {
                                p.Url = uriNode.Uri.AbsoluteUri;
                            }

                            literalNode = res.Value("locationName") as LiteralNode;
                            if (literalNode != null)
                            {
                                p.Location = literalNode.Value;
                            }

                            result.Add(p);
                        }
                    }

                    return result;
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, GetAllSlovenianRegions, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        #endregion Vizualization - Triple Story Query

        #endregion Public Methods

        #region Private Methods

        #region Category

        /// <summary>
        /// Check if categories already exist in repository and save them if not
        /// </summary>
        /// <param name="category"></param>
        /// <returns>Child category repository url</returns>
        private string CheckAndSaveCategories(Category category)
        {
            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    Category tempCategory = category;
                    if (category.HasChild)
                    {
                        tempCategory = category.LastChild;
                    }

                    /// SELECT ?url
                    /// WHERE {
                    ///     ?url rdf:type sioc:Category .
                    ///     ?url rdfs:seeAlso <cat_url> .
                    /// }
                    string queryPattern = "SELECT ?url WHERE {{ ?url {0} {1} . ?url {2} <{3}> . }}";
                    SparqlResultSet result = connector.QueryFormat(queryPattern, Predicate.RdfType, Predicate.SiocCategory, Predicate.RdfsSeeAlso, tempCategory.Url);
                    if (result == null)
                    {
                        return null;
                    }
                    else if (!result.Results.IsEmpty())
                    {
                        /// child category already exists
                        return result.Results.First().Value("url").ToSafeString();
                    }

                    string categoryRepositoryUrl = null;

                    /// save category
                    using (IGraph g = new Graph())
                    {
                        g.BaseUri = RepositoryHelper.BaseUrl.ToUri();

                        IList<Triple> newTriples = new List<Triple>();

                        tempCategory = category;

                        bool topCategory = true;
                        do
                        {
                            /// fetch and check if category exists
                            result = connector.QueryFormat(queryPattern, Predicate.RdfType, Predicate.SiocCategory, Predicate.RdfsSeeAlso, tempCategory.Url);
                            if (result == null)
                            {
                                return null;
                            }
                            else if (result.Results.IsEmpty())
                            {

                                categoryRepositoryUrl = string.Format(RepositoryHelper.CategoryUrlPattern, tempCategory.Label);
                                INode subject = categoryRepositoryUrl.ToUriNode(g);

                                newTriples.Add(new Triple(subject, Predicate.RdfType.ToUriNode(g), Predicate.SiocCategory.ToUriNode(g)));
                                newTriples.Add(new Triple(subject, Predicate.RdfsSeeAlso.ToUriNode(g), tempCategory.Url.ToUriNode(g)));
                                newTriples.Add(new Triple(subject, Predicate.RdfsLabel.ToUriNode(g), tempCategory.Label.ToLiteralNode(g)));

                                if (!topCategory)
                                {
                                    INode parentObject = string.Format(RepositoryHelper.CategoryUrlPattern, tempCategory.Parent.Label).ToUriNode(g);
                                    newTriples.Add(new Triple(subject, Predicate.NewsSubCategoryOf.ToUriNode(g), parentObject));
                                }
                            }

                            topCategory = false;
                            tempCategory = tempCategory.NextChild;
                        }
                        while (tempCategory != null);

                        /// save new category
                        connector.UpdateGraph(g.BaseUri, newTriples, new List<Triple>());
                    }

                    return categoryRepositoryUrl;
                }
                else
                {
                    this._logger.FatalFormat("RepositoryService, CheckAndSaveCategories, SesameHttpProtocolConnector is not ready");
                }
            }

            return null;
        }

        #endregion Category

        /// <summary>
        /// Add triples with predicates provided in list to remove
        /// </summary>
        /// <param name="removeTriples"></param>
        /// <param name="queryResult"></param>
        /// <param name="g"></param>
        /// <param name="subject"></param>
        /// <param name="removePredicates"></param>
        /// <param name="predicate"></param>
        /// <param name="obj"></param>
        private void RemoveTriples(
            IList<Triple> removeTriples, 
            SparqlResultSet queryResult,
            IGraph g,
            INode subject,
            string[] removePredicates, 
            string predicate = "predicate",
            string obj = "object")
        {
            removeTriples = removeTriples ?? new List<Triple>();

            if (!removePredicates.IsEmpty() && queryResult != null && !queryResult.Results.IsEmpty())
            {
                IEnumerable<SparqlResult> triples;

                foreach (string pr in removePredicates)
                {
                    triples = queryResult.Results
                        .Where(x => x.Value(predicate).ToSafeString() == pr.ToFullNamespaceUrl());

                    if (!triples.IsEmpty())
                    {
                        foreach (SparqlResult sr in triples)
                        {
                            removeTriples.Add(new Triple(
                                subject,
                                pr.ToUriNode(g),
                                sr.Value("object").CopyNode(g)));
                        }
                    }
                }
            }
        }

        #endregion Private Methods
    }
}
