using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using TextToSpeech.Api.Extensions;
using TextToSpeech.Api.Middleware;
using TextToSpeech.Core;
using TextToSpeech.Infra;
using TextToSpeech.Infra.Config;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.SignalR;
using static TextToSpeech.Infra.Config.ConfigConstants;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.SetConfig();

ConfigureLogging(builder);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddHttpContextAccessor();

builder.Services.AddServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebCorsPolicy",
        builder => builder.WithOrigins($"http://localhost:{AppConstants.ClientPort}",
        $"https://localhost:{AppConstants.ClientPort}",
        AppConstants.Domain)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errorDict = context.ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value!.Errors.Select(e =>
                    !string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.ErrorMessage : e.Exception?.Message
                        ?? string.Empty)
                .ToArray());

        var details = new ValidationProblemDetails(errorDict)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://httpstatuses.com/400",
            Title = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Validation errors: {@Errors}", details.Errors);

        return new BadRequestObjectResult(details);
    };
});

AddAuthentication(builder);
builder.Services.AddAuthorization();

builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection(SectionNames.EmailConfig));
builder.Services.Configure<NarakeetConfig>(builder.Configuration.GetSection(SectionNames.NarakeetConfig));
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection(SectionNames.JwtConfig));

var connectionString = builder.Configuration.GetConnectionString(ConnectionStrings.DbConnection);
builder.Services.AddDbContext<AppDbContext>(b => b.UseNpgsql(connectionString));

builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("WebCorsPolicy"); // CORS should be before other middleware

app.UseHealthChecks("/health");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();
app.MapHub<AudioHub>(Shared.AudioHubEndpoint);

using var scope = app.Services.CreateScope();

await scope.ServiceProvider.GetRequiredService<IDbInitializer>().Initialize();

scope.ServiceProvider.GetRequiredService<ILogger<Program>>()
    .LogInformation("Is test mode: {IsTestMode}", HostingEnvironment.IsTestMode());

await app.RunAsync();

static void ConfigureLogging(WebApplicationBuilder builder)
{
    var logFile = Path.Combine(builder.Configuration[AppDataPath]!, "logs", "log_.txt");

    builder.Host.UseSerilog((context, loggerConfig) =>
    {
        loggerConfig.Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(logFile, rollingInterval: RollingInterval.Day);

        if (HostingEnvironment.IsDevelopment())
        {
            return;
        }

        var elasticConfig = builder.Configuration.GetRequiredSection(nameof(ElasticsearchConfig))
            .Get<ElasticsearchConfig>()!;

        loggerConfig.WriteTo.Elasticsearch([new Uri(elasticConfig.Url)], opts =>
        {
            opts.DataStream = new DataStreamName("logs", AppConstants.AppName.ToLower(), HostingEnvironment.Current.ToLower());
            opts.BootstrapMethod = BootstrapMethod.Failure;
        }, transport =>
        {
            transport.Authentication(new BasicAuthentication(elasticConfig.Username, elasticConfig.Password));
        });
    });
}

static void AddAuthentication(WebApplicationBuilder builder)
{
    var jwt = builder.Configuration.GetSection(SectionNames.JwtConfig).Get<JwtConfig>()
        ?? throw new InvalidOperationException("Jwt configuration is missing or invalid.");

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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            options.Events.OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/audioHub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            };
        });
}