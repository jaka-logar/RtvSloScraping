using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using RtvSlo.Core.Entities.RtvSlo;

namespace RtvSlo.Visualization.Models.Home
{
    public class NewsFromRegionModel
    {
        [Display(Name = "From date")]
        public string DateFrom { get; set; }

        [Display(Name = "To date")]
        public string DateTo { get; set; }

        public IEnumerable<SelectListItem> AllRegions { get; set; }

        [Display(Name = "Select region")]
        public String SelectedRegion { get; set; }

        public IList<Post> Posts { get; set; }
    }
}