using System.Security.Claims;

using BasicWebApp.Application.Members;
using BasicWebApp.Domain.Members;
using BasicWebApp.Infrastructure.EFCore;
using BasicWebApp.Web;

using D3Fragments.Application;
using D3Fragments.AspNetCore;
using D3Fragments.EFCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options => options.Conventions.AuthorizePage("/Members/Index"));
builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
{
    var messages = options.ModelBindingMessageProvider;
    messages.SetAttemptedValueIsInvalidAccessor((_, field) => $"{field}の入力値が不正です。");
    messages.SetValueIsInvalidAccessor(_ => "入力値が不正です。");
    messages.SetValueMustNotBeNullAccessor(_ => "値を入力してください。");
    messages.SetValueMustBeANumberAccessor(field => $"{field}には数値を入力してください。");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => "入力値が不正です。");
});
builder.Services.AddScoped<PageErrorHandler>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.TimeProvider = TimeProvider.System;
    options.Events.OnValidatePrincipal = async context =>
    {
        // 再起動でDBが消えた後の古いCookieを拒否する
        string? email = context.Principal?.FindFirstValue(ClaimTypes.Email);
        string? id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (email is not null && id is not null)
        {
            var findMember = context.HttpContext.RequestServices.GetRequiredService<IRequestExecutor<FindMemberByEmailAddressQuery, Member?>>();
            Member? member = await findMember.ExecuteAsync(new FindMemberByEmailAddressQuery { EmailAddress = email }, context.HttpContext.RequestAborted);
            if (member?.Id.ToString() == id)
            {
                return;
            }
        }
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    };
});

// Development以外ではAddD3FragmentsApplicationがTimeProvider.Systemを登録する（TryAddのため、ここでの登録が優先される）
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<DemoTimeProvider>();
    builder.Services.AddSingleton<TimeProvider>(services => services.GetRequiredService<DemoTimeProvider>());
}

string connectionString = $"Data Source=BasicWebApp-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
builder.Services.AddSingleton(_ => new SqliteConnection(connectionString));

// D3Fragments関連クラスの登録
builder.Services.AddD3FragmentsApplication([typeof(BasicWebApp.Application.Books.GetBookQuery).Assembly]);
builder.Services.AddD3FragmentsEFCore<BasicWebAppDbContext>(
    [typeof(MemberType).Assembly, typeof(BookRepository).Assembly],
    config => config.UseSqlite(connectionString));
builder.Services.AddD3FragmentsAspNetCore();

var app = builder.Build();

await app.Services.GetRequiredService<SqliteConnection>().OpenAsync();
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<BasicWebAppDbContext>().Database.EnsureCreatedAsync();
}

app.UseRequestLocalization("ja-JP");
app.UseExceptionHandler("/Error");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api => api.UseCsrfProtection());

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program;