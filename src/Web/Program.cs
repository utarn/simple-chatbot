using System.Net;
using System.Security.Claims;
using System.Text.Json;
using ChatbotApi.Application;
using ChatbotApi.Infrastructure;
using ChatbotApi.Infrastructure.Data;
using ChatbotApi.Web;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureSerilog();
builder.WebHost.UseKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1073741824; // 1GB
});
builder.Services.Configure<HostOptions>(hostOptions =>
{
    hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true,
        reloadOnChange: false)
    .AddJsonFile($"settings{Path.DirectorySeparatorChar}appsettings.json", optional: true,
        reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddWebServices();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddWebOptimizer(minifyJavaScript: false, minifyCss: false);
}
else
{
    // Add WebOptimizer to the service collection
    builder.Services.AddWebOptimizer(pipeline =>
    {
        // Configure the pipeline to minify JavaScript files
        pipeline.MinifyJsFiles("javascript/**/*.js");
    });
}

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.All });

// Configure the HTTP request pipeline.
await app.InitialiseDatabaseAsync();

app.UseHttpsRedirection();
app.UseSerilogRequestLogging(
    options =>
    {
        options.MessageTemplate =
            "{RemoteIpAddress} {RequestScheme} {RequestHost} {RequestMethod} {RequestPath} {UserName} {Email} {Role} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (
            diagnosticContext,
            httpContext) =>
        {
            string? header =
                (httpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ??
                 httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()) ??
                httpContext.Connection.RemoteIpAddress?.ToString();
            if (IPAddress.TryParse(header, out IPAddress? ip))
            {
                diagnosticContext.Set("RemoteIpAddress", ip);
            }

            diagnosticContext.Set("Email", httpContext.User.FindFirstValue(ClaimTypes.Email));
            diagnosticContext.Set("UserName",
                httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "-");
            diagnosticContext.Set("Role", httpContext.User.FindFirstValue(ClaimTypes.Role) ?? "-");
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
    });

app.UseForwardedHeaders();
app.UseHealthChecks("/health");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use((context, next) =>
{
    if (context.Request.Headers["x-forwarded-proto"] == "https")
    {
        context.Request.Scheme = "https";
    }

    var pathBase = builder.Configuration.GetConnectionString("PathBase");
    if (!string.IsNullOrEmpty(pathBase))
    {
        context.Request.PathBase = new PathString(pathBase);
    }
    return next();
});

// Use WebOptimizer
app.UseWebOptimizer();
app.UseStaticFiles();

app.UseCookiePolicy();

app.UseCors("javascript");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chatbots}/{action=Index}");

app.MapRazorPages();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/detail", new HealthCheckOptions
{
    // Use the default response writer, which returns a JSON response
    ResponseWriter = async (context, report) =>
    {
        var result = JsonSerializer.Serialize(
            new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    exception = e.Value.Exception?.Message,
                    duration = e.Value.Duration.ToString()
                })
            });
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }
});

app.UseExceptionHandler("/error");

app.MapFallbackToFile("index.html");

app.MapEndpoints();

app.Run();

public partial class Program
{
}
