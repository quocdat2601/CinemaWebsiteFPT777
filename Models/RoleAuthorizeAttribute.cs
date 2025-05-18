using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly int[] _allowedRoles;

    public RoleAuthorizeAttribute(int[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.Session.GetInt32("Role");

        if (role == null || !_allowedRoles.Contains(role.Value))
        {
            // Redirect to AccessDenied (you can change this)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
