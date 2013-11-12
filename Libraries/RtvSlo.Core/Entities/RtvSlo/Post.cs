using System;
using System.Collections.Generic;

namespace RtvSlo.Core.Entities.RtvSlo
{
    public partial class Post : BaseRtvSloEntity
    {
        public Post()
        {
            this.AccessedDate = DateTime.UtcNow.ToUniversalTime();
            this.Authors = new List<User>();
            this.Category = new Category();
            
            this.Id = -1;
            this.AvgRating = -1;
            this.NumOfComments = -1;
            this.NumOfFbLikes = -1;
            this.NumOfRatings = -1;
            this.NumOfTweets = -1;
        }

        public virtual int Id { get; set; }

        #region Category

        public virtual Category Category { get; set; }

        #endregion Category

        #region Post Content

        public virtual string Title { get; set; }

        public virtual string Subtitle { get; set; }

        public virtual string Abstract { get; set; }

        public virtual DateTime LastUpdated { get; set; }

        public virtual string Location { get; set; }

        public virtual string Content { get; set; }

        public virtual List<User> Authors { get; set; }

        #endregion Post Content

        #region Statistics

        public virtual decimal AvgRating { get; set; }

        public virtual int NumOfRatings { get; set; }

        public virtual int NumOfFbLikes { get; set; }

        public virtual int NumOfTweets { get; set; }

        public virtual int NumOfComments { get; set; }

        #endregion Statistics
    }
}
