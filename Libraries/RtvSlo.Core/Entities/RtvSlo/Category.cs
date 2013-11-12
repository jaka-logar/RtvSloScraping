using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RtvSlo.Core.Entities.RtvSlo
{
    public partial class Category
    {
        public virtual string Url { get; set; }

        public virtual string Label { get; set; }

        public virtual string Name { get; set; }

        private Category ChildCategory { get; set; }

        private Category ParentCategory { get; set; }


        #region Public Methods

        public bool HasChild
        {
            get { return this.ChildCategory != null; }
        }

        public Category LastChild
        {
            get
            {
                Category tempCategory = this.ChildCategory;
                if (tempCategory == null)
                {
                    return null;
                }

                while (tempCategory.ChildCategory != null)
                {
                    tempCategory = tempCategory.ChildCategory;
                }

                return tempCategory;
            }
        }

        public Category NextChild
        {
            get
            {
                return this.ChildCategory;
            }
        }

        public Category Parent
        {
            get
            {
                return this.ParentCategory;
            }
        }

        public void SaveChildCategory(Category category)
        {
            /// first child
            if (!this.HasChild)
            {
                category.Url = string.Format("{0}{1}/", this.Url, category.Label);
                this.ChildCategory = category;
                category.ParentCategory = this;
                return;
            }

            Category tempCategory = this.ChildCategory;
            
            while (tempCategory.ChildCategory != null)
            {
                tempCategory = tempCategory.ChildCategory;
            }

            category.Url = string.Format("{0}{1}/", tempCategory.Url, category.Label);
            category.ParentCategory = tempCategory;
            tempCategory.ChildCategory = category;

        }

        #endregion Public Methods
    }
}
