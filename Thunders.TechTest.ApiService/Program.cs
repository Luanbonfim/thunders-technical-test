using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rebus.Config;
using Rebus.Handlers;
using Thunders.TechTest.ApiService.Data;
using Thunders.TechTest.ApiService.Handlers;
using Thunders.TechTest.ApiService.Messages;
using Thunders.TechTest.ApiService.Middleware;
using Thunders.TechTest.ApiService.Repositories;
using Thunders.TechTest.ApiService.Services;
using Thunders.TechTest.OutOfBox.Queues;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
   
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Toll Usage API", Version = "v1" });
    c.EnableAnnotations();
});

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Configure DbContext
builder.Services.AddDbContext<TollUsageDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable(builder.Configuration.GetValue<string>("EnvironmentVariables:DatabaseConnection"));
    options.UseSqlServer(connectionString,
        x => x.MigrationsAssembly("Thunders.TechTest.ApiService"));
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

// Run migrations before registering services
using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TollUsageDbContext>();

    // Get pending migrations
    var pendingMigrations = await db.Database.GetPendingMigrationsAsync();

    if (pendingMigrations.Any())
        await db.Database.MigrateAsync();
}

// Configure Rebus
builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseRabbitMq(
        Environment.GetEnvironmentVariable(builder.Configuration.GetValue<string>("EnvironmentVariables:RabbitMqConnection")),
        builder.Configuration.GetValue<string>("QueueSettings:TollUsageQueueName") ?? "toll-usage-queue")));

// Register services
builder.Services.AddScoped<ITollUsageRepository, TollUsageRepository>();
builder.Services.AddScoped<ITollUsageService, TollUsageService>();
builder.Services.AddScoped<IHandleMessages<TollUsageMessage>, TollUsageMessageHandler>();
builder.Services.AddScoped<IHandleMessages<ReportGenerationMessage>, TollUsageMessageHandler>();
builder.Services.AddScoped<IMessageSender, RebusMessageSender>();
builder.Services.AddScoped<ITimeoutService, TimeoutService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Toll Usage API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.Use(async (context, next) =>
{
    int timeoutSeconds = builder.Configuration.GetValue<int>("ApiSettings:TimeoutInSeconds");
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)); // Set timeout
    context.RequestAborted = cts.Token; // Assign cancellation token

    try
    {
        await next(context); // Process request
    }
    catch (OperationCanceledException)
    {
        context.Response.StatusCode = StatusCodes.Status408RequestTimeout; // 408 Timeout
    }
});

app.UseHttpsRedirection();
app.UseAuthorization();


// Use exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();
