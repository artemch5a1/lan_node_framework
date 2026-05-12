using System.Net;
using Backend.Application;
using Backend.Infrastructure;
using DistributedLocalSystem.Root.Bootstrap;
using DistributedLocalSystem.Infrastructure.Attributes;
using DistributedLocalSystem.Infrastructure.Middleware;
using Microsoft.OpenApi.Models;

static string ResolveLocalHttpUrl(string[] args)
{
    foreach (string key in new[] { "BACKEND_HTTP_BASE_URL", "LOCAL_HTTP_BASE" })
    {
        string? v = Environment.GetEnvironmentVariable(key)?.Trim();
        if (
            !string.IsNullOrEmpty(v)
            && Uri.TryCreate(v, UriKind.Absolute, out Uri? u)
            && u.Scheme == Uri.UriSchemeHttp
        )
            return $"{u.Scheme}://{u.Authority}";
    }

    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == "--urls" && Uri.TryCreate(args[i + 1], UriKind.Absolute, out Uri? au))
            return $"{au.Scheme}://{au.Authority}";
    }

    return "http://127.0.0.1:5000";
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedLocalSystemCore(builder.Configuration);

builder.Services.Configure<ClientHostProxyOptions>(
    builder.Configuration.GetSection("ClientHostProxy")
);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "'Bearer {}' .",
        }
    );

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

string localHttpUrl = ResolveLocalHttpUrl(args);
int lanPort = builder.Configuration.GetValue("Net:LanPort", 17891);
builder.WebHost.UseUrls(localHttpUrl, $"http://0.0.0.0:{lanPort}");

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

WebApplication app = builder.Build();
app.UseCors();
app.UseDistributedLocalSystemCoreProxy();

app.MapControllers();

app.MapGet(
    "/greet",
    (string? name) =>
    {
        string safeName = string.IsNullOrWhiteSpace(name) ? "Anonymous" : name.Trim();
        return Results.Text($"Hello, {safeName}! You've been greeted from C#!");
    }
);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Backend.API v1");
        c.RoutePrefix = "swagger";
    });
}

app.MapGet("/health", () => Results.Text("OK")).WithMetadata(new NotRedirect());

app.Run();
