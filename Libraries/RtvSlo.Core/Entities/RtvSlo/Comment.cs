using System;

namespace RtvSlo.Core.Entities.RtvSlo
{
    public partial class Comment : BaseRtvSloEntity
    {
        public Comment()
        {
            this.AccessedDate = DateTime.UtcNow.ToUniversalTime();

            this.Id = -1;
            this.PostId = -1;
            this.Rating = -1;
        }

        public virtual int Id { get; set; }

        public virtual int PostId { get; set; }

        public virtual string UserId { get; set; }

        //public virtual string PostUrl { get; set; }

        public virtual string PostGuidUrl { get; set; }

        public virtual string UserUrl { get; set; }

        public virtual string UserGuidUrl { get; set; }

        public virtual string UserName { get; set; }

        public virtual string Content { get; set; }

        public virtual int Rating { get; set; }
    }
}
