using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;

namespace PokemonGoApp.Models
{
    public class PokemonLocation
    {
        public PokemonLocation()
        {
            IpAddress = "0.0.0.0";
            Times = new List<HourMinute>();
        }

        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string PokemonName { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime LastSighted { get; set; }
        public int PokeIndexNum { get; set; }
        public List<HourMinute> Times { get; set; } 
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public GeoJson2DGeographicCoordinates Location { get; set; }

        // Clustering
        [BsonRepresentation(BsonType.ObjectId)]
        public string FirstClusterId { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string SecondClusterId { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string ThirdClusterId { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string FourthClusterId { get; set; }
    }
}