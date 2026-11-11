// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Host.Commands;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Hosting.CommandLine;
using XiHan.Framework.Hosting.CommandLine.Runtime;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证种子的可重复执行与 <c>seed</c> 命令
/// </summary>
/// <remarks>
/// 可重复执行、运营数据只首写；<c>seed</c> 命令经框架的命令行运行器分发，stdout 是 JSON 报告，用法错误退出码 64
/// </remarks>
public sealed class SeedTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Seeders_AreIdempotent_AndTheSuperAdmin_KeepsItsPassword()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:SuperAdminPassword", "Initial@12345")]);
        await server.RunSeedersAsync();
        var first = await AuthFlowTests.LoginAsync(server, "superadmin", "Initial@12345");

        // 本人改密后再跑种子：账号已存在不改密码、授予已存在不重复
        var (changed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"Initial@12345","newPassword":"Changed@12345"}""", first.GetProperty("accessToken").GetString()));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        await server.RunSeedersAsync();
        var (stale, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"superadmin","password":"Initial@12345"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, stale.StatusCode);
        var second = await AuthFlowTests.LoginAsync(server, "superadmin", "Changed@12345");
        Assert.Equal(first.GetProperty("user").GetProperty("id").GetString(), second.GetProperty("user").GetProperty("id").GetString());

        var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"keyword":"superadmin"}""", second.GetProperty("accessToken").GetString()));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(1, listBody.GetProperty("total").GetInt64());
    }

    [Fact]
    public async Task SeedStartup_RunsTheSeeders_OnlyWhenRunOnStartupIsOn()
    {
        // 缺省关：启动不写种子，还没有超级管理员
        await using (var off = await BasicAppTestServer.StartAsync(_database.ConnectionString))
        {
            Assert.True(await SeedStartup.RunAsync(off.App));
            var (login, _) = await off.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"superadmin","password":"SuperAdmin@123"}"""));
            Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        }

        await using var on = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:RunOnStartup", "true")]);
        Assert.True(await SeedStartup.RunAsync(on.App));
        await AuthFlowTests.LoginAsync(on, "superadmin", "SuperAdmin@123");
    }

    [Fact]
    public async Task SeedCommand_ListsRunsAndRejectsUnknownSeeders()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, configure: static builder => builder.Services.AddBasicAppCommands());
        var runner = server.Services.GetRequiredService<CommandLineRunner>();

        var (listed, listedOut, _) = await RunAsync(runner, ["seed", "--list"]);
        Assert.Equal(CommandLineExitCodes.Success, listed);
        var report = JsonElement.Parse(listedOut);
        Assert.Equal("listed", report.GetProperty("status").GetString());
        Assert.Contains(report.GetProperty("seeders").EnumerateArray(), static s => s.GetProperty("name").GetString() == "identity.platform" && s.GetProperty("order").GetInt32() == 100);

        // 只列出不执行：还没有超级管理员
        var (beforeRun, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"superadmin","password":"SuperAdmin@123"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, beforeRun.StatusCode);

        var (unknown, _, unknownErr) = await RunAsync(runner, ["seed", "--only", "identity.platform,nope"]);
        Assert.Equal(CommandLineExitCodes.Usage, unknown);
        Assert.Equal("HOST_VALIDATION_001", JsonElement.Parse(unknownErr).GetProperty("code").GetString());

        var (ran, ranOut, _) = await RunAsync(runner, ["seed", "--only", "identity.platform"]);
        Assert.Equal(CommandLineExitCodes.Success, ran);
        Assert.Equal("completed", JsonElement.Parse(ranOut).GetProperty("status").GetString());
        await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123");

        // 框架的 help 列出本应用的命令，说明按当前消息作用域翻译（目录由命令模式登记）
        // 调用流上没有作用域时取进程默认作用域：测试进程里各宿主并行启动、后启动的覆盖它（框架设计如此），
        // 所以这里在调用流上装本宿主的作用域，说明一定出自本宿主的目录
        using (MessageScope.Use(server.Services.GetRequiredService<IMessageResolver>(), "en-US"))
        {
            var expected = HostMessages.SeedDescription.L();
            Assert.NotEqual(HostMessages.SeedDescription, expected);
            var (help, helpOut, _) = await RunAsync(runner, ["help"]);
            Assert.Equal(CommandLineExitCodes.Success, help);
            Assert.Contains("  seed", helpOut, StringComparison.Ordinal);
            Assert.Contains(expected, helpOut, StringComparison.Ordinal);
        }
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(CommandLineRunner runner, string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = await runner.RunAsync(args, output, error, CancellationToken.None);
        return (exitCode, output.ToString(), error.ToString());
    }
}
