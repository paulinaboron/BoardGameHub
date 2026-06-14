using BoardGameHub.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BoardGameHub.Models;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DataBaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BoardGameHubConnectionString")));

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=BoardGame}/{action=UserViewAll}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var appDb = services.GetRequiredService<ApplicationDbContext>();
        appDb.Database.Migrate();
    }
    catch (Exception ex)
    {
    }

    try
    {
        var dataDb = services.GetRequiredService<DataBaseContext>();
        dataDb.Database.Migrate();
    }
    catch {  }

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var config = services.GetRequiredService<IConfiguration>();

    string[] roles = new[] { "Admin", "User" };
    foreach (var role in roles)
    {
        var exists = roleManager.RoleExistsAsync(role).GetAwaiter().GetResult();
        if (!exists)
        {
            var rres = roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            if (!rres.Succeeded)
            {
                var errs = string.Join(", ", rres.Errors.Select(e => e.Description));
                throw new Exception($"Unable to create role '{role}': {errs}");
            }
        }
    }

    var adminEmail = config["AdminUser:Email"];
    var adminPassword = config["AdminUser:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var admin = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
        if (admin == null)
        {
            admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var createResult = userManager.CreateAsync(admin, adminPassword).GetAwaiter().GetResult();
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new Exception("Admin creation failed: " + errors);
            }

            admin = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
            if (admin == null)
            {
                throw new Exception("Admin user was created but cannot be found afterwards.");
            }
        }

        var isInRole = userManager.IsInRoleAsync(admin, "Admin").GetAwaiter().GetResult();
        if (!isInRole)
        {
            var addRoleResult = userManager.AddToRoleAsync(admin, "Admin").GetAwaiter().GetResult();
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                throw new Exception("Assigning Admin role failed: " + errors);
            }
        }
    }
}

app.Run();
