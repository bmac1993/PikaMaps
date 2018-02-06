using System;

namespace PokemonGoApp.Models
{
    public class HourMinute
    {
        public string DayOfWeek { get; set; }
        public string TimeOfDay { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public DateTime DateAdded { get; set; }
    }
}