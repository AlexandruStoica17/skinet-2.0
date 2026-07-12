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
            services.AddIdentityCore<AppUser>(opt =>{})
            .AddRoles<IdentityRole>() 
            .AddRoleManager<RoleManager<IdentityRole>>() 
            .AddEntityFrameworkStores<StoreContext>()
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

                 options.Events = new JwtBearerEvents
                 {
                     OnMessageReceived = context => 
                     {
                         var accessToken = context.Request.Query["access_token"];
                         var path = context.HttpContext.Request.Path;
             
                         if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                         {
                             context.Token = accessToken;
                         }

                         return Task.CompletedTask;
                     }
                 };
            });

            services.AddAuthorization();

            return services;
        }
    }
}