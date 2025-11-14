// Authorization/RequireAuthAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ABCRetailers.Authorization
{
    public class RequireAuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var username = session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                context.Result = new RedirectToActionResult("Login", "Login", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}