using HRMS_System.Data;
using Microsoft.EntityFrameworkCore;
using HRMS_System.Pages.Account;

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

                // Validate connection string early to provide a clear error instead of a cryptic JSON parse/null reference later.
                var conn = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(conn))
                {
                    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing or empty in appsettings.json.");
                }

            /*    builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(conn));*/

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


                //         builder.Services.AddDbContext<LoginPageModel>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Connection")));
                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }
                else
                {
                    // Helpful during development
                    app.UseDeveloperExceptionPage();
                }

                app.UseHttpsRedirection();

                // Serve static files from wwwroot (standard for Razor Pages)
                app.UseStaticFiles();

                app.UseRouting();

                app.UseAuthorization();

                // Replace nonstandard mapping calls with the usual Razor Pages mapping.
                app.MapRazorPages();

                app.Run();
            }
            catch (System.Text.Json.JsonException jex)
            {
                // Specific helpful message for JSON parse errors (appsettings.json)
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