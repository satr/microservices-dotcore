using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromHours(4);
});

// ----- Authentication: Cookie + OpenID Connect (Keycloak) -----
// The browser navigates to Keycloak at Authority (localhost:8888).
// Server-side OIDC discovery fetches from MetadataAddress (docker-internal keycloak:8080).
var kcAuthority = builder.Configuration["Keycloak:Authority"]
                  ?? "http://localhost:8888/realms/library";
var kcMetadata  = builder.Configuration["Keycloak:MetadataAddress"]
                  ?? "http://localhost:8888/realms/library/.well-known/openid-configuration";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name     = "LibraryAuth";
        options.Cookie.HttpOnly = true;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan    = TimeSpan.FromHours(4);
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority        = kcAuthority;      // used for browser redirect URLs
        options.MetadataAddress  = kcMetadata;       // used for server-side OIDC discovery
        options.ClientId         = "library-frontend";
        options.ResponseType     = OpenIdConnectResponseType.Code;
        options.SaveTokens       = true;             // stores access_token in the auth cookie
        options.RequireHttpsMetadata = false;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");

        // Map Keycloak claims to ASP.NET Core equivalents
        options.ClaimActions.MapJsonKey("preferred_username", "preferred_username");
        options.ClaimActions.MapJsonKey("roles", "roles");

        options.TokenValidationParameters.RoleClaimType = "roles";
        options.TokenValidationParameters.NameClaimType = "preferred_username";
    });

builder.Services.AddAuthorization();
// ---------------------------------------------------------------

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("frontend"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://jaeger:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            });
    });

builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Login: triggers OIDC challenge → redirects to Keycloak
app.MapGet("/login", async ctx =>
{
    if (ctx.User.Identity?.IsAuthenticated == true)
        ctx.Response.Redirect("/");
    else
        await ctx.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" });
});

// Logout: clears cookie and ends Keycloak session
app.MapPost("/logout", async ctx =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" });
});

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
