//using BlazorApp.Client.Pages;
//using BlazorApp.Client.Services;
using BlazorApp.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Server; 


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
//builder.Services.AddScoped<WorkspaceOverviewService>();
builder.Services.AddScoped<BlazorApp.Client.Services.WorkspaceOverviewService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.UseEndpoints(endpoints =>
{
    // static assets + Razor Components endpoints via endpoint routing
    endpoints.MapStaticAssets();
    endpoints.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(BlazorApp.Client._Imports).Assembly);

    // endpoint de test
    endpoints.MapGet("/health", () => Results.Text("ok"));
});

app.Run();

