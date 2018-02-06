using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PokemonGoApp.Startup))]
namespace PokemonGoApp
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}
