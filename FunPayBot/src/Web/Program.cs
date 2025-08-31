using FunPayBot.src.Domain.Entities;
using FunPayBot.src.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "FunPayBot")
        .WriteTo.Console()
        .WriteTo.Seq("http://localhost:5341"));

builder.Services.Configure<FunPaySettings>(
    builder.Configuration.GetSection("FunPaySettings"));
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddDbContext<FunPayBotDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient();

builder.Services.AddHttpClient("PythonAPI", (serviceProvider, client) =>
{
    var funPaySettings = serviceProvider.GetRequiredService<IOptions<FunPaySettings>>().Value;
    client.BaseAddress = new Uri(funPaySettings.PythonApiUrl);
});
builder.Services.Configure<FunPaySettings>(
    builder.Configuration.GetSection("FunPaySettings"));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FunPayBot API", Version = "v1" });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

// Swagger только в Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FunPayBot API v1");
        c.RoutePrefix = "swagger";  
    });
}
app.MapGet("/index.html", () => Results.Redirect("/"));
app.MapRazorPages();

app.MapControllers();
app.MapFallbackToPage("/Index");
app.Run();