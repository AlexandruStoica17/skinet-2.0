using API.Extensions;
using API.Middleware;
using API.SignalR;
using Core.Entities.Identity;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddSignalR();

// Singleton — tine evidenta conexiunilor active pentru notificari in timp real
builder.Services.AddSingleton<PresenceTracker>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseStatusCodePagesWithReExecute("/errors/{0}");
app.UseSwaggerDocumention();
app.UseStaticFiles();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<MessageHub>("hubs/message");
app.MapHub<NotificationHub>("hubs/notification");
app.MapHub<PresenceHub>("hubs/presence");

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Content")),
    RequestPath = "/Content"
});

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var context = services.GetRequiredService<StoreContext>();
var userManager = services.GetRequiredService<UserManager<AppUser>>();
var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
var logger = services.GetRequiredService<ILogger<Program>>();

try
{
   await context.Database.MigrateAsync();

// MODIFICAT: întâi seed-uim userii/producătorii
await AppIdentityDbContextSeed.SeedUsersAsync(userManager, roleManager);

// MODIFICAT: apoi seed-uim produsele, pentru că ele au ProducerId / ProducerName
await StoreContextSeed.SeedAsync(context, userManager);
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occured during migration");
}

app.Run();