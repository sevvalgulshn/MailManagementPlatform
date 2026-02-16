using Microsoft.AspNetCore.Identity;
using Project2EmailNight.Context;
using Project2EmailNight.Entities;
using Project2EmailNight.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EmailContext>();

builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<EmailContext>()
    .AddErrorDescriber<CustomIdentityValidator>();

// Login yönlendirmesi 
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/UserLogin";
    options.AccessDeniedPath = "/Login/UserLogin";
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));


var app = builder.Build();

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
    pattern: "{controller=Login}/{action=UserLogin}/{id?}"); // baþlangýç login

app.Run();


