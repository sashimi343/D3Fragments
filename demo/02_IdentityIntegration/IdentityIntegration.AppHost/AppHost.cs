var builder = DistributedApplication.CreateBuilder(args);

// Destination of emails. Sent emails can be checked in the Mailpit web UI
var mailpit = builder.AddMailPit("mailpit");

// OIDC provider for external login. The port is fixed because both the browser and the app refer to the same URL.
// Imports the realm in the Realms folder (the client and the demo user) at startup
var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithRealmImport("./Realms");

builder.AddDotnetProject("web", "../IdentityIntegration.Web/IdentityIntegration.Web.csproj")
    .WithReference(mailpit)
    .WithReference(keycloak)
    // For the link that opens Mailpit from the pages
    .WithEnvironment("MAILPIT_UI_URL", mailpit.GetEndpoint("http"))
    .WaitFor(mailpit)
    .WaitFor(keycloak);

builder.Build().Run();
