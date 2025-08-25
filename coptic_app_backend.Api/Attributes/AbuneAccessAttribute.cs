using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace coptic_app_backend.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AbuneAccessAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            // Check if user is authenticated
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check if user is an Abune
            var userType = user.FindFirst("UserType")?.Value;
            if (userType != "Abune")
            {
                context.Result = new ForbidResult();
                return;
            }

            // Store AbuneId in HttpContext for use in controllers
            var abuneId = user.FindFirst("AbuneId")?.Value;
            if (!string.IsNullOrEmpty(abuneId))
            {
                context.HttpContext.Items["AbuneId"] = abuneId;
            }
        }
    }
}
