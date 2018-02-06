using System.Globalization;
using System.Threading;
using System.Web.Mvc;

namespace PokemonGoApp.ActionFilters
{
    public class InternationalizationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var language = (string)filterContext.RouteData.Values["language"] ?? "en";
            var culture = (string)filterContext.RouteData.Values["culture"] ?? "US";

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo($"{language}-{culture}");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo($"{language}-{culture}");
        }
    }
}