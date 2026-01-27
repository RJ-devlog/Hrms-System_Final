using HRMS_System.Data;
using HRMS_System.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Add services to the container.
                builder.Services.AddRazorPages();

                // Validate connection string early
                var conn = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(conn))
                {
                    throw new InvalidOperationException(
                        "Connection string 'DefaultConnection' is missing or empty in appsettings.json.");
                }

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(conn));

                // ✅ Register your custom services BEFORE Build()
                builder.Services.AddScoped<DepartmentSelectService>();

                // Cookie Authentication
                builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/Account/LoginPage"; // must match your Razor Page route
                    });

                builder.Services.AddAuthorization();

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                }
                else
                {
                    // In minimal hosting model this is fine, but optional.
                    app.UseDeveloperExceptionPage();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapRazorPages();

                app.Run();
            }
            catch (System.Text.Json.JsonException jex)
            {
                Console.Error.WriteLine("Failed to parse appsettings.json: " + jex.Message);
                Console.Error.WriteLine(jex.ToString());
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Application failed to start: " + ex.Message);
                Console.Error.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}
