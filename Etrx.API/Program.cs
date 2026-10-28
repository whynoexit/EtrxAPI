using Etrx.API.Extensions;
using Etrx.API.Middlewares;
using Serilog;
using Etrx.API.Security;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Host.AddSerilog();

builder.Services.ConfigureControllers();
builder.Services.AddEtrxAuthentication(configuration, builder.Environment.IsDevelopment());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddServices();
builder.Services.AddBackgroundServices();
builder.Services.AddRepositories();
builder.Services.ConfigureOptions(configuration);
builder.Services.AddDbContexts(configuration);
builder.Services.AddAutoMapper(cfg => { }, AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.ConfigureSwagger();
}

await app.ApplyMigrationsAsync();

// Custom header + explicit CORS allowlist prevent cross-origin browser mutations.
app.UseCors();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method) && context.Request.Headers["X-ETRX-Request"] != "1")
    { context.Response.StatusCode = 400; return; }
    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();
