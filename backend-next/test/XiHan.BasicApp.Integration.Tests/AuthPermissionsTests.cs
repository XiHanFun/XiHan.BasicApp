// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证 <c>auth.permissions</c> 的展开、裁剪与权限目录
/// </summary>
/// <remarks>
/// 通配展开、按作用侧裁剪、角色摘要、按权限裁剪的菜单树（菜单种子来自各包页面登记、可重复执行）；<c>permissions.list</c> 的目录
/// </remarks>
public sealed class AuthPermissionsTests : IDisposable
{
    // 只有 identity.users.read 的主体看到的树：登录即可见的页面（工作台、审批中心、导出中心、我的消息、关于）加用户页，空了的目录不下发
    private const string PrunedOutline = "workbench.dashboard, custom.workflow-center[workflow.approvals], custom.system-management[custom.system-permission[identity.users], custom.file-services[files.export-center], custom.message-center[notification.inbox]], custom.about[about.project, custom.about-docs, custom.about-github, custom.about-gitee]";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<BasicAppTestServer> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        return server;
    }

    [Fact]
    public async Task SuperAdmin_SeesTheWholeCatalog_AndEveryMenu()
    {
        await using var server = await StartAsync();
        // 种子可重复执行：菜单对齐后不重复
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: admin));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("isPlatform").GetBoolean());
        var permissions = body.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()).ToList();
        Assert.Contains("identity.users.create", permissions);
        Assert.Contains("access.roles.grant", permissions);
        Assert.DoesNotContain("*", permissions);
        Assert.Equal(permissions.Order(StringComparer.Ordinal), permissions);
        Assert.Equal(["admin"], body.GetProperty("roles").EnumerateArray().Select(static r => r.GetProperty("code").GetString()).ToList());

        var menus = body.GetProperty("menus");
        // 工作台（Tier 3 模块，排序 0、登录即可见）是第一页：登录后的落点；其余页面按登记挂在内置目录下，平台侧看不到租户侧的部门、岗位与我的订阅
        Assert.Equal(
            "workbench.dashboard, "
            + "custom.ai-center[ai.providers, ai.assistants, ai.prompts, ai.knowledge-documents], "
            + "custom.workflow-center[workflow.approvals, workflow.definitions, workflow.instances], "
            + "custom.integration-center[notification.email-configs, notification.sms-configs, custom.push-service[notification.bot-configs, notification.telegram-bots], custom.open-api[openplatform.oauth-apps, auditing.open-api-logs]], "
            + "custom.system-management["
            + "custom.system-permission[identity.users, access.roles, access.permissions, access.delegations, access.policies, access.constraint-rules, access.field-security, access.menus], "
            + "custom.tenant-management[tenancy.tenants, tenancy.editions], "
            + "custom.system-settings[settings.configs, settings.dicts, settings.numbering-rules], "
            + "custom.system-services[maintenance.jobs, maintenance.caches, maintenance.versions, maintenance.migrations], "
            + "custom.system-monitor[maintenance.server, identity.online-users], "
            + "custom.system-logs[auditing.login-logs, auditing.access-logs, auditing.operation-logs, auditing.exception-logs, auditing.diff-logs, auditing.permission-change-logs, auditing.condition-hits, auditing.traces], "
            + "custom.file-services[files.library, files.storage-configs, files.export-center], "
            + "custom.online-dev[codegeneration.code-gen, printing.print-templates], "
            + "custom.message-center[notification.inbox, notification.notifications, notification.templates, notification.messages, chat.conversations, chat.audit]], "
            + "custom.about[about.project, custom.about-docs, custom.about-github, custom.about-gitee]",
            MenuTrees.Outline(menus));
        var dashboard = menus[0];
        Assert.Equal("Page", dashboard.GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, dashboard.GetProperty("permission").ValueKind);
        Assert.Equal("identity/users/index", MenuTrees.Find(menus, "identity.users").GetProperty("component").GetString());
        var roles = MenuTrees.Find(menus, "access.roles");
        Assert.Equal("/access/roles", roles.GetProperty("route").GetString());
        Assert.Equal("access.roles.read", roles.GetProperty("permission").GetString());
        Assert.Equal("Directory", MenuTrees.Find(menus, "custom.system-permission").GetProperty("kind").GetString());
        // 页面码按树深度优先
        Assert.Equal(["workbench.dashboard", "ai.providers", "ai.assistants", "ai.prompts", "ai.knowledge-documents", "workflow.approvals", "workflow.definitions", "workflow.instances", "notification.email-configs", "notification.sms-configs", "notification.bot-configs", "notification.telegram-bots", "openplatform.oauth-apps", "auditing.open-api-logs", "identity.users", "access.roles", "access.permissions", "access.delegations", "access.policies", "access.constraint-rules", "access.field-security", "access.menus", "tenancy.tenants", "tenancy.editions", "settings.configs", "settings.dicts", "settings.numbering-rules", "maintenance.jobs", "maintenance.caches", "maintenance.versions", "maintenance.migrations", "maintenance.server", "identity.online-users", "auditing.login-logs", "auditing.access-logs", "auditing.operation-logs", "auditing.exception-logs", "auditing.diff-logs", "auditing.permission-change-logs", "auditing.condition-hits", "auditing.traces", "files.library", "files.storage-configs", "files.export-center", "codegeneration.code-gen", "printing.print-templates", "notification.inbox", "notification.notifications", "notification.templates", "notification.messages", "chat.conversations", "chat.audit", "about.project"], body.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()).ToList());

        var (catalog, catalogBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/permissions", token: admin));
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        var roleRead = catalogBody.GetProperty("items").EnumerateArray().Single(static i => i.GetProperty("code").GetString() == "access.roles.read");
        Assert.Equal("access", roleRead.GetProperty("package").GetString());
        Assert.Equal("roles", roleRead.GetProperty("resource").GetString());
        Assert.Equal("Both", roleRead.GetProperty("scope").GetString());

        // 显示名与说明按调用方的区域取（各包语言文件里的 permissions.<码>.name / .description），不是描述表里的英文
        foreach (var (locale, name, description) in new[] { ("zh-CN", "读取存储配置", "查看当前一侧的存储配置；访问密钥不回显。"), ("en-US", "Read storage configurations", "View the caller's side's storage configurations; access keys are never shown.") })
        {
            var (_, localized) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/permissions", token: admin, acceptLanguage: locale));
            var storageRead = localized.GetProperty("items").EnumerateArray().Single(static i => i.GetProperty("code").GetString() == "files.storage-configs.read");
            Assert.Equal((name, description), (storageRead.GetProperty("displayName").GetString(), storageRead.GetProperty("description").GetString()));
        }
    }

    [Fact]
    public async Task RoleMember_SeesOnlyGrantedPermissions_AndPrunedMenus()
    {
        await using var server = await StartAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (role, roleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"viewer","name":"Viewer"}""", admin));
        Assert.Equal(HttpStatusCode.Created, role.StatusCode);
        var roleId = roleBody.GetProperty("id").GetString()!;
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read"]}""", admin));
        var (bob, bobBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"bob","displayName":"Bob","password":"Bob@123456"}""", admin));
        Assert.Equal(HttpStatusCode.Created, bob.StatusCode);
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{bobBody.GetProperty("id").GetString()}}"]}""", admin));

        var token = (await AuthFlowTests.LoginAsync(server, "bob", "Bob@123456")).GetProperty("accessToken").GetString()!;
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["identity.users.read"], body.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()).ToList());
        Assert.Equal(["viewer"], body.GetProperty("roles").EnumerateArray().Select(static r => r.GetProperty("code").GetString()).ToList());
        // 裁剪后空了的目录不下发：只剩装着可见页面的那几个
        Assert.Equal(PrunedOutline, MenuTrees.Outline(body.GetProperty("menus")));
        Assert.Equal(["workbench.dashboard", "workflow.approvals", "identity.users", "files.export-center", "notification.inbox", "about.project"], body.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()).ToList());

        // 没有 access.permissions.read 的主体看不到权限目录
        var (catalog, catalogBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/permissions", token: token));
        Assert.Equal(HttpStatusCode.Forbidden, catalog.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", catalogBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ExternalToken_IsResolvedByItsRoleClaims_WithinItsTenant()
    {
        await using var server = await StartAsync();
        // 测试令牌的 role 声明是字符串角色名，与种子的全局授予（identity.users.read / create）对接
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body.GetProperty("isPlatform").GetBoolean());
        Assert.Equal(["identity.users.create", "identity.users.read"], body.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()).ToList());
        Assert.Equal(0, body.GetProperty("roles").GetArrayLength());
        Assert.Equal(PrunedOutline, MenuTrees.Outline(body.GetProperty("menus")));

        var (anonymous, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions"));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }
}
