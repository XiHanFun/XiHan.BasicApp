// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Plugins.Infrastructure;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Plugins.OutOfProcess;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证已加载插件的运行期贡献接进权限目录、套餐、审计与插件主体
/// </summary>
/// <remarks>
/// 插件的权限码在加载期间进权限目录（按作用侧下发、能授给角色）与套餐白名单，带着审计标记，撤回即失效；
/// 插件主体只持有批准的回调权限码，在租户里缺省什么都看不到，批准了「全部」才不受数据范围限制
/// </remarks>
public sealed class PluginRuntimeCatalogTests : IDisposable
{
    private const string ManifestId = "xihan.contoso.weather";
    private const string Read = "weather.weather.read";
    private const string Manage = "weather.forecasts.manage";

    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static LoadedPlugin Weather(IReadOnlyList<string>? callbacks = null, bool unrestricted = false) => new(
        ManifestId,
        "weather",
        "1.0.0",
        [new PermissionDescriptor(Read, Read, PermissionScope.Tenant, audited: true), new PermissionDescriptor(Manage, Manage, PermissionScope.Platform)],
        [],
        callbacks ?? [],
        unrestricted);

    private static async Task<string> RoleAsync(BasicAppTestServer server, string token, string code)
    {
        var (created, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", JsonSerializer.Serialize(new { code, name = code }), token));
        Assert.True(created.StatusCode == HttpStatusCode.Created, body.GetRawText());
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> GrantAsync(BasicAppTestServer server, string token, string roleId, string permission)
    {
        var (granted, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", JsonSerializer.Serialize(new { permissions = new[] { permission } }), token));
        Assert.True(granted.StatusCode == HttpStatusCode.OK, body.GetRawText());
        return body;
    }

    private static async Task<string> PermissionsAsync(BasicAppTestServer server, string token, string? side = null)
    {
        var (listed, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, side is null ? "/api/permissions" : $"/api/permissions?side={side}", token: token));
        Assert.True(listed.StatusCode == HttpStatusCode.OK, body.GetRawText());
        return body.GetRawText();
    }

    /// <summary>
    /// 以插件主体经完整管道调用一个宿主操作
    /// </summary>
    /// <remarks>
    /// 与框架插件宿主的回调同一身份：主体为清单 ID、方案 Plugin、租户在 Baggage
    /// </remarks>
    private static async Task<OperationResponse> CallbackAsync(BasicAppTestServer server, string? tenant, string operationId, string payload)
    {
        await using var scope = server.Services.CreateAsyncScope();
        var baggage = tenant is null ? null : new Dictionary<string, string> { [BaggageKeys.Tenant] = tenant };
        var context = App.CreateContext(scope.ServiceProvider, new InvocationContextOptions { Baggage = baggage }, CancellationToken.None);
        context.SetSecurity(new SecurityContext(new Principal(ManifestId, PluginPrincipal.Scheme), null));
        return await scope.ServiceProvider.GetRequiredService<IOperationInvoker>().InvokeAsync(context, new OperationRequest(operationId, Encoding.UTF8.GetBytes(payload)));
    }

    [Fact]
    public void PluginScheme_MatchesTheFrameworkPluginHost() => Assert.Equal(OutOfProcessPluginHost.PluginScheme, PluginPrincipal.Scheme);

    [Fact]
    public async Task PluginPermissions_JoinTheCatalogAndEditions_UntilWithdrawn()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var _ = server;
        var catalog = server.Services.GetRequiredService<PluginRuntimeCatalog>();
        var enterprise = await EditionsTests.EditionIdAsync(server, platform, "enterprise");
        var role = await RoleAsync(server, tenant, "forecaster");

        // 没加载：目录里没有，角色授不了，套餐放不进
        Assert.Equal(AccessErrors.PermissionUnknown, (await GrantAsync(server, tenant, role, Read)).GetProperty("failed")[0].GetProperty("code").GetString());
        var (_, detail) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/editions/{enterprise}", token: platform));
        string[] whitelist = [.. detail.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()!), Read];
        var (rejected, rejectedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/editions/{enterprise}:set-permissions", JsonSerializer.Serialize(new { permissions = whitelist }), platform));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal("TENANCY_VALIDATION_002", rejectedBody.GetProperty("code").GetString());

        catalog.Put(Weather());

        // 加载期间：按作用侧进目录，带审计标记，能授给角色
        var registered = server.Services.GetRequiredService<RegisteredPermissions>();
        Assert.Equal((PermissionScope.Tenant, true), (registered.Find(Read)!.Scope, registered.Find(Read)!.Audited));
        Assert.DoesNotContain(registered.BuiltIn, static p => p.Code == Read);
        var tenantSide = await PermissionsAsync(server, platform, "tenant");
        Assert.Contains($"\"{Read}\"", tenantSide, StringComparison.Ordinal);
        Assert.DoesNotContain($"\"{Manage}\"", tenantSide, StringComparison.Ordinal);
        var platformSide = await PermissionsAsync(server, platform, "platform");
        Assert.Contains($"\"{Manage}\"", platformSide, StringComparison.Ordinal);
        Assert.DoesNotContain($"\"{Read}\"", platformSide, StringComparison.Ordinal);
        Assert.Equal(Read, (await GrantAsync(server, tenant, role, Read)).GetProperty("succeeded")[0].GetString());

        // 租户要等套餐加进插件码才用得上
        Assert.DoesNotContain($"\"{Read}\"", await PermissionsAsync(server, tenant), StringComparison.Ordinal);
        var (set, setBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/editions/{enterprise}:set-permissions", JsonSerializer.Serialize(new { permissions = whitelist }), platform));
        Assert.True(set.StatusCode == HttpStatusCode.OK, setBody.GetRawText());
        Assert.Contains($"\"{Read}\"", await PermissionsAsync(server, tenant), StringComparison.Ordinal);

        // 撤回：目录里又没有了，新的授予照旧被拒
        Assert.True(catalog.Remove(ManifestId));
        Assert.Null(registered.Find(Read));
        Assert.DoesNotContain($"\"{Read}\"", await PermissionsAsync(server, tenant), StringComparison.Ordinal);
        var another = await RoleAsync(server, tenant, "observer");
        Assert.Equal(AccessErrors.PermissionUnknown, (await GrantAsync(server, tenant, another, Read)).GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.False(catalog.Remove(ManifestId));
    }

    [Fact]
    public async Task PluginPrincipal_HoldsOnlyApprovedCallbacks_AndSeesNoTenantDataByDefault()
    {
        var (server, _, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var _ = server;
        var catalog = server.Services.GetRequiredService<PluginRuntimeCatalog>();
        const string ListUsers = """{"page":{"index":1,"size":20}}""";

        // 没加载：插件主体什么都不持有
        var unloaded = await CallbackAsync(server, "contoso", "users.list", ListUsers);
        Assert.Equal("SECURITY_DENIED_004", unloaded.Outcome.Error?.Code);

        // 批准了读用户：调得通，但缺省数据范围为无，一行也看不到；没批准的码照旧拒绝
        catalog.Put(Weather(callbacks: ["identity.users.read"]));
        var scoped = await CallbackAsync(server, "contoso", "users.list", ListUsers);
        Assert.True(scoped.IsSuccess, scoped.Outcome.Error?.Code);
        Assert.Equal(0, JsonDocument.Parse(scoped.Payload).RootElement.GetProperty("items").GetArrayLength());
        var create = await CallbackAsync(server, "contoso", "users.create", """{"userName":"bot","displayName":"Bot","password":"Bot@123456"}""");
        Assert.Equal("SECURITY_DENIED_004", create.Outcome.Error?.Code);

        // 批准了 data-scope:all：看得到本租户的用户
        catalog.Put(Weather(callbacks: ["identity.users.read"], unrestricted: true));
        var unrestricted = await CallbackAsync(server, "contoso", "users.list", ListUsers);
        Assert.True(unrestricted.IsSuccess, unrestricted.Outcome.Error?.Code);
        Assert.Contains(JsonDocument.Parse(unrestricted.Payload).RootElement.GetProperty("items").EnumerateArray(), static u => u.GetProperty("userName").GetString() == "admin");

        // 卸载后：授予随之失效
        catalog.Remove(ManifestId);
        Assert.Equal("SECURITY_DENIED_004", (await CallbackAsync(server, "contoso", "users.list", ListUsers)).Outcome.Error?.Code);
    }
}
