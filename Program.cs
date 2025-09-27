using Floaty_Music;
using Floaty_Music.Models;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.Load();
GlobalConfiguration.LoadConfig();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

if (GlobalConfiguration.isSQLITE)
{
    builder.Services.AddDbContext<FloatlyContext>(options =>
    options.UseSqlite(GlobalConfiguration.ConnectionString));
}
else
{
    builder.Services.AddDbContext<FloatlyContext>(options =>
        options.UseSqlServer(GlobalConfiguration.ConnectionString));
}
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
//app.UseStaticFiles(new StaticFileOptions
//{
//    ServeUnknownFileTypes = true,
//    ContentTypeProvider = new FileExtensionContentTypeProvider
//    {
//        Mappings = {
//            [".srt"] = "application/x-subrip",
//            [".mp3"] = "audio/mpeg"
//        }
//    }
//});
app.UseStaticFiles();
// app.UseStaticFiles(new StaticFileOptions
// {
//     OnPrepareResponse = ctx =>
//     {
//         // cache for 30 days
//         ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
//     }
// });


app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();
app.UseResponseCompression();

if (GlobalConfiguration.isSQLITE)
{
    // For SQLite, ensure database file and tables are created
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FloatlyContext>();
    db.Database.EnsureCreated();
}
app.Run();
