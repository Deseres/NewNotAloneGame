using NotAlone.Services;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NotAlone.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Add database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configure Identity
builder.Services.AddIdentityCore<User>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

// 3. Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];
var key = Encoding.ASCII.GetBytes(secretKey ?? throw new InvalidOperationException("JWT SecretKey not configured"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 4. Add controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Allow unknown enum values to fail instead of defaulting
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5. Register game services
builder.Services.AddScoped<GameStore>();
builder.Services.AddScoped<GameEngine>();
builder.Services.AddScoped<CreatureLogic>();
builder.Services.AddScoped<SurvivalService>();
builder.Services.AddScoped<TradeService>();

var app = builder.Build();

// 6. Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    // Seed demo user if it doesn't exist
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var demoUser = await userManager.FindByEmailAsync("demo@notalone.com");
    if (demoUser == null)
    {
        var newUser = new User
        {
            UserName = "demo",
            Email = "demo@notalone.com",
            DisplayName = "Demo Player",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(newUser, "Demo123!");
    }
}

// 7. Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();