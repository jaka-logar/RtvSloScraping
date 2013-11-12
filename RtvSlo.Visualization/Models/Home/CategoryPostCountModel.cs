using System.ComponentModel.DataAnnotations;

namespace RtvSlo.Visualization.Models.Home
{
    public class CategoryPostCountModel
    {
        [Display(Name="From date")]
        public string DateFrom { get; set; }

        [Display(Name="To date")]
        public string DateTo { get; set; }

        
        public string JsonPie { get; set; }
        public string JsonBarCategory { get; set; }
        public string JsonBarCount { get; set; }
    }
}