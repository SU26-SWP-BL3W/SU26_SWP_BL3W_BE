using SEAL_Backend.Extensions;
using SEAL_Backend.Middlewares;
using SEAL_Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Application, Infrastructure, Database & External Services
builder.Services.AddApplicationCoreServices(builder.Configuration);

// 2. Register Authentication (JWT) & Security
builder.Services.AddJwtAuthentication(builder.Configuration);

// 3. Register CORS Policy
builder.Services.AddCustomCorsPolicy();

// 4. Register Controllers & Swagger Documentation
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// 5. Database Auto-Seeding
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình Seeding cơ sở dữ liệu.");
    }
}

// 6. HTTP Pipeline Middlewares
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SEAL API v1");
    options.DisplayRequestDuration();
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

