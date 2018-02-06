using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using PokemonGoApp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PokemonGoApp.Models
{
    public class Cluster
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public GeoJson2DGeographicCoordinates Location { get; set; }
        [BsonRepresentation(BsonType.String)]
        public ZoomLevel ZoomLevel { get; set; }
    }
}