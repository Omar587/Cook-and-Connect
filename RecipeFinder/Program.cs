using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeFinder.Data;
using RecipeFinder.Models;
using RecipeFinder.Services;
using RecipeFinder.Services.Forum;

var builder = WebApplication.CreateBuilder(args);

//
// ─────────────────────────────────────────────
// DATABASE (LOCAL + RENDER SAFE FIXED)
// ─────────────────────────────────────────────
//
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

string connString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    // FIX: prevent Port = -1 crash on Render
    var port = uri.Port > 0 ? uri.Port : 5432;

    connString =
        $"Host={uri.Host};" +
        $"Port={port};" +
        $"Username={userInfo[0]};" +
        $"Password={userInfo[1]};" +
        $"Database={uri.AbsolutePath.TrimStart('/')};" +
        $"SSL Mode=Require;Trust Server Certificate=true;";
}
else
{
    connString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connString));

//
// ─────────────────────────────────────────────
// IDENTITY
// ─────────────────────────────────────────────
//
builder.Services.AddIdentity<Customer, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

//
// ─────────────────────────────────────────────
// SERVICES
// ─────────────────────────────────────────────
//
builder.Services.AddScoped<IForumPostService, ForumPostService>();
builder.Services.AddScoped<IForumCommentService, ForumCommentService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

//
// ─────────────────────────────────────────────
// DATABASE MIGRATION
// ─────────────────────────────────────────────
//
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}

//
// ─────────────────────────────────────────────
// SEED DATA (SAFE GUARDS)
// ─────────────────────────────────────────────
//
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    var recipeSeeder = new RecipeSeeder();

    if (!context.Recipes.Any())
    {
        await RecipeSeeder.SeedRecipes(context);
        recipeSeeder.SeedInstructions(context);
    }

    if (!context.ForumPosts.Any())
    {
        await ForumSeeder.SeedAsync(services);
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    var recipeSeeder = new RecipeSeeder();
    
    recipeSeeder.SeedInstructions(context);

   
}

//
// ─────────────────────────────────────────────
// HTTP PIPELINE
// ─────────────────────────────────────────────
//
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();