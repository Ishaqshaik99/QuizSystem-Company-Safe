using System.Reflection;
using Microsoft.OpenApi;
using QuizSystem.Api.Middleware;
using QuizSystem.Infrastructure.DependencyInjection;
using QuizSystem.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);
var seedOnly = args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("UiPolicy", cors =>
    {
        cors
            .WithOrigins(
                builder.Configuration.GetValue<string>("Ui:BaseUrl") ?? "https://localhost:7002",
                "http://localhost:5204",
                "https://localhost:7002")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QuizSystem API",
        Version = "v1",
        Description = "Online Quiz & Examination System API"
    });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT token in the format: Bearer {token}"
    };

    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [document.Components.SecuritySchemes["Bearer"]] = Array.Empty<string>()
    });

    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("UiPolicy");

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "QuizSystem API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", utc = DateTime.UtcNow }));

await DatabaseSeeder.SeedAsync(app.Services);

if (!seedOnly)
{
    app.Run();
}
