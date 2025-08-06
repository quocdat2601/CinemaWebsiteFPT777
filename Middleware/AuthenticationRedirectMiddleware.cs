using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using System.Security.Claims;

namespace MovieTheater.Middleware
{
    public class AuthenticationRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationRedirectMiddleware> _logger;

        public AuthenticationRedirectMiddleware(RequestDelegate next, ILogger<AuthenticationRedirectMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAccountRepository accountRepository)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Kiểm tra nếu đang truy cập trang Login và đã đăng nhập
            if (path == "/account/login" && context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    var account = accountRepository.GetById(userId);
                    if (account != null)
                    {
                        // Redirect dựa trên role
                        if (account.RoleId == 1) // Admin
                        {
                            context.Response.Redirect("/Admin/MainPage");
                            return;
                        }
                        else if (account.RoleId == 2) // Employee
                        {
                            context.Response.Redirect("/Employee/MainPage");
                            return;
                        }
                        else // Member (RoleId == 3)
                        {
                            context.Response.Redirect("/Home/Index");
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }

    // Extension method để dễ dàng thêm middleware
    public static class AuthenticationRedirectMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationRedirect(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationRedirectMiddleware>();
        }
    }
} 