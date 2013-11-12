using System.ComponentModel.DataAnnotations;

namespace RtvSlo.Visualization.Models.Home
{
    public class UsersGenderCountModel
    {
        [Display(Name = "From date")]
        public string DateFrom { get; set; }

        [Display(Name = "To date")]
        public string DateTo { get; set; }

        public string JsonPie { get; set; }
    }
}