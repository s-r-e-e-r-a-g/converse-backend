using Converse.Data;
using Converse.Hubs;
using Converse.Events;
using Converse.Services.Chat;
using Converse.Services.User;
using Converse.Services.Group;
using Converse.Services.Message;

using System.IO;
using System.Text;
using MongoDB.Driver;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Ensure logs directory exists
string logFilePath = "logs/app.log";
if (!Directory.Exists("logs"))
{
    Directory.CreateDirectory("logs");
}

// Simple function to log messages to a file
void LogToFile(string message)
{
    var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | {message}\n";
    File.AppendAllText(logFilePath, logEntry);
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var hostURL = configuration.GetValue<string>("HostURL");
var frontendurl = configuration.GetValue<string>("frontendurl");

// Initialize MongoDB Client and Database
var mongoClient = new MongoClient(configuration.GetValue<string>("MongoDB:ConnectionString"));
var mongoDatabase = mongoClient.GetDatabase(configuration.GetValue<string>("MongoDB:DatabaseName"));

builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton(mongoDatabase);

// Register database classes
builder.Services.AddSingleton<UserDb>();
builder.Services.AddSingleton<GroupDb>();
builder.Services.AddSingleton<MessageDb>();

builder.Services.AddSignalR();

builder.Services.AddScoped<IEventBus, EventBus>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ConnectionService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<GroupManagementService>();


var allowedOrigins = new[] { hostURL, frontendurl };
Console.WriteLine("\n Allowed Origins: " + string.Join(", ", allowedOrigins));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


// JWT Authentication Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                LogToFile($"SignalR connection request to {path}");

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                {
                    LogToFile($"Received token: {accessToken}");
                    context.Token = accessToken;
                }
                else
                {
                    LogToFile("No token found in request.");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.FindFirst(ClaimTypes.Name)?.Value;
                LogToFile($"Token successfully validated for user: {userId}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                LogToFile($"Token validation failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Global Exception Handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        LogToFile("An unexpected error occurred.");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An unexpected error occurred.");
    });
});

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowMyApp");

// Apply authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", async context =>
{
    await context.Response.SendFileAsync(Path.Combine("wwwroot", "index.html"));
});

app.MapHub<ChatHub>("/chathub");

// Map controllers for API endpoints
app.MapControllers();

app.Run();