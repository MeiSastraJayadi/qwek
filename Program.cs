using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using RealTimeApps.Applications.Commons.Options;
using RealTimeApps.Applications.Ports;
using RealTimeApps.Applications.UseCases;
using RealTimeApps.Infrastructures;
using RealTimeApps.Interfaces.Endpoints;
using RealTimeApps.Interfaces.RealTime;
using RealTimeApps.Interfaces.Web;


var builder = WebApplication.CreateBuilder(args);

string uploadsRoot = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(Path.Combine(uploadsRoot, "avatars"));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);

JwtOptions jwt = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt settings are missing");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true, 
          ValidIssuer = jwt.Issuer, 
          ValidateAudience = true, 
          ValidAudience = jwt.Audience,   
          ValidateIssuerSigningKey = true, 
          IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt.Key)
          ), 
          ValidateLifetime = true, 
          ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                string? accessToken = context.Request.Query["access_token"];      

                if(!string.IsNullOrWhiteSpace(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddAuthorization(); 
builder.Services.AddSignalR();


builder.Services.AddW3CLogging(logging =>
{
    logging.LogDirectory = Path.Combine(builder.Environment.ContentRootPath, "LogFiles");
    logging.FileName = "w3c";
    logging.FileSizeLimit = 10 * 1024 * 1024;
    logging.RetainedFileCountLimit = 10;
    logging.FlushInterval = TimeSpan.FromSeconds(5);
    logging.LoggingFields = W3CLoggingFields.All;
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor(); 
builder.Services.AddScoped<ICurrentUser, CurrentUser>(); 
builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();
builder.Services.AddSingleton<PresenceTracker>();

builder.Services.AddScoped<Auth>(); 
builder.Services.AddScoped<Servers>(); 
builder.Services.AddScoped<Channels>(); 
builder.Services.AddScoped<Messages>();
builder.Services.AddScoped<Users>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.UseAuthentication();
app.UseAuthorization();
app.UseW3CLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapAuthEndpoints(); 
app.MapServerEndpoints(); 
app.MapChannelEndpoints(); 
app.MapMessageEndpoints();
app.MapUserEndpoints(); 
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

