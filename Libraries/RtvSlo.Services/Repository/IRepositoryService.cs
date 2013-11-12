using System;
using System.Collections.Generic;
using RtvSlo.Core.Entities.RtvSlo;
using RtvSlo.Core.HelperModels;

namespace RtvSlo.Services.Repository
{
    public partial interface IRepositoryService
    {
        #region Initialize repository

        /// <summary>
        /// Initialize repository
        /// Save site triples, role triples, new schema definition
        /// </summary>
        void Initialize();

        /// <summary>
        /// Clear repository - delete graph
        /// </summary>
        void ClearRepository();

        #endregion Initialize repository

        #region Post

        /// <summary>
        /// Get next post which hasn't been scraped and saved
        /// </summary>
        /// <returns>Post (url, id)</returns>
        Post GetNextUnsavedPost();

        /// <summary>
        /// Get post which needs to be updated
        /// </summary>
        /// <returns></returns>
        Post GetNextPostToUpdate();

        /// <summary>
        /// Save only post data scraped from archive page in RDF format if don't already exist
        /// ID, Title, Url, Category
        /// </summary>
        /// <param name="post"></param>
        /// <returns>Guid url</returns>
        string SaveOrUpdatePostOverview(Post post);

        /// <summary>
        /// Save post details page in RDF format
        /// Updates post title
        /// </summary>
        /// <param name="post"></param>
        /// <param name="update"></param>
        /// <returns>Guid url</returns>
        string SavePostDetails(Post post, bool update = false);

        #endregion Post

        #region Comment

        /// <summary>
        /// Save new comment in RDF format
        /// </summary>
        /// <param name="comment"></param>
        /// <param name="update"></param>
        /// <returns>Guid url</returns>
        string SaveComment(Comment comment, bool update = false);

        #endregion Comment

        #region User

        /// <summary>
        /// Search user by id (id is part of url)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="update"></param>
        /// <returns>Guid url</returns>
        string SearchUserById(string id, bool update = false);

        /// <summary>
        /// Search user by name (for posts author)
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Guid url</returns>
        string SearchUserByName(string name);

        /// <summary>
        /// Save new user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="update"></param>
        /// <returns>User guid url</returns>
        string SaveUser(User user, bool update = false);

        /// <summary>
        /// Save post author to repository
        /// </summary>
        /// <param name="user"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        string SaveAuthor(User user, bool update = false);

        #endregion User

        #region Vizualization - Triple Story Query

        /// <summary>
        /// Get name of top categories by posts and number of posts in these categories
        /// </summary>
        /// <param name="fromDate">From date filtering</param>
        /// <param name="toDate">To date filtering</param>
        /// <param name="limit">Number of categories returned</param>
        /// <returns></returns>
        IList<CategoryPostCount> GetTopCategoriesPostCount(DateTime? fromDate = null, DateTime? toDate = null, int limit = 10);

        /// <summary>
        /// Get top locations by post counts
        /// From DBpedia get abstract and coordinates
        /// </summary>
        /// <param name="fromDate">From date filtering</param>
        /// <param name="toDate">To date filtering</param>
        /// <param name="limit"></param>
        /// <param name="includeDBpedia"></param>
        /// <returns></returns>
        IList<LocationInfo> GetTopLocations(DateTime? fromDate = null, DateTime? toDate = null, int limit = 10, bool includeDBpedia = true);

        /// <summary>
        /// Get number of users by gender
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        IList<UsersGenderCount> GetUsersGenderCount(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get all statistical regions in Slovenia
        /// Gorenjska, Primorska ...
        /// </summary>
        /// <returns></returns>
        IList<String> GetAllSlovenianRegions();

        /// <summary>
        /// Get posts from selected region in Slovenia
        /// </summary>
        /// <param name="region"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        IList<Post> GetPostsFromRegion(string region, DateTime? fromDate = null, DateTime? toDate = null);

        #endregion Vizualization - Triple Story Query
    }
}
