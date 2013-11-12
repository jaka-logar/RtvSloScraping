using System;
using RtvSlo.Core.HelperEnums;

namespace RtvSlo.Core.Entities.RtvSlo
{
    public partial class User : BaseRtvSloEntity
    {
        public User()
        {
            this.AccessedDate = DateTime.UtcNow.ToUniversalTime();
            this.PublishedComments = -1;
            this.PublishedPictures = -1;
            this.BlogPosts = -1;
            this.ForumPosts = -1;
            this.Rating = -1;
            this.NumOfRatings = -1;
            this.PublishedVideos = -1;
        }

        public virtual string Id { get; set; }

        public virtual UserFunctionEnum Function { get; set; }

        public virtual string Name { get; set; }

        public virtual string Email { get; set; }

        public virtual UserGenderEnum Gender { get; set; }

        public virtual DateTime? Birthdate { get; set; }

        public virtual decimal Rating { get; set; }

        public virtual int NumOfRatings { get; set; }

        public virtual int ForumPosts { get; set; }

        public virtual int BlogPosts { get; set; }

        public virtual int PublishedPictures { get; set; }

        public virtual int PublishedComments { get; set; }

        public virtual int PublishedVideos { get; set; }

        public virtual string Description { get; set; }
    }
}
