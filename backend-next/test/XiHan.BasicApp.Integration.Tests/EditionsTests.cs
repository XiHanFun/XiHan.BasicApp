// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证套餐门控对有效权限的收窄与套餐管理。
/// </summary>
/// <remarks>
/// 种子四档套餐（免费版缺省）、租户侧权限目录、开通即绑定缺省套餐、有效权限与判定都按白名单收窄（租户管理员的 <c>*</c> 也收窄）、
/// 改白名单与换套餐立即生效、不受限套餐、套餐的增改删与校验、租户主体碰不到套餐管理。
/// </remarks>
public sealed class EditionsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    /// <summary>
    /// 按编码取套餐主键（种子写入的套餐用）。
    /// </summary>
    public static async Task<string> EditionIdAsync(BasicAppTestServer server, string token, string code)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/editions", token: token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return body.GetProperty("items").EnumerateArray().Single(e => e.GetProperty("code").GetString() == code).GetProperty("id").GetString()!;
    }

    private static List<string?> Codes(JsonElement body, string property) => body.GetProperty(property).EnumerateArray().Select(static p => p.ValueKind == JsonValueKind.String ? p.GetString() : p.GetProperty("code").GetString()).ToList();

    [Fact]
    public async Task Editions_GateTenantPermissions_AndFollowChangesImmediately()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        // 租户侧目录（平台主体可看）：没有平台专属权限
        var (catalog, catalogBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/permissions?side=tenant", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        var tenantSide = Codes(catalogBody, "items");
        Assert.Contains("identity.users.create", tenantSide);
        Assert.DoesNotContain("tenancy.tenants.read", tenantSide);
        var reads = tenantSide.Where(static c => c!.EndsWith(".read", StringComparison.Ordinal)).ToList();

        // 种子：四档、免费版缺省；免费版只读，企业版是租户侧全部
        var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/editions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var items = listBody.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(["free", "basic", "pro", "enterprise"], items.Select(static e => e.GetProperty("code").GetString()).ToList());
        Assert.Equal(["free"], items.Where(static e => e.GetProperty("isDefault").GetBoolean()).Select(static e => e.GetProperty("code").GetString()).ToList());
        Assert.Equal(reads.Count, items[0].GetProperty("permissionCount").GetInt32());
        Assert.Equal(tenantSide.Count, items[3].GetProperty("permissionCount").GetInt32());
        var freeId = items[0].GetProperty("id").GetString()!;
        var basicId = items[1].GetProperty("id").GetString()!;
        var enterpriseId = items[3].GetProperty("id").GetString()!;
        var (freeDetail, freeDetailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/editions/{freeId}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, freeDetail.StatusCode);
        Assert.Equal(reads, Codes(freeDetailBody, "permissions"));

        // 开通不指定套餐 → 缺省的免费版；租户管理员持 * 也只剩只读
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", """{"tenantId":"contoso","name":"Contoso"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(freeId, createdBody.GetProperty("editionId").GetString());
        var contosoId = createdBody.GetProperty("id").GetString()!;
        var (initialized, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{contosoId}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        var tenantAdmin = (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "contoso")).GetProperty("accessToken").GetString()!;

        async Task<List<string?>> EffectiveAsync()
        {
            var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: tenantAdmin));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return Codes(body, "permissions");
        }

        Assert.Equal(reads, await EffectiveAsync());
        var (gated, gatedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"bob","displayName":"Bob","password":"Bob@1234567"}""", tenantAdmin));
        Assert.Equal(HttpStatusCode.Forbidden, gated.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", gatedBody.GetProperty("code").GetString());
        var (ownCatalog, ownCatalogBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/permissions", token: tenantAdmin));
        Assert.Equal(HttpStatusCode.OK, ownCatalog.StatusCode);
        Assert.Equal(reads, Codes(ownCatalogBody, "items"));
        var (platformCatalog, platformCatalogBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/permissions?side=platform", token: tenantAdmin));
        Assert.Equal(HttpStatusCode.Forbidden, platformCatalog.StatusCode);
        Assert.Equal("ACCESS_DENIED_001", platformCatalogBody.GetProperty("code").GetString());

        // 租户主体碰不到套餐管理（平台侧权限，白名单里也不会有）
        var (tenantSideEditions, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/editions", token: tenantAdmin));
        Assert.Equal(HttpStatusCode.Forbidden, tenantSideEditions.StatusCode);

        // 改白名单立即生效：免费版加上建用户
        var widened = reads.Append("identity.users.create").ToList();
        var (set, setBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/editions/{freeId}:set-permissions", JsonSerializer.Serialize(new { permissions = widened }), superadmin));
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        Assert.Equal(widened.Count, setBody.GetProperty("edition").GetProperty("permissionCount").GetInt32());
        Assert.Equal(1, setBody.GetProperty("edition").GetProperty("tenantCount").GetInt32());
        Assert.Contains("identity.users.create", await EffectiveAsync());
        var (allowed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"bob","displayName":"Bob","password":"Bob@1234567"}""", tenantAdmin));
        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);

        // 白名单只能放租户能生效的权限：平台专属与目录外的码都拒绝
        foreach (var bad in new[] { "tenancy.tenants.read", "nope.things.read" })
        {
            var (rejected, rejectedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/editions/{freeId}:set-permissions", JsonSerializer.Serialize(new { permissions = new[] { bad } }), superadmin));
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal("TENANCY_VALIDATION_002", rejectedBody.GetProperty("code").GetString());
        }

        // 换套餐立即生效：企业版有授权管理；换成不受限即租户侧全部；不存在的套餐 404
        var (toEnterprise, toEnterpriseBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{contosoId}:change-edition", $$"""{"editionId":"{{enterpriseId}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, toEnterprise.StatusCode);
        Assert.Equal(enterpriseId, toEnterpriseBody.GetProperty("editionId").GetString());
        Assert.Contains("access.roles.grant", await EffectiveAsync());
        var (toBasic, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{contosoId}:change-edition", $$"""{"editionId":"{{basicId}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, toBasic.StatusCode);
        var basic = await EffectiveAsync();
        Assert.Contains("organization.departments.create", basic);
        Assert.DoesNotContain("access.roles.grant", basic);
        Assert.DoesNotContain("identity.impersonation.start", basic);
        var (unrestricted, unrestrictedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{contosoId}:change-edition", "{}", superadmin));
        Assert.Equal(HttpStatusCode.OK, unrestricted.StatusCode);
        Assert.Equal(JsonValueKind.Null, unrestrictedBody.GetProperty("editionId").ValueKind);
        Assert.Equal(tenantSide, await EffectiveAsync());
        var (missing, missingBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{contosoId}:change-edition", $$"""{"editionId":"{{Guid.NewGuid()}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("TENANCY_NOTFOUND_004", missingBody.GetProperty("code").GetString());

        // 新建：编码唯一、文法校验；设为缺省即接管开通的缺省
        var (duplicate, duplicateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/editions", """{"code":"free","name":"Again"}""", superadmin));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("TENANCY_CONFLICT_006", duplicateBody.GetProperty("code").GetString());
        var (invalid, invalidBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/editions", """{"code":"Bad Code","name":"Bad"}""", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.True(invalidBody.GetProperty("errors").TryGetProperty("code", out _));
        var (custom, customBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/editions", """{"code":"custom","name":"Custom","isDefault":true,"order":50,"permissions":["identity.users.read","identity.users.read"]}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, custom.StatusCode);
        Assert.Equal(["identity.users.read"], Codes(customBody, "permissions"));
        var customId = customBody.GetProperty("edition").GetProperty("id").GetString()!;
        var (afterCreate, afterCreateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/editions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, afterCreate.StatusCode);
        Assert.Equal(["custom"], afterCreateBody.GetProperty("items").EnumerateArray().Where(static e => e.GetProperty("isDefault").GetBoolean()).Select(static e => e.GetProperty("code").GetString()).ToList());
        var (globex, globexBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", """{"tenantId":"initech","name":"Initech"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, globex.StatusCode);
        Assert.Equal(customId, globexBody.GetProperty("editionId").GetString());

        // 取消缺省后没有缺省套餐：开通必须指定
        var (updated, updatedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/editions/{customId}", """{"name":"Custom Plan","isDefault":false,"order":50}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Custom Plan", updatedBody.GetProperty("edition").GetProperty("name").GetString());
        var (noDefault, noDefaultBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", """{"tenantId":"hooli","name":"Hooli"}""", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, noDefault.StatusCode);
        Assert.Equal("TENANCY_VALIDATION_003", noDefaultBody.GetProperty("code").GetString());

        // 删除：还有租户在用的逐项拒绝，没人用的删掉，不存在的 404
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/editions?ids={customId}&ids={freeId}&ids={Guid.NewGuid()}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal([freeId], deletedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal(["TENANCY_CONFLICT_007", "TENANCY_NOTFOUND_004"], deletedBody.GetProperty("failed").EnumerateArray().Select(static f => f.GetProperty("code").GetString()).ToList());
        var (gone, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/editions/{freeId}", token: superadmin));
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }
}
