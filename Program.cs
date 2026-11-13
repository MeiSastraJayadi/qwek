using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Qwek.Applications.Commons;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Ports;
using Qwek.Applications.UseCases;
using Qwek.Infrastructures;
using Qwek.Interfaces.Endpoints;
using Qwek.Interfaces.RealTime;
using Qwek.Interfaces.Web;


var builder = WebApplication.CreateBuilder(args);

StorageOptions storageOptions = builder.Configuration
    .GetSection(StorageOptions.SectionName)
    .Get<StorageOptions>() ?? new StorageOptions();

string uploadsRoot = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, storageOptions.Local.Path));
string uploadsRequestPath =
    "/" + storageOptions.Local.PublicBasePath.Trim('/');

Directory.CreateDirectory(uploadsRoot);

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
            },
            OnTokenValidated = async context =>
            {
                string? userId = context.Principal?
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    context.Fail("Invalid token");
                    return;
                }

                string? rawVersion = context.Principal?
                    .FindFirstValue(AuthClaims.TokenVersion);
                int tokenVersion = int.TryParse(rawVersion, out int parsed)
                    ? parsed
                    : 0;

                IIdentityService identity = context.HttpContext
                    .RequestServices.GetRequiredService<IIdentityService>();
                int currentVersion = await identity.GetTokenVersionAsync(
                    userId, context.HttpContext.RequestAborted);

                if (currentVersion != tokenVersion)
                {
                    context.Fail("Token has been revoked");
                }
            }
        };

    });

builder.Services.AddAuthorization(); 
builder.Services.AddSignalR();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    foreach (string proxy in builder.Configuration
        .GetSection("ForwardedHeaders:KnownProxies")
        .Get<string[]>() ?? [])
    {
        if (IPAddress.TryParse(proxy, out IPAddress? address))
        {
            options.KnownProxies.Add(address);
        }
    }
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPolicy.Create(context, permitLimit: 300));

    options.AddPolicy(RateLimitPolicy.Auth, context =>
        RateLimitPolicy.Create(context, permitLimit: 10));
    options.AddPolicy(RateLimitPolicy.Messages, context =>
        RateLimitPolicy.Create(context, permitLimit: 60));
    options.AddPolicy(RateLimitPolicy.Uploads, context =>
        RateLimitPolicy.Create(context, permitLimit: 20));
});

string[] allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "")
    .Split(
        ',',
        StringSplitOptions.RemoveEmptyEntries |
        StringSplitOptions.TrimEntries);

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}


builder.Services.AddW3CLogging(logging =>
{
    logging.LogDirectory = Path.Combine(builder.Environment.ContentRootPath, "LogFiles");
    logging.FileName = "w3c";
    logging.FileSizeLimit = 10 * 1024 * 1024;
    logging.RetainedFileCountLimit = 10;
    logging.FlushInterval = TimeSpan.FromSeconds(5);
    logging.LoggingFields =
        W3CLoggingFields.All & ~W3CLoggingFields.UriQuery;
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor(); 
builder.Services.AddScoped<ICurrentUser, CurrentUser>(); 
builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();
builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddSingleton<HubRateLimiter>();

builder.Services.AddScoped<Auth>(); 
builder.Services.AddScoped<Servers>(); 
builder.Services.AddScoped<Channels>(); 
builder.Services.AddScoped<Messages>();
builder.Services.AddScoped<Users>();

var app = builder.Build();

FileExtensionContentTypeProvider contentTypeProvider = new();
contentTypeProvider.Mappings[".md"] = "text/markdown";
contentTypeProvider.Mappings[".webm"] = "video/webm";
contentTypeProvider.Mappings[".webp"] = "image/webp";
contentTypeProvider.Mappings[".mov"] = "video/quicktime";
contentTypeProvider.Mappings[".ogg"] = "audio/ogg";
contentTypeProvider.Mappings[".wav"] = "audio/wav";
contentTypeProvider.Mappings[".mp3"] = "audio/mpeg";

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (!storageOptions.Private)
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsRoot),
        RequestPath = uploadsRequestPath,
        ContentTypeProvider = contentTypeProvider,
        DefaultContentType = "application/octet-stream",
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers["X-Content-Type-Options"] =
                "nosniff";
        }
    });
}

app.UseAuthentication();
app.UseAuthorization();

if (allowedOrigins.Length > 0)
{
    app.UseCors();
}

app.UseRateLimiter();
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
app.MapFileEndpoints(storageOptions.PrivateBasePath);
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

