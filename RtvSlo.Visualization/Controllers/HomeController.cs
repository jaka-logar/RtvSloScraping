using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Castle.Core.Logging;
using RtvSlo.Core.Entities.RtvSlo;
using RtvSlo.Core.HelperExtensions;
using RtvSlo.Core.HelperModels;
using RtvSlo.Core.Infrastructure.Windsor;
using RtvSlo.Services.Repository;
using RtvSlo.Visualization.Models.Home;

namespace RtvSlo.Visualization.Controllers
{
    public class HomeController : Controller
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IRepositoryService _repositoryService;

        #endregion Fields

        #region Ctor

        public HomeController()
        {
            this._logger = DependencyContainer.Instance.Resolve<ILogger>();
            this._repositoryService = DependencyContainer.Instance.Resolve<IRepositoryService>();
        }

        #endregion Ctor

        #region Public Methods

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CategoryPostCount()
        {
            DateTime fromDate = new DateTime(2013, 1, 1);
            DateTime toDate = new DateTime(2014, 1, 1);
            CategoryPostCountModel model = new CategoryPostCountModel()
            {
                DateFrom = fromDate.ToShortDateString(),
                DateTo = toDate.ToShortDateString()
            };

            IList<CategoryPostCount> categories = this._repositoryService.GetTopCategoriesPostCount(fromDate: fromDate, toDate: toDate);
            this.PrepareCategoryPostCountModel(model, categories);

            return View(model);
        }

        [HttpPost]
        public ActionResult CategoryPostCount(CategoryPostCountModel model)
        {
            DateTime? fromDate = this.TryParseDateTime(model.DateFrom);
            DateTime? toDate = this.TryParseDateTime(model.DateTo);

            IList<CategoryPostCount> categories = this._repositoryService.GetTopCategoriesPostCount(fromDate: fromDate, toDate: toDate);
            this.PrepareCategoryPostCountModel(model, categories);

            return View(model);
        }

        public ActionResult GoogleMapsPostLocations()
        {
            DateTime fromDate = new DateTime(2013, 1, 1);
            DateTime toDate = new DateTime(2014, 1, 1);
            GoogleMapsLocationsModel model = new GoogleMapsLocationsModel()
            {
                DateFrom = fromDate.ToShortDateString(),
                DateTo = toDate.ToShortDateString()
            };

            IList<LocationInfo> locations = this._repositoryService.GetTopLocations(fromDate: fromDate, toDate: toDate);
            this.PrepareGoogleMapsLocationsModel(model, locations);

            return View(model);
        }

        [HttpPost]
        public ActionResult GoogleMapsPostLocations(GoogleMapsLocationsModel model)
        {
            DateTime? fromDate = this.TryParseDateTime(model.DateFrom);
            DateTime? toDate = this.TryParseDateTime(model.DateTo);

            IList<LocationInfo> locations = this._repositoryService.GetTopLocations(fromDate: fromDate, toDate: toDate);
            this.PrepareGoogleMapsLocationsModel(model, locations);

            return View(model);
        }

        public ActionResult UsersGenderCount()
        {
            DateTime fromDate = new DateTime(2000, 1, 1);
            DateTime toDate = DateTime.Today;
            UsersGenderCountModel model = new UsersGenderCountModel()
            {
                DateFrom = fromDate.ToShortDateString(),
                DateTo = toDate.ToShortDateString()
            };

            IList<UsersGenderCount> genders = this._repositoryService.GetUsersGenderCount(fromDate: fromDate, toDate: toDate);
            this.PrepareUsersGenderCountModel(model, genders);

            return View(model);
        }

        [HttpPost]
        public ActionResult UsersGenderCount(UsersGenderCountModel model)
        {
            DateTime? fromDate = this.TryParseDateTime(model.DateFrom);
            DateTime? toDate = this.TryParseDateTime(model.DateTo);

            IList<UsersGenderCount> genders = this._repositoryService.GetUsersGenderCount(fromDate: fromDate, toDate: toDate);
            this.PrepareUsersGenderCountModel(model, genders);

            return View(model);
        }

        public ActionResult NewsFromRegion()
        {
            DateTime fromDate = new DateTime(2013, 1, 1);
            DateTime toDate = new DateTime(2014, 1, 1);
            NewsFromRegionModel model = new NewsFromRegionModel()
            {
                DateFrom = fromDate.ToShortDateString(),
                DateTo = toDate.ToShortDateString()
            };

            IList<String> regions = this._repositoryService.GetAllSlovenianRegions();
            this.PrepareNewsFromRegionModel(model, regions);

            return View(model);
        }

        [HttpPost]
        public ActionResult NewsFromRegion(NewsFromRegionModel model)
        {
            DateTime? fromDate = this.TryParseDateTime(model.DateFrom);
            DateTime? toDate = this.TryParseDateTime(model.DateTo);

            IList<String> regions = this._repositoryService.GetAllSlovenianRegions();
            this.PrepareNewsFromRegionModel(model, regions);

            IList<Post> posts = this._repositoryService.GetPostsFromRegion(model.SelectedRegion, fromDate: fromDate, toDate: toDate);
            model.Posts = posts;

            return View(model);
        }

        #endregion Public Methods

        #region Private Methods

        private void PrepareCategoryPostCountModel(CategoryPostCountModel model, IList<CategoryPostCount> categories)
        {
            model = model ?? new CategoryPostCountModel();

            if (!categories.IsEmpty())
            {
                StringBuilder sbPie = new StringBuilder(@"[");
                StringBuilder sbBarCount = new StringBuilder(@"[");
                StringBuilder sbBarCat = new StringBuilder(@"[");
                foreach (CategoryPostCount m in categories)
                {
                    m.Category = HttpUtility.JavaScriptStringEncode(string.Format("{0} ({1})", m.Category, m.PostCount));

                    sbPie.AppendFormat(@"['{0}', {1}],", m.Category, m.PostCount);

                    sbBarCount.AppendFormat(@"{0}, ", m.PostCount);
                    sbBarCat.AppendFormat(@"'{0}', ", m.Category);
                }
                sbPie.Append(@"]");
                sbBarCount.Append(@"]");
                sbBarCat.Append(@"]");

                model.JsonPie = sbPie.ToString();
                model.JsonBarCount = sbBarCount.ToString();
                model.JsonBarCategory = sbBarCat.ToString();
            }
        }

        private void PrepareGoogleMapsLocationsModel(GoogleMapsLocationsModel model, IList<LocationInfo> locations)
        {
            model = model ?? new GoogleMapsLocationsModel();

            if (!locations.IsEmpty())
            {
                /// filter only locations with coordinates
                locations = locations.Where(x => x.Longitude.HasValue && x.Latitude.HasValue).ToList();

                foreach (LocationInfo li in locations)
                {
                    li.Name = HttpUtility.JavaScriptStringEncode(li.Name);
                    li.Description = HttpUtility.JavaScriptStringEncode(li.Description);
                }

                model.Locations = locations;
            }
        }

        private void PrepareUsersGenderCountModel(UsersGenderCountModel model, IList<UsersGenderCount> genders)
        {
            model = model ?? new UsersGenderCountModel();

            if (!genders.IsEmpty())
            {
                StringBuilder sbPie = new StringBuilder(@"[");

                foreach (UsersGenderCount g in genders)
                {
                    g.Gender = HttpUtility.JavaScriptStringEncode(string.Format("{0} ({1})", g.Gender, g.Count));

                    sbPie.AppendFormat(@"['{0}', {1}],", g.Gender, g.Count);
                }

                sbPie.Append(@"]");

                model.JsonPie = sbPie.ToString();
            }
        }

        private void PrepareNewsFromRegionModel(NewsFromRegionModel model, IList<String> regions)
        {
            model = model ?? new NewsFromRegionModel();

            if (!regions.IsEmpty())
            {
                model.AllRegions = regions.Select(x =>
                {
                    var m = new SelectListItem()
                    {
                        Text = x,
                        Value = x,
                        Selected = !String.IsNullOrEmpty(model.SelectedRegion) && model.SelectedRegion == x
                    };

                    return m;
                });
            }
        }

        private DateTime? TryParseDateTime(string s)
        {
            DateTime tempDate = DateTime.MinValue;
            DateTime? date = null;

            if (DateTime.TryParse(s, out tempDate))
            {
                date = tempDate;
            }

            return date;
        }

        #endregion Private Methods
    }
}
