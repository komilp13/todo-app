using TodoApp.Api.Endpoints;
using TodoApp.Api.Extensions;
using TodoApp.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services
    .AddSwaggerConfiguration()
    .AddCorsConfiguration(builder.Configuration)
    .AddApplicationServices()
    .AddInfrastructureServices();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Map endpoints
app.MapHealthEndpoints();

app.Run();
