using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using Stronghold.EnterpriseEstimating.Api.Authorization;
using Stronghold.EnterpriseEstimating.Api.Domain;
using Stronghold.EnterpriseEstimating.Api.Services;
using Stronghold.EnterpriseEstimating.Data;
using ZymLabs.NSwag.FluentValidation;

var builder = WebApplication.CreateBuilder(args);
var isLocal = builder.Environment.IsEnvironment("Local") || builder.Environment.IsDevelopment();
var skipDatabaseInitialization = string.Equals(
    Environment.GetEnvironmentVariable("SkipDatabaseInitialization"),
    "true",
    StringComparison.OrdinalIgnoreCase
);
var hasSqlConnectionString = !string.IsNullOrWhiteSpace(
    builder.Configuration.GetConnectionString("SqlDb")
);

if (isLocal)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

// Add appsettings.{environment}.json override
builder.Configuration
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true);

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "stronghold-estimating",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "stronghold-estimating",
            IssuerSigningKey = jwtKey,
        };
    });

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Application services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Versioning
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning();
builder.Services.AddVersionedApiExplorer(opt =>
{
    opt.GroupNameFormat = "'v'V";
    opt.SubstituteApiVersionInUrl = true;
});

// NSwag — Bearer token auth for Swagger UI
builder.Services.AddSingleton<FluentValidationSchemaProcessor>();
builder.Services.AddOpenApiDocument(
    (configure, serviceProvider) =>
    {
        configure.Version = "v1";
        configure.DocumentName = "v1";
        configure.ApiGroupNames = new[] { "v1" };
        configure.Title = "Stronghold Enterprise Estimating API";
        configure.SchemaProcessors.Add(serviceProvider.GetService<FluentValidationSchemaProcessor>());
        configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
        configure.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token.",
        });
    }
);

// Database
#if DEBUG
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options
        .UseSqlServer(
            builder.Configuration.GetConnectionString("SqlDb"),
            o => o.EnableRetryOnFailure().UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        )
        .EnableSensitiveDataLogging()
);
#else
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlDb"),
        o => o.EnableRetryOnFailure().UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    )
);
#endif

builder.Services.AddHttpContextAccessor();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

var app = builder.Build();

if (isLocal)
{
    app.UseOpenApi();
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
    app.UseSwaggerUi3();
}
else
{
    app.UseHsts();
}

if (hasSqlConnectionString && !skipDatabaseInitialization)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    DbInitializer.Initialize(context);
}

app.UseCors(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
