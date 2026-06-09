using System.Globalization;
using CookBook.Data;
using CookBook.Models;
using CookBook.Repositories;
using CookBook.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// Licencja QuestPDF (darmowa Community wystarcza dla projektu)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// InvariantCulture — model binder zawsze parsuje liczby z kropką (0.5),
// niezależnie od ustawień systemowych (ważne dla pól Amount w przepisach)
builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    var invariant = new[] { CultureInfo.InvariantCulture };
    opts.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
    opts.SupportedCultures = invariant;
    opts.SupportedUICultures = invariant;
});

builder.Services.AddDbContext<CookBookContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CookBookDb")));

// Generyczne repozytorium dla prostych encji słownikowych (Tag, Unit, DifficultyLevel...)
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Generyczny serwis słownikowy ({Id, Name}) — obsługuje wszystkie słowniki z LookupRegistry
builder.Services.AddScoped(typeof(LookupService<>));

// Repozytoria i serwisy (wzorzec: jedna para na encję)
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Wartości odżywcze składników z zewnętrznego LLM (Mistral), za wymiennym INutritionProvider
builder.Services.Configure<MistralOptions>(builder.Configuration.GetSection("Mistral"));
builder.Services.AddHttpClient<INutritionProvider, MistralNutritionProvider>();
builder.Services.AddScoped<INutritionService, NutritionService>();

builder.Services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
builder.Services.AddScoped<IShoppingListService, ShoppingListService>();
builder.Services.AddScoped<IShoppingListPdfService, ShoppingListPdfService>();

builder.Services.AddScoped<IMealPlanRepository, MealPlanRepository>();
builder.Services.AddScoped<IMealPlanService, MealPlanService>();

builder.Services.AddScoped<ICollectionService, CollectionService>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<CookBookContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.Initialize(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRequestLocalization();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();