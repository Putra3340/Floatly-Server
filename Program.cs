using Floaty_Music;
using Floaty_Music.Models;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System;

DotNetEnv.Env.Load();
GlobalConfiguration.LoadConfig();
var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.Services.AddDbContext<FloatlyContext>(options =>
    options.UseSqlServer("Data Source=WIN-BNOFJBSA8BF;Initial Catalog=Floatly;Integrated Security=True;Encrypt=True;Trust Server Certificate=True"));
#else
builder.Services.AddDbContext<FloatlyContext>(options =>
    options.UseSqlite("Data Source=database.db;Foreign Keys=True;"));
#endif

builder.Services.AddControllers();

builder.Services.AddControllersWithViews();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication("MyAuth")
    .AddCookie("MyAuth", options =>
    {
        options.LoginPath = "/auth/login";
        options.AccessDeniedPath = "/auth/denied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // also compress HTTPS
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
    "image/svg+xml",   // vector, compressible
    "image/jpeg",      // usually no benefit
    "image/png",       // usually no benefit
    "image/webp"       // usually no benefit
    });

});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(x =>
    {
        x.InjectStylesheet("/swagger/swagger-dark.css");
    });
}
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings = {
            [".srt"] = "application/x-subrip",
            [".mp3"] = "audio/mpeg"
        }
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
//app.UseHttpsRedirection();

app.MapControllers();
app.UseResponseCompression();

// wakey wakey
using (var ctx = new FloatlyContext())
{
    ctx.Database.EnsureCreated();
    ctx.Database.GetDbConnection().Open();
    ctx.Songs.FirstOrDefault(); // triggers model & query compilation
}
app.Run();
