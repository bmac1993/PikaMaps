using System.Collections.Generic;
using PokemonGoApp.Models;

namespace PokemonGoApp.ViewModels
{
    public class PokemonListViewModel
    {
        public PokemonListViewModel()
        {
            Data = new List<PokemonLocation>();
        }

        public List<PokemonLocation> Data { get; set; } 
    }
}