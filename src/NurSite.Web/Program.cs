using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Net.Http.Headers;
using NurSite.Infrastructure;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;
using NurSite.Infrastructure.Persistence.Seed;
using NurSite.Web.Extensions;
using NurSite.Web.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSiteIdentity();
builder.Services.AddScoped<ThemeResolver>();

builder.Services.AddRazorPages(options =>
{
    // کل پنل ادمین پشت احراز هویت
    options.Conventions.AuthorizeAreaFolder("Admin", "/", policy: "AdminArea");
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminArea", policy => policy.RequireRole(
        AppRoles.SuperAdmin, AppRoles.Admin, AppRoles.Editor, AppRoles.Author, AppRoles.Mufti))
    // هر دسترسی به صورت Claim بررسی می‌شود
    .AddPolicy(Permissions.Articles.Publish, p => p.RequireClaim(Permissions.ClaimType, Permissions.Articles.Publish))
    .AddPolicy(Permissions.Users.Manage, p => p.RequireClaim(Permissions.ClaimType, Permissions.Users.Manage))
    .AddPolicy(Permissions.Settings.Manage, p => p.RequireClaim(Permissions.ClaimType, Permissions.Settings.Manage));

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = new[]
    {
        "text/html", "text/css", "text/javascript", "application/javascript",
        "application/json", "image/svg+xml", "application/manifest+json"
    };
});

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(p => p.Expire(TimeSpan.FromMinutes(5)));
    // صفحاتی که محتوایشان کم عوض می‌شود
    options.AddPolicy("Content", p => p.Expire(TimeSpan.FromMinutes(30)).SetVaryByQuery("page"));
});

builder.Services.AddHsts(o =>
{
    o.MaxAge = TimeSpan.FromDays(365);
    o.IncludeSubDomains = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/khata");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/khata/{0}");
app.UseHttpsRedirection();
app.UseResponseCompression();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name;
        // فایل‌های نسخه‌دار می‌توانند طولانی کش شوند
        var oneYear = TimeSpan.FromDays(365);
        var oneHour = TimeSpan.FromHours(1);

        var isImmutable = path.EndsWith(".woff2") || path.EndsWith(".png") ||
                          path.EndsWith(".webp") || path.EndsWith(".svg");

        ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = isImmutable ? oneYear : oneHour
        };

        // سرویس‌ورکر هرگز نباید کش شود، وگرنه نسخه جدید به کاربر نمی‌رسد
        if (path == "sw.js")
        {
            ctx.Context.Response.GetTypedHeaders().CacheControl =
                new CacheControlHeaderValue { NoCache = true, NoStore = true, MustRevalidate = true };
        }
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapRazorPages();

// سرویس‌ورکر باید از ریشه سرو شود وگرنه دامنه کنترلش محدود می‌شود
app.MapGet("/sw.js", async (HttpContext ctx, IWebHostEnvironment env) =>
{
    ctx.Response.ContentType = "application/javascript; charset=utf-8";
    ctx.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
    await ctx.Response.SendFileAsync(Path.Combine(env.WebRootPath, "sw.js"));
});

// مقداردهی اولیه دیتابیس در محیط توسعه
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("SeedOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    await DbInitializer.SeedAsync(
        sp.GetRequiredService<AppDbContext>(),
        sp.GetRequiredService<UserManager<ApplicationUser>>(),
        sp.GetRequiredService<RoleManager<ApplicationRole>>(),
        sp.GetRequiredService<ILogger<Program>>(),
        builder.Configuration["Seed:AdminMobile"] ?? "",
        builder.Configuration["Seed:AdminPassword"] ?? "");
}

app.Run();