var builder = DistributedApplication.CreateBuilder(args);

// メールの送信先。送ったメールは Mailpit の Web 画面で確認する
var mailpit = builder.AddMailPit("mailpit");

// 外部ログインの OIDC プロバイダー。ブラウザからもアプリからも同じ URL で参照するため、ポートを固定する。
// Realms フォルダーの realm（クライアントとデモ用のユーザー）を起動時に取り込む
var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithRealmImport("./Realms");

builder.AddDotnetProject("web", "../IdentityIntegration.Web/IdentityIntegration.Web.csproj")
    .WithReference(mailpit)
    .WithReference(keycloak)
    // 画面から Mailpit を開くリンク用
    .WithEnvironment("MAILPIT_UI_URL", mailpit.GetEndpoint("http"))
    .WaitFor(mailpit)
    .WaitFor(keycloak);

builder.Build().Run();
