using Internship_Backend_cpt.Services.Main;
using Internship_Backend_cpt.Services.MsSql;
using Internship_Backend_cpt.Services.Postgres;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Internship_cpt.Session";
    options.IdleTimeout = TimeSpan.FromHours(1);
});


builder.Services.AddDistributedMemoryCache();

builder.Services.AddControllers();

builder.Services.AddScoped<MsSqlService>();
builder.Services.AddScoped<ComparerService>();
builder.Services.AddScoped<PostgreSqlService>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<MsSqlService>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApi", Version = "v1" });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi v1");
    c.RoutePrefix = string.Empty;
});


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();