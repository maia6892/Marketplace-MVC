using System.Net;
using System.Net.Mail;
using Market.Data;
using Market.Models;
using Market.Services;
using Market.Tools;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<MarketUserService>();

builder.Services.AddDbContext<MarketDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Default") ?? throw new InvalidCastException("Connection string 'MarketDbContext' not found.")));

builder.Services.AddDbContext<MarketIdentityDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Identity") ?? throw new InvalidCastException("Connection string 'MarketIdentityDbContext' not found.")));

builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<MarketIdentityDbContext>()
    .AddDefaultTokenProviders();



builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
});

// Email configure
Helpers.Configure(builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>() ?? new SmtpSettings());


builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Error/AccessDenied";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Create the admin user
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    
    var adminEmail = "admin@example.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        
        var admin = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin", 
            LastName = "User", 
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, "Admin1234!"); 
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        DbInitializer.Seed(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
