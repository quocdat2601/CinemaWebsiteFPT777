using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly int[] _allowedRoles;

    public RoleAuthorizeAttribute(int[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity.IsAuthenticated)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var roleClaimValue = user.FindFirst(ClaimTypes.Role)?.Value;
        if (roleClaimValue == null || !int.TryParse(roleClaimValue, out int userRole) || !_allowedRoles.Contains(userRole))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
