using ExpenseSplitterAPI.Data;
using ExpenseSplitterAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Configure Database Connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ✅ Enable CORS for React (Adjust if needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:3000") // Allow frontend origin
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()); // Allow cookies if needed
});

// ✅ Register JwtAuthenticationService
builder.Services.AddScoped<JwtAuthenticationService>(); // 🔥 Fix added

// ✅ Configure Authentication with JWT Bearer
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is missing from appsettings.json");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Set to true in production
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), // 🔑 Ensure Key is set
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, // ✅ Ensure tokens expire
            ClockSkew = TimeSpan.Zero, // Optional: Removes default 5-min leeway
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Splitter API v1");
    c.RoutePrefix = string.Empty; // Access Swagger at root URL
});

app.UseHttpsRedirection();
app.UseCors("AllowReactApp"); // ✅ Apply CORS before Authentication

app.UseAuthentication(); // ✅ Enable Authentication
app.UseAuthorization();  // ✅ Enable Authorization

app.MapControllers();
app.Run();
