using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RtvSlo.Core.HelperModels;

namespace RtvSlo.Visualization.Models.Home
{
    public class GoogleMapsLocationsModel
    {
        [Display(Name = "From date")]
        public string DateFrom { get; set; }

        [Display(Name = "To date")]
        public string DateTo { get; set; }

        public IList<LocationInfo> Locations { get; set; }
    }
}