using System.Text;
using Core.Entities.Identity;
using Infrastructure.Data; 
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace API.Extensions
{
    public static class IdentityServiceExtensions
    {
        public static IServiceCollection AddIdentityServices( this IServiceCollection services, IConfiguration config)
        {
            /* --- COD VECHI COMENTAT (Baza de date separată pentru Identity) ---
            services.AddDbContext<AppIdentityDbContext>(opt =>
            {
                opt.UseSqlite(config.GetConnectionString("IdentityConnection"));
            });
            ------------------------------------------------------------------ */
            
            services.AddIdentityCore<AppUser>(opt =>
            {
                //identity options here
            })
            /* --- COD VECHI COMENTAT ---
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            ----------------------------- */
            .AddEntityFrameworkStores<StoreContext>() // <--- FOLOSIM NOUA BAZĂ DE DATE UNIFICATĂ
            .AddSignInManager<SignInManager<AppUser>>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                 options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Token:Key"])),
                        ValidIssuer = config["Token:Issuer"],
                        ValidateIssuer = true,
                        ValidateAudience = false
                    };

                 // ---> ADAUGĂ ACEASTĂ PARTE PENTRU SIGNALR <---
                 options.Events = new JwtBearerEvents
                 {
                     OnMessageReceived = context => 
                     {
                         var accessToken = context.Request.Query["access_token"];
                         var path = context.HttpContext.Request.Path;
                         
                         // Verificăm dacă requestul este pentru un Hub și avem un token în URL
                         if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                         {
                             context.Token = accessToken;
                         }

                         return Task.CompletedTask;
                     }
                 };
                 // ---> PÂNĂ AICI <---
            });

            services.AddAuthorization();

            return services;
        }
    }
}