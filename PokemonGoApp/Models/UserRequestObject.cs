using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PokemonGoApp.Models
{
    public class UserRequestObject
    {
        public DateTime Timestamp { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
    }
}