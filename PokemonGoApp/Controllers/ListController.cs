using System.Collections.Generic;
using System.Web.Mvc;
using PokemonGoApp.Models;
using PokemonGoApp.Services;
using PokemonGoApp.ViewModels;

namespace PokemonGoApp.Controllers
{
    public class ListController : Controller
    {
        //http://localhost:53822/List?bottomLeftLongitude=43.14225085508007&bottomLeftLatitude=-77.56777719608152&topRightLongitude=43.161036088374466&topRightLatitude=-77.60640100589598
        public ActionResult Index(double bottomLeftLongitude = 0.0, double bottomLeftLatitude = 0.0, double topRightLongitude = 0.0, double topRightLatitude = 0.0)
        {
            var mongoService = new MongoService();
            var pokemon = new List<PokemonLocation>();
            if (bottomLeftLongitude > 0.0)
            {
                pokemon = mongoService.GetFilteredLocations(bottomLeftLongitude, bottomLeftLatitude, topRightLongitude, topRightLatitude);
            }
            else
            {
                pokemon = mongoService.GetAllLocations();
            }
            var model = new PokemonListViewModel { Data = pokemon };
            return View(model);
        }
    }
}