// Diagnostic.App – web front end. Pages are served by MVC; all data comes from Diagnostic.Api and
// PDFs from Diagnostic.Report, called from the browser with the JWT kept in localStorage.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();
app.MapStaticAssets();

app.MapControllerRoute(name: "master", pattern: "Master/{id}", defaults: new { controller = "Master", action = "Index" });
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
