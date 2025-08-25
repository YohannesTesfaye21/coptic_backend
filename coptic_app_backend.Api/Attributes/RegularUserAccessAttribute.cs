using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace coptic_app_backend.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RegularUserAccessAttribute : AuthorizeAttribute, IAuthorizationFilter
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

            // Check if user is a Regular user
            var userType = user.FindFirst("UserType")?.Value;
            if (userType != "Regular")
            {
                context.Result = new ForbidResult();
                return;
            }

            // Store UserId and AbuneId in HttpContext for use in controllers
            var userId = user.FindFirst("UserId")?.Value;
            var abuneId = user.FindFirst("AbuneId")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                context.HttpContext.Items["UserId"] = userId;
            }
            if (!string.IsNullOrEmpty(abuneId))
            {
                context.HttpContext.Items["AbuneId"] = abuneId;
            }
        }
    }
}
