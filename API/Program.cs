using API.Extensions;
using API.Middleware;
using API.SignalR;
using Core.Entities.Identity;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders; // Asigură-te că pui asta sus de tot la importuri, dacă lipsește

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddSignalR();
var app = builder.Build();

// Configure the HTTP request pipeline. //POSITION IS IMPORTANT
app.UseMiddleware<ExceptionMiddleware>();

app.UseStatusCodePagesWithReExecute("/errors/{0}");

app.UseSwaggerDocumention();

app.UseStaticFiles();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization(); //middleware for us to use authorisation

app.MapControllers(); //middleware to map controllers => our API knows where to send the HTTP requests
app.MapHub<MessageHub>("hubs/message");

// ADAUGĂ ACEST BLOC NOU: Îi spune serverului să servească fișiere din folderul "Content"
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Content")),
    RequestPath = "/Content"
});

using var scope = app.Services.CreateScope();

var services = scope.ServiceProvider;
var context = services.GetRequiredService<StoreContext>();
// Am sters: var identityContext = services.GetRequiredService<AppIdentityDbContext>();
var userManager = services.GetRequiredService<UserManager<AppUser>>();
// 1. Extragem și RoleManager-ul:
var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
var logger = services.GetRequiredService<ILogger<Program>>();

try
{
    await context.Database.MigrateAsync();
    // Am sters: await identityContext.Database.MigrateAsync();
    
    await StoreContextSeed.SeedAsync(context);
   await AppIdentityDbContextSeed.SeedUsersAsync(userManager, roleManager);
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occured during migration");
}

app.Run(); //runs the app