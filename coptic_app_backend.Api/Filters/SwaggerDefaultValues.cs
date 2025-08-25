using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace coptic_app_backend.Api.Filters
{
    public class SwaggerDefaultValues : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            // Check if the operation has authorization attributes
            var hasAuthorize = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Any() ?? false;

            var hasAllowAnonymous = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any() ?? false;

            var methodHasAuthorize = context.MethodInfo.GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Any();

            var methodHasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any();

            // If the operation requires authorization and doesn't have AllowAnonymous
            if ((hasAuthorize || methodHasAuthorize) && !hasAllowAnonymous && !methodHasAllowAnonymous)
            {
                // Add security requirement
                if (operation.Security == null)
                    operation.Security = new List<OpenApiSecurityRequirement>();

                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            }

            // Add operation summary if not present
            if (string.IsNullOrEmpty(operation.Summary))
            {
                operation.Summary = apiDescription.ActionDescriptor.DisplayName;
            }

            // Add operation description if not present
            if (string.IsNullOrEmpty(operation.Description))
            {
                operation.Description = $"Endpoint: {apiDescription.HttpMethod} {apiDescription.RelativePath}";
            }
        }
    }
}
