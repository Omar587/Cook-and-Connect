using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeFinder.Data;
using RecipeFinder.Models;
using RecipeFinder.Services;
using RecipeFinder.Repository;
using RecipeFinder.Data;
using RecipeFinder.Services.Forum;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add this (PostgreSQL):
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

Console.WriteLine(connectionString);

// Add Identity services
builder.Services.AddIdentity<Customer, IdentityRole<int>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<IForumPostService,    ForumPostService>();
builder.Services.AddScoped<IForumCommentService, ForumCommentService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();


//-- RECIPE SEEDER --------------------------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await RecipeSeeder.SeedRecipes(context);
}


// ── Forum seeder ──────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    await ForumSeeder.SeedAsync(scope.ServiceProvider);
}

// Instruction seeder 

// ── Forum seeder ──────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    
    var services = scope.ServiceProvider;
    RecipeSeeder recipeSeeder = new RecipeSeeder();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    recipeSeeder.SeedInstructions(context);
}


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    RecipeService recipeService = new RecipeService(context);
    Console.WriteLine(recipeService.GetById(34).Name);
    
    
    recipeService.TestIngridents();
    
  

}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Add these authentication/authorization middleware (IMPORTANT: order matters!)
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();