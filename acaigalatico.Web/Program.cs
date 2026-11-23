using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using acaigalatico.Infrastructure.Context;
using acaigalatico.Application.Interfaces;
using acaigalatico.Application.Services;
using acaigalatico.Web.Services;
using acaigalatico.Infrastructure.Repositories;
using acaigalatico.Domain.Interfaces;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURAÇÃO DO BANCO ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    "Data Source=AcaiGalatico.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// --- IDENTITY ---
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

// --- DEPENDENCY INJECTION ---
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IVendaRepository, VendaRepository>();
builder.Services.AddScoped<IVendaService, VendaService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPagamentoService, PagamentoService>();
builder.Services.AddSingleton<IConteudoSiteService, ConteudoSiteService>();
builder.Services.AddScoped<acaigalatico.Infrastructure.SeedingService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddAntiforgery(options => 
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// --- HTTP CLIENT ---
builder.Services.AddHttpClient();

// --- SWAGGER ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- FIX PARA PREVIEW NO IDE (ERR_ABORTED) ---
// Só em desenvolvimento: em produção o site não pode ser carregado dentro de iframes de outros domínios (clickjacking).
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() => {
            context.Response.Headers.Remove("X-Frame-Options");
            context.Response.Headers.Remove("Frame-Options");
            context.Response.Headers["Content-Security-Policy"] = "frame-ancestors *";
            return Task.CompletedTask;
        });
        await next();
    });
}

// --- SEED AUTOMÁTICO ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        var seeding = services.GetRequiredService<acaigalatico.Infrastructure.SeedingService>();
        // A senha do administrador inicial vem da configuração (user-secrets ou variável de ambiente).
        // Para quem clona o projeto conseguir ver o painel, em DESENVOLVIMENTO existe uma senha de
        // demonstração (documentada no README). Em produção ela nunca é usada: sem AdminSeed:Password
        // configurada, o administrador não é criado.
        const string SenhaDemonstracao = "admin123";
        var senhaAdmin = app.Configuration["AdminSeed:Password"];
        if (string.IsNullOrWhiteSpace(senhaAdmin) && app.Environment.IsDevelopment())
        {
            senhaAdmin = SenhaDemonstracao;
        }
        seeding.SeedAsync(senhaAdmin).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Falha no Seed: {ex.Message}");
    }
}

// --- CULTURA ---
var defaultCulture = new CultureInfo("pt-BR");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

// --- SWAGGER PIPELINE (somente em desenvolvimento) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Açaí Galáctico API v1");
    });
}

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
