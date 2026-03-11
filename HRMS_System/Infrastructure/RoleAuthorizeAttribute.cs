using System;
using System.Linq;
using HRMS_System.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRMS_System.Infrastructure
{
    // Because the class name ends with "Attribute",
    // you can use it as [RoleAuthorize(...)]
    public class RoleAuthorizeAttribute : Attribute, IPageFilter
    {
        private readonly AccessRole[] _allowed;

        public RoleAuthorizeAttribute(params AccessRole[] allowedRoles)
        {
            _allowed = allowedRoles ?? Array.Empty<AccessRole>();
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            var roleClaim = context.HttpContext.User.FindFirst("UserRole")?.Value;

            // not logged in
            if (string.IsNullOrWhiteSpace(roleClaim))
            {
                context.Result = new RedirectToPageResult("/Account/LoginPage");
                return;
            }

            if (!Enum.TryParse<AccessRole>(roleClaim, out var role))
            {
                context.Result = new RedirectToPageResult("/Account/ForbiddenPage/Index");
                return;
            }

            // HR can access everything
            if (role == AccessRole.HR) return;

            // If page expects specific roles, enforce it
            if (_allowed.Length > 0 && !_allowed.Contains(role))
            {
                context.Result = new RedirectToPageResult("/Forbidden");
                // or: context.Result = new StatusCodeResult(403);
            }
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
        public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }
    }
}