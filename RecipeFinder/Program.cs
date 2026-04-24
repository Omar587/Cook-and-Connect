using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeFinder.Data;
using RecipeFinder.Models;
using RecipeFinder.Services;
using RecipeFinder.Repository;
using RecipeFinder.Services.Forum;

var builder = WebApplication.CreateBuilder(args);

//
// ─────────────────────────────────────────────────────────────
// DATABASE (LOCAL + RENDER SAFE)
// ─────────────────────────────────────────────────────────────
//
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

string connString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    connString =
        $"Host={uri.Host};" +
        $"Port={uri.Port};" +
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
// ─────────────────────────────────────────────────────────────
// IDENTITY
// ─────────────────────────────────────────────────────────────
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
// ─────────────────────────────────────────────────────────────
// SERVICES
// ─────────────────────────────────────────────────────────────
//
builder.Services.AddScoped<IForumPostService, ForumPostService>();
builder.Services.AddScoped<IForumCommentService, ForumCommentService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

//
// ─────────────────────────────────────────────────────────────
// DATABASE MIGRATION (SAFE)
// ─────────────────────────────────────────────────────────────
//
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

//
// ─────────────────────────────────────────────────────────────
// SEED DATA (RUN ON STARTUP)
// ─────────────────────────────────────────────────────────────
//
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    await RecipeSeeder.SeedRecipes(context);
    await ForumSeeder.SeedAsync(services);

    var recipeSeeder = new RecipeSeeder();
    recipeSeeder.SeedInstructions(context);
}

//
// ─────────────────────────────────────────────────────────────
// HTTP PIPELINE
// ─────────────────────────────────────────────────────────────
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