using temp_clean_arch.Infrastructure.Data;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

// Add Duende IdentityServer
builder.Services.AddIdentityServer()
    .AddInMemoryClients(temp_clean_arch.Web.Config.Clients)
    .AddInMemoryIdentityResources(temp_clean_arch.Web.Config.IdentityResources)
    .AddInMemoryApiScopes(temp_clean_arch.Web.Config.ApiScopes)
    .AddAspNetIdentity<temp_clean_arch.Infrastructure.Identity.ApplicationUser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(static builder =>
    builder.AllowAnyMethod()
        .AllowAnyHeader()
        .AllowAnyOrigin());


app.UseFileServer();

// Add IdentityServer middleware
app.UseIdentityServer();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });



app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);
app.MapGroup("/api").WithGroup<temp_clean_arch.Web.Endpoints.AdminUsers>();

app.MapFallbackToFile("index.html");

app.Run();
