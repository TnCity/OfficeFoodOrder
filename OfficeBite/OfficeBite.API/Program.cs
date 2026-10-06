using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeBite.BLL.Services;
using OfficeBite.DAL.Data;

var builder = WebApplication.CreateBuilder(args);

// Controllers & Views
builder.Services.AddControllersWithViews();

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OfficeBite API",
        Version = "v1",
        Description = "Office Food Ordering API"
    });

    // JWT Authentication in Swagger
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Enter your JWT token like: Bearer {your token}"
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

// Database
builder.Services.AddDbContext<OfficeBiteDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"));
    options.ConfigureWarnings(w =>
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Business Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<OrderService>();

// JWT Authentication
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]!))
            };
    });

var app = builder.Build();

// Swagger (available at /swagger)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "OfficeBite API v1");
    options.RoutePrefix = "swagger"; // Swagger UI at /swagger, leaving / for the Home Web page
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

// Auto-apply EF migrations on startup and ensure Admin users
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OfficeBiteDbContext>();
    try
    {
        db.Database.Migrate();
        Console.WriteLine("[DB] Migrations applied successfully.");

        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
        await authService.EnsureAdminUsersAsync();
        Console.WriteLine("[DB] Admin roles verified.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB Startup Error]: {ex.Message}");
    }
}

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=MenuWeb}/{action=Index}/{id?}");

app.Run();