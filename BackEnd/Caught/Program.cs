using Caught;
using Caught.Data;
using Caught.Services;
using Dapper;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Caught");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Caught is not configured.");
}

DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(ConnectionStrings.Normalize(connectionString)));
builder.Services.AddScoped<Db>();
builder.Services.AddScoped<CreatorService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<VideoService>();
builder.Services.AddSingleton<ProductClickBuffer>();
builder.Services.AddHostedService<ProductClickFlushService>();
builder.Services.AddScoped<SaleAlertService>();
builder.Services.AddScoped<ProductClickService>();
builder.Services.AddScoped<UnsuccessfulSearchService>();
builder.Services.AddScoped<ShelfService>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        await Results.Problem(
            title: "The request could not be completed.",
            detail: "The request could not be completed.",
            statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context);
    });
});

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (ApiException exception)
    {
        await Results.Problem(
            title: exception.Message,
            detail: exception.Message,
            statusCode: exception.Status).ExecuteAsync(context);
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
