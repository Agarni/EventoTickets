using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Linq;
using EventoTickets.Server.Data;
using EventoTickets.Server.Hubs;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

var configuration = builder.Configuration;
var connectionDbContext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "EventoDbContext" : "EventoDbContextUnix";
var localDB = Path.GetDirectoryName(configuration.GetConnectionString(connectionDbContext)
    .Replace("Filename=", ""));
if (!Directory.Exists(localDB))
{
    _ = Directory.CreateDirectory(localDB);
}
builder.Services.AddDbContext<EventoDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString(connectionDbContext)));

var app = builder.Build();

// Pipeline de requisição
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseResponseCompression();

app.MapRazorPages();
app.MapControllers();
app.MapHub<EventoHub>("/eventohub");
app.MapFallbackToFile("index.html");

app.Run();
