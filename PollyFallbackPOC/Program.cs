using Polly;
using Serilog;
using PollyFallbackPOC.Services;
using Serilog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ILoggerFactory>(loggerFactory =>
{
    var serilogLogger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .CreateLogger();

    return new SerilogLoggerFactory(serilogLogger);
});

builder.Services.AddHttpClient<IUsersService, UsersService>(client =>
{
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
});

builder.Services.AddSingleton<IAsyncPolicy<string>>(serviceProvider =>
{
    return Policy<string>
        .Handle<Exception>()
        .FallbackAsync(async (cancellationToken) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<UsersService>>();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();

            httpClient.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");

            //Execute the request but this time with the right resource
            var response = await httpClient.GetStringAsync("/users", cancellationToken);

            return response;
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
