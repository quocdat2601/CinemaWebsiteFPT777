using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.Services;

namespace MovieTheater
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<MovieTheaterContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
             .AddCookie(options =>
             {
                 options.LoginPath = "/Account/Login";
                 options.LogoutPath = "/Account/Logout";
                 options.AccessDeniedPath = "/Account/AccessDenied";
                 options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
             })
             .AddGoogle(options =>
             {
                 IConfigurationSection googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
                 options.ClientId = googleAuthNSection["ClientId"];
                 options.ClientSecret = googleAuthNSection["ClientSecret"];
                 options.CallbackPath = "/signin-google";
             });


            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IMovieRepository, MovieRepository>();
            builder.Services.AddScoped<IMovieService, MovieService>();
            builder.Services.AddScoped<ICinemaRepository, CinemaRepository>();
            builder.Services.AddScoped<ICinemaService, CinemaService>();
            builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            builder.Services.AddScoped<IEmployeeService, EmployeeService>();
            builder.Services.AddScoped<IMemberRepository, MemberRepository>();
            builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
            builder.Services.AddScoped<IPromotionService, PromotionService>();
            builder.Services.AddScoped<ISeatRepository, SeatRepository>();
            builder.Services.AddScoped<ISeatService, SeatService>();
            builder.Services.AddScoped<ISeatTypeRepository, SeatTypeRepository>();
            builder.Services.AddScoped<ISeatTypeService, SeatTypeService>();

           

            builder.Services.AddControllersWithViews();
            var app = builder.Build();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();


            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Movie}/{action=MovieList}/{id?}");

            app.Run();
        }
    }
}
