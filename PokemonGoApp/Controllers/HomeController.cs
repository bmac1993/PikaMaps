using System.Web.Mvc;
using MongoDB.Driver.GeoJsonObjectModel;
using PokemonGoApp.Models;
using PokemonGoApp.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using PokemonGoApp.Attributes;
using PokemonGoApp.Helpers;
using PokemonGoApp.Enums;
using System.Threading.Tasks;

namespace PokemonGoApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly MongoService _mongoService;

        public HomeController()
        {
            _mongoService = new MongoService();
        }

        public ActionResult Index(string culture = "")
        {
            var currentCulture = (string)HttpContext?.Session?["culture"] ?? string.Empty;

            if (!string.IsNullOrEmpty(culture) && currentCulture != culture)
            {
                HttpContext.Session["culture"] = culture;
                currentCulture = culture;
            }
            if (!string.IsNullOrEmpty(currentCulture))
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(currentCulture);
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(currentCulture);
            }

            return View();
        }

        [AjaxOnly]
        public ActionResult GetPokemonLocations(double bottomLeftLongitude, double bottomLeftLatitude, double topRightLongitude, double topRightLatitude, int zoom, int pokeIndexNum = 0, bool liveMode = false)
        {
            var zoomLevel = GetZoomLevel(zoom);
            var mongoService = new MongoService();
            var pokemon = new List<PokemonLocation>();

            if (zoomLevel == ZoomLevel.All)
            {
                pokemon = mongoService.GetFilteredLocations(bottomLeftLongitude, bottomLeftLatitude, topRightLongitude, topRightLatitude, pokeIndexNum, liveMode);
            }
            else
            {
                var clusters = mongoService.GetClustersByLocation(bottomLeftLongitude, bottomLeftLatitude, topRightLongitude, topRightLatitude, zoomLevel);
                Parallel.ForEach(clusters, cluster =>
                {
                    pokemon.AddRange(mongoService.GetFilteredLocationsByCluster(zoomLevel, cluster.Id, pokeIndexNum, liveMode));
                });
            }
                
            return Json(pokemon, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AddPokemonLocation(int pokeIndexNum, double lng, double lat, DateTime date)
        {
            if (IsSpam(Request.UserHostAddress, Request.UserAgent))
            {
                return Json("Nope", JsonRequestBehavior.AllowGet);
            }

            if (date > DateTime.UtcNow) // Bug.. Not going to be accurate between UTC and client Date. Maybe check if it's more than 1 day in the future? Fix it
            {
                date = DateTime.UtcNow;
            }

            var dateTime = new HourMinute()
            {
                DayOfWeek = date.DayOfWeek.ToString(),
                TimeOfDay = (date.Hour > 12) ? "PM" : "AM",
                Minute = date.Minute,
                Hour = ((date.Hour == 0 ? 12 : date.Hour) > 12 ? date.Hour - 12 : date.Hour),
                DateAdded = DateTime.UtcNow
            };

            var location = new PokemonLocation
            {
                Location = new GeoJson2DGeographicCoordinates(lng, lat),
                LastSighted = date,
                DateCreated = DateTime.UtcNow,
                PokemonName = LookupTable.GetPokemonName(pokeIndexNum),
                PokeIndexNum = pokeIndexNum,
                IpAddress = Request.UserHostAddress,
                UserAgent = Request.UserAgent,
                Times = new List<HourMinute> { dateTime }
            };

            var pokemon = _mongoService.AddLocation(location);
            return Json(pokemon, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AddPokemonInstance(string id, DateTime date)
        {
            var instance = new HourMinute()
            {
                DayOfWeek = date.DayOfWeek.ToString(),
                TimeOfDay = (date.Hour > 12) ? "PM" : "AM",
                Minute = date.Minute,
                Hour = ((date.Hour == 0 ? 12 : date.Hour) > 12 ? date.Hour - 12 : date.Hour),
                DateAdded = DateTime.UtcNow
            };

            var pokemon = _mongoService.GetLocationById(id);
            pokemon.LastSighted = (date < pokemon.LastSighted) ? date : pokemon.LastSighted;
            pokemon.Times.Add(instance);

            var result = _mongoService.UpdateInstances(pokemon);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// Checks if they are black listed or have submitted more than 20 locations in 5 minutes
        /// </summary>
        private bool IsSpam(string userHostAddress, string userAgent)
        {
            if (_mongoService.IsBlacklisted(userHostAddress, userAgent, 60))
            {
                return true;
            }

            var records = _mongoService.GetRecentSightingsCountByIp(userAgent, userHostAddress, 5);
            if (records >= 20)
            {
                _mongoService.AddUserRequestData(new UserRequestObject()
                {
                    Timestamp = DateTime.UtcNow,
                    IpAddress = userHostAddress,
                    UserAgent = userAgent
                });
                _mongoService.DeleteRecentLocationsByIp(userHostAddress, userAgent, 6);
                return true;
            }

            return false;
        }

        public ActionResult ClusterData(int zoom)
        {
            MongoService mongoService = new MongoService();
            var data = mongoService.GetAllLocations();
            var zoomLevel = GetZoomLevel(zoom);
            var radius = GetClusterRadius(zoom);
            var clusters = new List<PokemonLocation>();
            foreach(var location in data)
            {
                if(!mongoService.IsClusterWithinDistance(location.Location, radius, zoomLevel))
                {
                    mongoService.AddCluster(new Cluster()
                    {
                        Location = location.Location,
                        ZoomLevel = zoomLevel
                    });
                }
            }
            foreach (var location in data)
            {
                var nearestCluster = mongoService.FindNearestCluster(location.Location, radius, zoomLevel);
                if(zoomLevel == ZoomLevel.OneThroughFive)
                {
                    location.FirstClusterId = nearestCluster.Id;
                }
                if (zoomLevel == ZoomLevel.SixThroughSeven)
                {
                    location.SecondClusterId = nearestCluster.Id;
                }
                if (zoomLevel == ZoomLevel.EightThroughTen)
                {
                    location.ThirdClusterId = nearestCluster.Id;
                }
                if (zoomLevel == ZoomLevel.ElevenThroughThirteen)
                {
                    location.FourthClusterId = nearestCluster.Id;
                }
                mongoService.UpdateInstances(location);
            }

            return View();
        }

        private ZoomLevel GetZoomLevel(int zoom)
        {
            if(zoom <= 5)
            {
                return ZoomLevel.OneThroughFive;
            }
            else if(zoom == 6 || zoom == 7)
            {
                return ZoomLevel.SixThroughSeven;
            }
            else if(zoom >= 8 && zoom <= 10 )
            {
                return ZoomLevel.EightThroughTen;
            }
            else if(zoom >= 11 && zoom <= 13)
            {
                return ZoomLevel.ElevenThroughThirteen;
            }
            else
            {
                return ZoomLevel.All;
            }
        }
        // in meters
        private int GetClusterRadius(int zoom)
        {
            if (zoom <= 5)
            {
                return 402336;
            }
            else if (zoom == 6 || zoom == 7)
            {
                return 80467;
            }
            else if (zoom >= 8 && zoom <= 10)
            {
                return 12070;
            }
            else // 11-13
            {
                return 1609;
            }
        }

    }
}