using System;

namespace RtvSlo.Core.Entities
{
    public abstract class BaseRtvSloEntity
    {
        public virtual string Url { get; set; }

        public virtual string RepositoryGuidUrl { get; set; }

        public virtual DateTime? DateCreated { get; set; }

        public virtual DateTime AccessedDate { get; set; }
    }
}
