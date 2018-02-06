using System.Web.Mvc;

namespace PokemonGoApp.Attributes
{
    /// <summary>
    /// Avoid someone from directly calling our URL for a data dump
    /// </summary>
    public class AjaxOnlyAttribute : ActionMethodSelectorAttribute
    {
        public override bool IsValidForRequest(ControllerContext controllerContext, System.Reflection.MethodInfo methodInfo)
        {
            return controllerContext.RequestContext.HttpContext.Request.IsAjaxRequest();
        }
    }
}