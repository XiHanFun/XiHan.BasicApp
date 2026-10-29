// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;

namespace XiHan.BasicApp.Integration.Tests.Smoke;

/// <summary>
/// 验证 Hub / 静态资源与 <c>UseXiHan</c> 的共存。
/// </summary>
/// <remarks>
/// 方案 9.1 第二项。
/// 结论：Hub 端点 <c>BypassXiHanPipeline()</c> + ASP.NET Core <c>AddJwtBearer</c>（与 <c>Security.Jwt</c> 共用密钥）排在 <c>UseXiHan()</c> 之前；静态文件中间件也排在它之前。
/// 绕过的端点没有调用上下文：不受截止时间约束，内核应用停止后仍可用；<c>/api</c> 照旧受管。
/// </remarks>
public sealed class HubCoexistenceSmokeTests : IDisposable
{
    private readonly SqliteDatabase _database = new();
    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), "xihan-basicapp-static-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Hub_WithAspNetCoreJwt_AndStaticFiles_BypassTheKernelPipeline()
    {
        Directory.CreateDirectory(_webRoot);
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "app.js"), "console.log('static');");

        await using var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            configure: static builder =>
            {
                builder.Services.AddSignalR();
                builder.Services.AddAuthorization();
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                {
                    // 与 Security.Jwt 同一密钥、签发者与受众；保留原始声明名，Hub 里按 sub 认人
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestTokens.Secret)),
                        ValidIssuer = TestTokens.Issuer,
                        ValidAudience = TestTokens.Audience,
                        NameClaimType = "sub",
                    };
                    options.Events = new JwtBearerEvents
                    {
                        // SignalR 客户端把令牌放在查询串 access_token 里
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        },
                    };
                });
            },
            configureApp: app =>
            {
                // 排在 UseXiHan() 之前：静态文件与 ASP.NET Core 认证不经内核桥接
                app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(_webRoot), RequestPath = "/assets" });
                app.UseAuthentication();
                app.UseAuthorization();
            },
            afterMap: static app => PipelineBypassMetadata.BypassXiHanPipeline(app.MapHub<EchoHub>("/hubs/echo").RequireAuthorization()));
        await server.SeedDefaultsAsync();
        var token = TestTokens.Issue("alice", "acme", "admin");

        // Hub：长连接经 ASP.NET Core 认证，Hub 里能认出主体
        await using var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/echo", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
        await connection.StartAsync();
        Assert.Equal("hello", await connection.InvokeAsync<string>("Echo", "hello"));
        Assert.Equal("alice", await connection.InvokeAsync<string>("Who"));

        // 未认证的连接被 ASP.NET Core 授权拒绝（401），不是内核的 Problem Details
        var negotiate = await server.Client.PostAsync("/hubs/echo/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, negotiate.StatusCode);

        // 静态文件在 UseXiHan 之前服务
        var asset = await server.Client.GetAsync("/assets/app.js");
        Assert.Equal(HttpStatusCode.OK, asset.StatusCode);
        Assert.Equal("text/javascript", asset.Content.Headers.ContentType!.MediaType);

        // 截止时间：绕过的端点没有调用上下文、不受 xihan-deadline 约束；/api 照旧 504
        var expired = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O", CultureInfo.InvariantCulture);
        var negotiateWithDeadline = BasicAppTestServer.Request(HttpMethod.Post, "/hubs/echo/negotiate?negotiateVersion=1", token: token);
        negotiateWithDeadline.Headers.Add(HttpInvocation.DeadlineHeader, expired);
        Assert.Equal(HttpStatusCode.OK, (await server.Client.SendAsync(negotiateWithDeadline)).StatusCode);
        var apiWithDeadline = BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", token);
        apiWithDeadline.Headers.Add(HttpInvocation.DeadlineHeader, expired);
        var (timedOut, timedOutBody) = await server.SendAsync(apiWithDeadline);
        Assert.Equal(HttpStatusCode.GatewayTimeout, timedOut.StatusCode);
        Assert.Equal("KERNEL_TIMEOUT_001", timedOutBody.GetProperty("code").GetString());

        // 内核应用停止后：/api 返回 503，Hub 与静态文件仍可用（它们不依赖内核状态）
        await server.Kernel.StopAsync(CancellationToken.None);
        var (blocked, blockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", token));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, blocked.StatusCode);
        Assert.Equal("KERNEL_UNAVAILABLE_001", blockedBody.GetProperty("code").GetString());
        Assert.Equal("still here", await connection.InvokeAsync<string>("Echo", "still here"));
        Assert.Equal(HttpStatusCode.OK, (await server.Client.GetAsync("/assets/app.js")).StatusCode);
        await connection.StopAsync();
    }

    private sealed class EchoHub : Hub
    {
        public Task<string> Echo(string message) => Task.FromResult(Context.ConnectionAborted.IsCancellationRequested ? string.Empty : message);

        public Task<string> Who() => Task.FromResult(Context.User?.Identity?.Name ?? string.Empty);
    }
}
