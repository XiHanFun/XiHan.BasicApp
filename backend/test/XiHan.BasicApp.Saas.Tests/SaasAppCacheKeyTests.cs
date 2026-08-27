// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 业务缓存键构造器测试。
/// </summary>
/// <remarks>
/// <see cref="SaasCacheKeys"/> 是纯函数集合，但它决定了**缓存会不会串味**：
/// 键少一个维度（租户/场景/渠道），不同主体就会读到彼此的数据；
/// 而失效用的匹配模式若与写入用的键对不上，失效就静默失灵。
/// 本类逐条锁定"键的维度组成"与"键 ↔ 模式必须匹配得上"两件事。
/// </remarks>
public sealed class SaasAppCacheKeyTests
{
    /// <summary>
    /// 配置值缓存键：正租户号入键，null/0/负数一律归为 platform 段。
    /// </summary>
    /// <param name="tenantId">租户标识。</param>
    /// <param name="expectedSegment">期望的租户段文本。</param>
    [Theory]
    [InlineData(null, "platform")]
    [InlineData(0L, "platform")]
    [InlineData(-1L, "platform")]
    [InlineData(7L, "7")]
    public void ConfigValue_TenantSegment_ShouldFoldNonPositiveTenantToPlatform(long? tenantId, string expectedSegment)
    {
        var key = SaasCacheKeys.ConfigValue(tenantId, "saas.demo");

        Assert.Equal($"tenant:{expectedSegment}:key:saas.demo", key, StringComparer.Ordinal);
    }

    /// <summary>
    /// 配置值缓存键必须走配置键规范化（大小写与首尾空白不构成两份缓存）。
    /// </summary>
    [Fact]
    public void ConfigValue_ShouldNormalizeConfigKey()
    {
        var upper = SaasCacheKeys.ConfigValue(1, "  SAAS.Demo  ");
        var lower = SaasCacheKeys.ConfigValue(1, "saas.demo");

        Assert.Equal(lower, upper, StringComparer.Ordinal);
    }

    /// <summary>
    /// 配置键非法时构造缓存键必须直接抛错，而不是落一个脏键。
    /// </summary>
    [Fact]
    public void ConfigValue_BlankConfigKey_ShouldThrow()
    {
        Assert.ThrowsAny<ArgumentException>(() => SaasCacheKeys.ConfigValue(1, null!));
        Assert.ThrowsAny<ArgumentException>(() => SaasCacheKeys.ConfigValue(1, "   "));
    }

    /// <summary>
    /// 单键失效模式必须能匹配到该键在任意租户下写入的缓存键。
    /// </summary>
    [Fact]
    public void ConfigValuePattern_ShouldMatchEveryTenantVariantOfThatKey()
    {
        var pattern = SaasCacheKeys.ConfigValuePattern("saas.demo");

        Assert.Equal("tenant:*:key:saas.demo", pattern, StringComparer.Ordinal);
        Assert.True(GlobMatches(pattern, SaasCacheKeys.ConfigValue(null, "saas.demo")));
        Assert.True(GlobMatches(pattern, SaasCacheKeys.ConfigValue(42, "saas.demo")));
        Assert.False(GlobMatches(pattern, SaasCacheKeys.ConfigValue(42, "saas.other")));
    }

    /// <summary>
    /// 全量配置失效模式必须能匹配任意租户任意键。
    /// </summary>
    [Fact]
    public void AllConfigValuesPattern_ShouldMatchAnyTenantAndKey()
    {
        var pattern = SaasCacheKeys.AllConfigValuesPattern();

        Assert.True(GlobMatches(pattern, SaasCacheKeys.ConfigValue(null, "saas.a")));
        Assert.True(GlobMatches(pattern, SaasCacheKeys.ConfigValue(9, "saas.b")));
    }

    /// <summary>
    /// 授权快照键必须同时含用户与租户两个维度：同一用户切换租户不得复用同一份快照。
    /// </summary>
    [Fact]
    public void AuthorizationSnapshot_ShouldIsolateSameUserAcrossTenants()
    {
        var inTenantOne = SaasCacheKeys.AuthorizationSnapshot(1, 100);
        var inTenantTwo = SaasCacheKeys.AuthorizationSnapshot(2, 100);
        var onPlatform = SaasCacheKeys.AuthorizationSnapshot(null, 100);

        Assert.Equal("user:100:tenant:1", inTenantOne, StringComparer.Ordinal);
        Assert.Equal("user:100:tenant:2", inTenantTwo, StringComparer.Ordinal);
        Assert.Equal("user:100:tenant:platform", onPlatform, StringComparer.Ordinal);
        Assert.Equal(3, new[] { inTenantOne, inTenantTwo, onPlatform }.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// 租户上下文为 0 与为 null 等价，都表示平台态。
    /// </summary>
    [Fact]
    public void AuthorizationSnapshot_ZeroTenant_ShouldEqualPlatformContext()
    {
        Assert.Equal(
            SaasCacheKeys.AuthorizationSnapshot(null, 5),
            SaasCacheKeys.AuthorizationSnapshot(0, 5),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 授权变更后按用户整体失效：模式必须覆盖该用户全部租户上下文，且不误伤其它用户。
    /// </summary>
    [Fact]
    public void AuthorizationSnapshotPattern_ShouldCoverAllTenantsOfThatUserOnly()
    {
        var pattern = SaasCacheKeys.AuthorizationSnapshotPattern(100);

        Assert.True(GlobMatches(pattern, SaasCacheKeys.AuthorizationSnapshot(1, 100)));
        Assert.True(GlobMatches(pattern, SaasCacheKeys.AuthorizationSnapshot(null, 100)));
        Assert.False(GlobMatches(pattern, SaasCacheKeys.AuthorizationSnapshot(1, 101)));
    }

    /// <summary>
    /// 用户设置键必须按 用户 × 场景 × 设置键 三维隔离，场景以枚举数值入键。
    /// </summary>
    [Fact]
    public void UserSetting_ShouldIsolateByUserSceneAndKey()
    {
        var preference = SaasCacheKeys.UserSetting(3, UserSettingScene.Preference, "theme");
        var page = SaasCacheKeys.UserSetting(3, UserSettingScene.Page, "theme");

        Assert.Equal("user:3:scene:0:key:theme", preference, StringComparer.Ordinal);
        Assert.Equal("user:3:scene:1:key:theme", page, StringComparer.Ordinal);
    }

    /// <summary>
    /// 用户设置写后按用户整体失效：模式必须命中该用户全部场景，但不得命中别的用户。
    /// </summary>
    [Fact]
    public void UserSettingPattern_ShouldCoverAllScenesOfThatUserOnly()
    {
        var pattern = SaasCacheKeys.UserSettingPattern(3);

        Assert.True(GlobMatches(pattern, SaasCacheKeys.UserSetting(3, UserSettingScene.Preference, "theme")));
        Assert.True(GlobMatches(pattern, SaasCacheKeys.UserSetting(3, UserSettingScene.Page, "identity.user")));
        Assert.False(GlobMatches(pattern, SaasCacheKeys.UserSetting(4, UserSettingScene.Page, "identity.user")));
    }

    /// <summary>
    /// 菜单路由键按权限集合内容取值，与传入顺序和重复项无关。
    /// </summary>
    [Fact]
    public void MenuRoutes_ShouldBeOrderAndDuplicateInsensitive()
    {
        var ordered = SaasCacheKeys.MenuRoutes([1, 2, 3], hasAllPermissions: false);
        var shuffled = SaasCacheKeys.MenuRoutes([3, 1, 2, 1], hasAllPermissions: false);

        Assert.Equal(ordered, shuffled, StringComparer.Ordinal);
    }

    /// <summary>
    /// 拥有全部权限时忽略权限集合，共用同一把超级用户键。
    /// </summary>
    [Fact]
    public void MenuRoutes_HasAllPermissions_ShouldIgnorePermissionIds()
    {
        var superWithIds = SaasCacheKeys.MenuRoutes([1, 2], hasAllPermissions: true);
        var superWithoutIds = SaasCacheKeys.MenuRoutes([], hasAllPermissions: true);
        var normal = SaasCacheKeys.MenuRoutes([1, 2], hasAllPermissions: false);

        Assert.Equal(superWithIds, superWithoutIds, StringComparer.Ordinal);
        Assert.NotEqual(superWithIds, normal, StringComparer.Ordinal);
    }

    /// <summary>
    /// 空权限集合与拥有全部权限必须是两把不同的键（否则无权用户会读到超级用户菜单）。
    /// </summary>
    [Fact]
    public void MenuRoutes_EmptySet_ShouldNotCollideWithAllPermissions()
    {
        Assert.NotEqual(
            SaasCacheKeys.MenuRoutes([], hasAllPermissions: false),
            SaasCacheKeys.MenuRoutes([], hasAllPermissions: true),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 哈希型键统一为「前缀 + 24 位小写十六进制」，且对同一输入稳定（跨进程可复现）。
    /// </summary>
    [Fact]
    public void HashedKeys_ShouldUse24LowerHexCharsAndBeStable()
    {
        var key = SaasCacheKeys.MenuRoutes([1, 2, 3], hasAllPermissions: false);
        var again = SaasCacheKeys.MenuRoutes([1, 2, 3], hasAllPermissions: false);

        Assert.Equal(key, again, StringComparer.Ordinal);

        var hash = key["permission-set:".Length..];
        Assert.Equal(24, hash.Length);
        Assert.All(hash, ch => Assert.True(
            (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f'),
            $"哈希段出现非小写十六进制字符：{ch}（完整键：{key}）"));
    }

    /// <summary>
    /// 选择项类缓存键按各自的过滤维度取值，任一维度变化都必须换键。
    /// </summary>
    [Fact]
    public void SelectKeys_ShouldVaryWithEveryFilterDimension()
    {
        var permissionKeys = new[]
        {
            SaasCacheKeys.PermissionSelect(null, null, 50),
            SaasCacheKeys.PermissionSelect("saas", null, 50),
            SaasCacheKeys.PermissionSelect(null, 1, 50),
            SaasCacheKeys.PermissionSelect(null, null, 100)
        };
        Assert.Equal(4, permissionKeys.Distinct(StringComparer.Ordinal).Count());

        var roleKeys = new[]
        {
            SaasCacheKeys.RoleSelect(null, null, 50),
            SaasCacheKeys.RoleSelect(1, null, 50),
            SaasCacheKeys.RoleSelect(null, true, 50),
            SaasCacheKeys.RoleSelect(null, null, 100)
        };
        Assert.Equal(4, roleKeys.Distinct(StringComparer.Ordinal).Count());

        var resourceKeys = new[]
        {
            SaasCacheKeys.ResourceSelect(null, 50),
            SaasCacheKeys.ResourceSelect(2, 50),
            SaasCacheKeys.ResourceSelect(null, 100)
        };
        Assert.Equal(3, resourceKeys.Distinct(StringComparer.Ordinal).Count());

        var operationKeys = new[]
        {
            SaasCacheKeys.OperationSelect(null, null, 50),
            SaasCacheKeys.OperationSelect(3, null, 50),
            SaasCacheKeys.OperationSelect(null, 4, 50),
            SaasCacheKeys.OperationSelect(null, null, 100)
        };
        Assert.Equal(4, operationKeys.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// 模块编码的首尾空白不改变权限选择项缓存键，空白串等同"不限模块"。
    /// </summary>
    [Fact]
    public void PermissionSelect_ModuleCode_ShouldTrimAndTreatBlankAsAll()
    {
        Assert.Equal(
            SaasCacheKeys.PermissionSelect("saas", null, 20),
            SaasCacheKeys.PermissionSelect("  saas  ", null, 20),
            StringComparer.Ordinal);

        Assert.Equal(
            SaasCacheKeys.PermissionSelect(null, null, 20),
            SaasCacheKeys.PermissionSelect("   ", null, 20),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 选择项键各自带独立前缀，跨类别不会撞键。
    /// </summary>
    [Fact]
    public void SelectKeys_ShouldCarryDistinctPrefixes()
    {
        Assert.StartsWith("permission-select:", SaasCacheKeys.PermissionSelect(null, null, 1), StringComparison.Ordinal);
        Assert.StartsWith("role-select:", SaasCacheKeys.RoleSelect(null, null, 1), StringComparison.Ordinal);
        Assert.StartsWith("resource-select:", SaasCacheKeys.ResourceSelect(null, 1), StringComparison.Ordinal);
        Assert.StartsWith("operation-select:", SaasCacheKeys.OperationSelect(null, null, 1), StringComparison.Ordinal);
    }

    /// <summary>
    /// 平台级单键缓存（版本列表、权限目录）全平台共享固定键。
    /// </summary>
    [Fact]
    public void PlatformSingletonKeys_ShouldBeConstant()
    {
        Assert.Equal("editions:enabled", SaasCacheKeys.EnabledTenantEditions(), StringComparer.Ordinal);
        Assert.Equal("permission-catalog", SaasCacheKeys.PermissionCatalog(), StringComparer.Ordinal);
    }

    /// <summary>
    /// 组织类选择项与部门树按租户隔离，平台态统一记为 platform。
    /// </summary>
    [Fact]
    public void OrganizationKeys_ShouldBeTenantScoped()
    {
        Assert.Equal("tenant:8:dept-select", SaasCacheKeys.DepartmentSelect(8), StringComparer.Ordinal);
        Assert.Equal("tenant:platform:dept-select", SaasCacheKeys.DepartmentSelect(0), StringComparer.Ordinal);
        Assert.Equal("tenant:8:position-select", SaasCacheKeys.PositionSelect(8), StringComparer.Ordinal);
        Assert.Equal("tenant:platform:position-select", SaasCacheKeys.PositionSelect(null), StringComparer.Ordinal);

        Assert.StartsWith("tenant:8:dept-tree:", SaasCacheKeys.DepartmentTree(8, true, 100), StringComparison.Ordinal);
        Assert.StartsWith("tenant:platform:dept-tree:", SaasCacheKeys.DepartmentTree(null, true, 100), StringComparison.Ordinal);
    }

    /// <summary>
    /// 部门树键的过滤条件（仅启用 / 上限）任一变化都必须换键。
    /// </summary>
    [Fact]
    public void DepartmentTree_ShouldVaryWithFilters()
    {
        var keys = new[]
        {
            SaasCacheKeys.DepartmentTree(1, true, 100),
            SaasCacheKeys.DepartmentTree(1, false, 100),
            SaasCacheKeys.DepartmentTree(1, true, 200),
            SaasCacheKeys.DepartmentTree(2, true, 100)
        };

        Assert.Equal(4, keys.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// 消息模板键按 租户 × 渠道 × 编码 隔离，且全量失效模式必须能命中它。
    /// </summary>
    [Fact]
    public void MessageTemplate_KeyAndAllPattern_ShouldMatch()
    {
        var tenantKey = SaasCacheKeys.MessageTemplate(6, 2, "welcome");
        var platformKey = SaasCacheKeys.MessageTemplate(null, 2, "welcome");

        Assert.Equal("tenant:6:channel:2:code:welcome", tenantKey, StringComparer.Ordinal);
        Assert.Equal("tenant:platform:channel:2:code:welcome", platformKey, StringComparer.Ordinal);

        var pattern = SaasCacheKeys.AllMessageTemplatesPattern();
        Assert.True(GlobMatches(pattern, tenantKey));
        Assert.True(GlobMatches(pattern, platformKey));
    }

    /// <summary>
    /// 版本门控键与其全量失效模式必须匹配得上。
    /// </summary>
    [Fact]
    public void EditionGate_KeyAndAllPattern_ShouldMatch()
    {
        var key = SaasCacheKeys.EditionGate(12);

        Assert.Equal("tenant:12", key, StringComparer.Ordinal);
        Assert.True(GlobMatches(SaasCacheKeys.AllEditionGatesPattern(), key));
    }

    /// <summary>
    /// 字典项树键按 租户 × 字典 × 过滤条件 隔离，全量失效模式必须命中。
    /// </summary>
    [Fact]
    public void DictItemTree_KeyAndAllPattern_ShouldMatch()
    {
        var key = SaasCacheKeys.DictItemTree(5, 77, true, 100);

        Assert.StartsWith("tenant:5:dict:77:", key, StringComparison.Ordinal);
        Assert.True(GlobMatches(SaasCacheKeys.AllDictItemTreesPattern(), key));
        Assert.True(GlobMatches(SaasCacheKeys.AllDictItemTreesPattern(), SaasCacheKeys.DictItemTree(null, 77, true, 100)));

        Assert.NotEqual(
            SaasCacheKeys.DictItemTree(5, 77, true, 100),
            SaasCacheKeys.DictItemTree(5, 77, false, 100),
            StringComparer.Ordinal);
        Assert.NotEqual(
            SaasCacheKeys.DictItemTree(5, 77, true, 100),
            SaasCacheKeys.DictItemTree(5, 78, true, 100),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Telegram 会话状态键按 机器人 × 会话 × 用户 三维隔离。
    /// </summary>
    [Fact]
    public void TelegramConversationState_ShouldCombineBotChatAndUser()
    {
        Assert.Equal("bot:10:20", SaasCacheKeys.TelegramConversationState("bot", 10, 20), StringComparer.Ordinal);
        Assert.NotEqual(
            SaasCacheKeys.TelegramConversationState("bot", 10, 20),
            SaasCacheKeys.TelegramConversationState("bot", 20, 10),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 会话状态键与其单会话失效模式必须逐字相同——两者一旦分叉，踢下线就清不掉缓存。
    /// </summary>
    [Fact]
    public void SessionStatePattern_ShouldExactlyMatchTheWrittenKey()
    {
        const string sessionId = "sess-abc";

        Assert.Equal("session:sess-abc", SaasCacheKeys.SessionState(sessionId), StringComparer.Ordinal);
        Assert.Equal(
            SaasCacheKeys.SessionState(sessionId),
            SaasCacheKeys.SessionStatePattern(sessionId),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 全部会话失效模式必须命中任意会话键。
    /// </summary>
    [Fact]
    public void AllSessionStatesPattern_ShouldMatchAnySessionKey()
    {
        Assert.True(GlobMatches(SaasCacheKeys.AllSessionStatesPattern(), SaasCacheKeys.SessionState("a")));
        Assert.True(GlobMatches(SaasCacheKeys.AllSessionStatesPattern(), SaasCacheKeys.SessionState("b-c-d")));
    }

    /// <summary>
    /// 缓存名称常量必须两两不同：任意两份缓存共用名称即等于共用命名空间，会互相覆盖。
    /// </summary>
    [Fact]
    public void SaasCacheNames_ShouldBeUnique()
    {
        var fields = typeof(SaasCacheNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (field.Name, Value: (string)field.GetRawConstantValue()!))
            .ToList();

        Assert.NotEmpty(fields);

        var duplicated = fields
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} ← {string.Join(", ", group.Select(item => item.Name))}")
            .ToList();

        Assert.True(duplicated.Count == 0, $"以下缓存名称被重复使用，缓存会互相覆盖：{string.Join(" | ", duplicated)}");
    }

    /// <summary>
    /// 缓存名称常量必须统一挂在 basicapp:saas: 命名空间下，避免与其它模块的缓存前缀混杂。
    /// </summary>
    [Fact]
    public void SaasCacheNames_ShouldShareModuleNamespacePrefix()
    {
        var offenders = typeof(SaasCacheNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (field.Name, Value: (string)field.GetRawConstantValue()!))
            .Where(item => !item.Value.StartsWith("basicapp:saas:", StringComparison.Ordinal))
            .Select(item => $"{item.Name}={item.Value}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下缓存名称未使用 basicapp:saas: 前缀：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 最小 glob 匹配：仅支持 <c>*</c> 通配任意长度片段，用于验证"键能被失效模式命中"。
    /// </summary>
    /// <param name="pattern">含 * 的匹配模式。</param>
    /// <param name="value">待匹配的缓存键。</param>
    /// <returns>是否命中。</returns>
    private static bool GlobMatches(string pattern, string value)
    {
        var regex = "^" + string.Join(
            ".*",
            pattern.Split('*').Select(System.Text.RegularExpressions.Regex.Escape)) + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(value, regex);
    }
}
