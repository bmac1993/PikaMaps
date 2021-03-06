﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using PokemonGoApp.Models;
using MongoDB.Driver.GeoJsonObjectModel;
using PokemonGoApp.Enums;

namespace PokemonGoApp.Services
{
    public class MongoService
    {
        MongoDatabase _db;

        public MongoService()
        {
            var pack = new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("camel case", pack, t => true);

            var client = new MongoClient(ConfigurationManager.AppSettings["dbConnectionString"]);
            var server = client.GetServer();
            _db = server.GetDatabase(ConfigurationManager.AppSettings["dbDatabaseName"]);
        }

        public PokemonLocation AddLocation(PokemonLocation location)
        {
            var locations = _db.GetCollection<PokemonLocation>("PokemonLocations");
            locations.Insert(location);
            return location;
        }

        public List<PokemonLocation> GetAllLocations()
        {
            var locations = _db.GetCollection<PokemonLocation>("PokemonLocations");
            var result = locations.FindAll();
            return result.ToList();
        }

        public List<PokemonLocation> GetFilteredLocations(double bottomLeftLongitude, double bottomLeftLatitude, double topRightLongitude, double topRightLatitude, int pokeIndexNum = 0, bool liveMode = false)
        {
            var locations = _db.GetCollection<PokemonLocation>("PokemonLocations");
            var query = Query<PokemonLocation>.WithinRectangle(a => a.Location, bottomLeftLongitude, bottomLeftLatitude, topRightLongitude, topRightLatitude);
            if (pokeIndexNum > 0)
            {
                query = Query.And(query, Query<PokemonLocation>.EQ(a => a.PokeIndexNum, pokeIndexNum));
            }
            if (liveMode)
            {
                query = Query.And(query, Query<PokemonLocation>.ElemMatch(a => a.Times, builder => Query<HourMinute>.GT(a => a.DateAdded, DateTime.UtcNow.AddMinutes(-15))));
            }

            return locations.Find(query).ToList();
        }

        public List<PokemonLocation> GetFilteredLocationsByCluster(ZoomLevel zoomLevel, string clusterId, int pokeIndexNum = 0, bool liveMode = false)
        {
            var locations = _db.GetCollection<PokemonLocation>("PokemonLocations");
            var query = Query.Create(new MongoDB.Bson.BsonDocument());
            if (pokeIndexNum > 0)
            {
                query = Query.And(query, Query<PokemonLocation>.EQ(a => a.PokeIndexNum, pokeIndexNum));
            }
            if (liveMode)
            {
                query = Query.And(query, Query<PokemonLocation>.ElemMatch(a => a.Times, builder => Query<HourMinute>.GT(a => a.DateAdded, DateTime.UtcNow.AddMinutes(-15))));
            }
            if (zoomLevel == ZoomLevel.OneThroughFive)
            {
                query = Query.And(query, Query<PokemonLocation>.EQ(a => a.FirstClusterId, clusterId));
                return locations.Find(query).SetLimit(10).ToList();
            }
            if (zoomLevel == ZoomLevel.SixThroughSeven)
            {
                query = Query.And(query, Query<PokemonLocation>.EQ(a => a.SecondClusterId, clusterId));
                return locations.Find(query).SetLimit(20).ToList();
            }
            if (zoomLevel == ZoomLevel.EightThroughTen)
            {
                query = Query.And(query, Query<PokemonLocation>.EQ(a => a.ThirdClusterId, clusterId));
                return locations.Find(query).SetLimit(40).ToList();
            }
            else // 11-13
            {
                query = Query.And(query, Query<PokemonLocation>.EQ(a => a.FourthClusterId, clusterId));
                return locations.Find(query).SetLimit(70).ToList();
            }
        }

        public List<Cluster> GetClustersByLocation(double bottomLeftLongitude, double bottomLeftLatitude, double topRightLongitude, double topRightLatitude, ZoomLevel zoomLevel)
        {
            var clusters = _db.GetCollection<Cluster>("Clusters");
            var query = Query<Cluster>.WithinRectangle(a => a.Location, bottomLeftLongitude, bottomLeftLatitude, topRightLongitude, topRightLatitude);
            query = Query.And(query, Query<Cluster>.EQ(a => a.ZoomLevel, zoomLevel));
            return clusters.Find(query).ToList();
        }

		public int GetRecentSightingsCountByIP(string userAgent, string ipAddress, int minutes)
		{
			var locations = _db.GetCollection<PokemonLocation>("PokemonLocations");
            var query = Query<PokemonLocation>.EQ(a => a.UserAgent, userAgent);
            query = Query.And(query, Query<PokemonLocation>.EQ(a => a.IpAddress, ipAddress));
            query = Query.And(query, Query<PokemonLocation>.GT(a => a.DateCreated, DateTime.UtcNow.AddMinutes(-minutes)));

            return (int)locations.Count(query);
        }

        public PokemonLocation GetLocationById(string id)
        {
            var locations = _db.GetCollection<PokemonLocation>("PokemonLocations");
            var query = Query<PokemonLocation>.EQ(a => a.Id, id);
            return locations.FindOne(Query.And(query));
        }

        public PokemonLocation UpdateInstances(PokemonLocation location)
        {
            var locations = _db.GetCollection<PokemonLocation>("PokemonLocations");
            var idQuery = Query<PokemonLocation>.EQ(e => e.Id, location.Id);
            var update = Update<PokemonLocation>.Set(x => x.Times, location.Times)
                                                .Set(x => x.LastSighted, location.LastSighted)
                                                .Set(x => x.FirstClusterId, location.FirstClusterId)
                                                .Set(x => x.SecondClusterId, location.SecondClusterId)
                                                .Set(x => x.ThirdClusterId, location.ThirdClusterId)
                                                .Set(x => x.FourthClusterId, location.FourthClusterId);

            var result = locations.FindAndModify(idQuery, SortBy.Descending("Id"), update);
            return location;
        }

        public UserRequestObject AddUserRequestData(UserRequestObject request)
        {
            var locations = _db.GetCollection<UserRequestObject>("SpammerInfo");
            locations.Insert(request);
            return request;
        }

        public void DeleteRecentLocationsByIp(string ipAddress, string userAgent, int minutes)
        {
            var locations = _db.GetCollection<PokemonLocation>("PokemonLocations");
            var query = Query<PokemonLocation>.EQ(a => a.UserAgent, userAgent);
            query = Query.And(query, Query<PokemonLocation>.EQ(a => a.IpAddress, ipAddress));
            query = Query.And(query, Query<PokemonLocation>.GT(a => a.DateCreated, DateTime.UtcNow.AddMinutes(-minutes)));
            locations.Remove(Query.And(query));
        }

        public bool IsBlacklisted(string ipAddress, string userAgent, int minutes)
        {
            var spammers = _db.GetCollection<UserRequestObject>("SpammerInfo");
            var query = Query<UserRequestObject>.EQ(a => a.UserAgent, userAgent);
            query = Query.And(query, Query<UserRequestObject>.EQ(a => a.IpAddress, ipAddress));
            query = Query.And(query, Query<UserRequestObject>.GT(a => a.Timestamp, DateTime.UtcNow.AddMinutes(-minutes)));

            return (int)spammers.Count(query) > 0;
        }

        public Cluster AddCluster(Cluster cluster)
        {
            var clusters = _db.GetCollection<Cluster>("Clusters");
            clusters.Insert(cluster);
            return cluster;
        }

        public Cluster FindNearestCluster(GeoJson2DGeographicCoordinates location, int distance, ZoomLevel zoomLevel)
        {
            var point = GeoJson.Point(GeoJson.Geographic(location.Longitude, location.Latitude));
            var clusters = _db.GetCollection<Cluster>("Clusters");
            var query = Query.And(Query<Cluster>.Near(a => a.Location, point, (double)distance),
                Query<Cluster>.EQ(a => a.ZoomLevel, zoomLevel));

            return clusters.Find(query).First();
        }

        public bool IsClusterWithinDistance(GeoJson2DGeographicCoordinates location, int distance, ZoomLevel zoomLevel)
        {
            var point = GeoJson.Point(GeoJson.Geographic(location.Longitude, location.Latitude));
            var clusters = _db.GetCollection<Cluster>("Clusters");
            var query = Query.And(Query<Cluster>.Near(a => a.Location, point, (double)distance),
                Query<Cluster>.EQ(a => a.ZoomLevel, zoomLevel));

            var cluster = clusters.FindOne(query);
            if(cluster == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}