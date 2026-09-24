// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限码目录契约测试：权限码是种子、鉴权特性与前端菜单三方共用的字符串常量，
/// 一旦重复、拼写漂移或漏进种子定义，就会出现「有码无权限」或「两处指同一码」的静默越权。
/// 本类以反射遍历 <see cref="SaasPermissionCodes"/> 的全部嵌套常量，锁定唯一性、命名格式与种子覆盖。
/// </summary>
public sealed class SaasDomainPermissionCatalogContractTests
{
    /// <summary>
    /// 权限码常量值必须全局唯一：同一字符串被两个常量指向会让权限语义合并，产生越权。
    /// </summary>
    [Fact]
    public void PermissionCodeConstants_ShouldBeGloballyUnique()
    {
        var duplicates = GetDeclaredCodes()
            .GroupBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} <- {string.Join(" / ", group.Select(item => item.Name))}")
            .ToList();

        Assert.True(
            duplicates.Count == 0,
            $"以下权限码被多个常量重复定义：{Environment.NewLine}{string.Join(Environment.NewLine, duplicates)}");
    }

    /// <summary>
    /// 权限码必须是 saas:{资源段}:{动作段} 三段小写格式，段内仅允许小写字母、数字与连字符。
    /// </summary>
    [Fact]
    public void PermissionCodeConstants_ShouldFollowThreeSegmentLowerKebabFormat()
    {
        var violations = GetDeclaredCodes()
            .Where(item => !IsWellFormedPermissionCode(item.Code))
            .Select(item => $"{item.Name} = {item.Code}")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"以下权限码不符合 saas:资源:动作 的三段小写格式：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 权限码的资源段必须与所在嵌套类登记的分组码一致，否则种子分组会把权限归错组。
    /// </summary>
    [Fact]
    public void PermissionCodeConstants_ResourceSegment_ShouldMatchDeclaringGroup()
    {
        var violations = new List<string>();
        foreach (var nested in typeof(SaasPermissionCodes).GetNestedTypes(BindingFlags.Public))
        {
            var groupField = nested.GetField("Group", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (groupField?.GetRawConstantValue() is not string groupCode)
            {
                violations.Add($"{nested.Name} 缺少 Group 分组码常量");
                continue;
            }

            foreach (var field in EnumerateCodeFields(nested))
            {
                var code = (string)field.GetRawConstantValue()!;
                var segments = code.Split(':');
                if (segments.Length != 3 || !string.Equals(segments[1], groupCode, StringComparison.Ordinal))
                {
                    violations.Add($"{nested.Name}.{field.Name} = {code}，与分组码 {groupCode} 不一致");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"以下权限码的资源段与所在分组不一致：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 每个权限码常量都必须出现在种子定义中，否则该码永远不会落库，带它的接口对任何人都是拒绝。
    /// </summary>
    [Fact]
    public void PermissionCodeConstants_ShouldAllAppearInSeedDefinitions()
    {
        var defined = SaasPermissionDefinitions.All
            .Select(definition => definition.PermissionCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = GetDeclaredCodes()
            .Where(item => !defined.Contains(item.Code))
            .Select(item => $"{item.Name} = {item.Code}")
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"以下权限码常量未登记进 SaasPermissionDefinitions，永远不会被种子落库：{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    /// <summary>
    /// 种子定义中的每条权限码都必须有对应常量，禁止在定义表里手写魔法字符串。
    /// </summary>
    [Fact]
    public void SeedDefinitions_ShouldNotContainCodesWithoutConstant()
    {
        var declared = GetDeclaredCodes().Select(item => item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphans = SaasPermissionDefinitions.All
            .Select(definition => definition.PermissionCode)
            .Where(code => !declared.Contains(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(
            orphans.Count == 0,
            $"以下种子权限码没有对应的常量定义：{Environment.NewLine}{string.Join(Environment.NewLine, orphans)}");
    }

    /// <summary>
    /// 种子定义扁平表不得出现重复权限码，重复会导致种子插入冲突或覆盖。
    /// </summary>
    [Fact]
    public void SeedDefinitions_ShouldNotContainDuplicateCodes()
    {
        var duplicates = SaasPermissionDefinitions.All
            .GroupBy(definition => definition.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(
            duplicates.Count == 0,
            $"以下权限码在种子定义中重复出现：{Environment.NewLine}{string.Join(Environment.NewLine, duplicates)}");
    }

    /// <summary>
    /// 扁平定义表由分组派生：模块码恒为 saas，优先级恒等于排序号，条数与分组内权限项总数一致。
    /// </summary>
    [Fact]
    public void SeedDefinitions_ShouldBeDerivedFromGroupsWithFixedModuleAndPriority()
    {
        var expectedCount = SaasPermissionDefinitions.Groups.Sum(group => group.Permissions.Count);

        Assert.Equal(expectedCount, SaasPermissionDefinitions.All.Count);
        Assert.All(SaasPermissionDefinitions.All, definition =>
        {
            Assert.Equal(SaasPermissionCodes.Module, definition.ModuleCode, StringComparer.Ordinal);
            Assert.Equal(definition.Sort, definition.Priority);
        });
    }

    /// <summary>
    /// 标签由「模块 + 组码」生成，导出/导入动作追加动作段（与历史落库值一致）。
    /// </summary>
    [Fact]
    public void SeedDefinitions_Tags_ShouldAppendActionSegmentOnlyForExportAndImport()
    {
        var tenantRead = FindDefinition(SaasPermissionCodes.Tenant.Read);
        var tenantExport = FindDefinition(SaasPermissionCodes.Tenant.Export);

        Assert.Equal("[\"saas\",\"tenant\"]", tenantRead.Tags, StringComparer.Ordinal);
        Assert.Equal("[\"saas\",\"tenant\",\"export\"]", tenantExport.Tags, StringComparer.Ordinal);
    }

    /// <summary>
    /// 分组码在 Groups 中必须唯一，否则派生的组码到组名字典会因重复键抛异常或静默丢组。
    /// </summary>
    [Fact]
    public void PermissionGroups_ShouldHaveUniqueGroupCodes()
    {
        var duplicates = SaasPermissionDefinitions.Groups
            .GroupBy(group => group.GroupCode, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(
            duplicates.Count == 0,
            $"以下分组码重复：{Environment.NewLine}{string.Join(Environment.NewLine, duplicates)}");
        Assert.Equal(SaasPermissionDefinitions.Groups.Count, SaasPermissionDefinitions.GroupNames.Count);
    }

    /// <summary>
    /// 组内每条权限的资源段必须等于该组组码，保证按组码可以反查出全部权限。
    /// </summary>
    [Fact]
    public void PermissionGroups_EveryItemResourceSegment_ShouldEqualGroupCode()
    {
        var violations = SaasPermissionDefinitions.Groups
            .SelectMany(group => group.Permissions.Select(item => new { group.GroupCode, item.PermissionCode }))
            .Where(item => !string.Equals(
                SaasPermissionDefinitions.ResolveGroupCode(item.PermissionCode),
                item.GroupCode,
                StringComparison.Ordinal))
            .Select(item => $"{item.PermissionCode} 落在分组 {item.GroupCode}")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"以下权限项与所在分组的组码不一致：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 组码解析规则：三段码取中间段，非三段码回退首段，空值回退 other。
    /// </summary>
    /// <param name="permissionCode">待解析的权限码。</param>
    /// <param name="expected">期望解析出的组码。</param>
    [Theory]
    [InlineData("saas:tenant:read", "tenant")]
    [InlineData("saas:tenant-member:invite-status", "tenant-member")]
    [InlineData("saas:tenant:read:extra", "tenant")]
    [InlineData("saas:tenant", "saas")]
    [InlineData("saas", "saas")]
    [InlineData("::", "other")]
    [InlineData("", "other")]
    [InlineData("   ", "other")]
    [InlineData(null, "other")]
    public void ResolveGroupCode_ShouldFallBackPredictably(string? permissionCode, string expected)
    {
        Assert.Equal(expected, SaasPermissionDefinitions.ResolveGroupCode(permissionCode), StringComparer.Ordinal);
    }

    /// <summary>
    /// 组显示名解析：已登记组码返回中文组名，未登记组码原样回退组码本身。
    /// </summary>
    [Fact]
    public void ResolveGroupName_ShouldFallBackToGroupCodeWhenUnregistered()
    {
        Assert.Equal("租户", SaasPermissionDefinitions.ResolveGroupName(SaasPermissionCodes.Tenant.Read), StringComparer.Ordinal);
        Assert.Equal("nosuchgroup", SaasPermissionDefinitions.ResolveGroupName("saas:nosuchgroup:read"), StringComparer.Ordinal);
        Assert.Equal("other", SaasPermissionDefinitions.ResolveGroupName(null), StringComparer.Ordinal);
    }

    /// <summary>
    /// 组码到组名字典必须忽略大小写查找，避免调用方大小写不一致时漏名。
    /// </summary>
    [Fact]
    public void GroupNames_ShouldBeCaseInsensitive()
    {
        Assert.True(SaasPermissionDefinitions.GroupNames.ContainsKey("TENANT"));
        Assert.True(SaasPermissionDefinitions.GroupNames.ContainsKey("tenant"));
    }

    /// <summary>
    /// 汇总列表 <see cref="SaasPermissionCodes.All"/> 不得出现常量表中不存在的野码，也不得重复。
    /// </summary>
    [Fact]
    public void PermissionCodesAll_ShouldContainNoUnknownOrDuplicateCode()
    {
        var declared = GetDeclaredCodes().Select(item => item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unknown = SaasPermissionCodes.All.Where(code => !declared.Contains(code)).ToList();
        var duplicates = SaasPermissionCodes.All
            .GroupBy(code => code, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(unknown.Count == 0, $"All 中存在常量表未定义的权限码：{string.Join(" / ", unknown)}");
        Assert.True(duplicates.Count == 0, $"All 中存在重复权限码：{string.Join(" / ", duplicates)}");
    }

    /// <summary>
    /// 回归锚点：<see cref="SaasPermissionCodes.All"/> 必须覆盖常量表里的每一条权限码，一条都不能少。
    /// </summary>
    /// <remarks>
    /// 此前 <c>All</c> 是手写清单，与常量表已经漂移（漏了 <c>saas:log-trace:read</c>），
    /// 而按 <c>All</c> 做白名单/授权枚举的调用方会静默少一条权限——没有任何编译期或运行期信号。
    /// 现已改为由 <see cref="SaasPermissionDefinitions.All"/> 派生；本用例是"不许再退回手写清单"的守卫：
    /// 只要有人重新手抄一份并漏抄，这里立刻变红。
    /// </remarks>
    [Fact]
    public void PermissionCodesAll_ShouldCoverEveryDeclaredConstant()
    {
        var enumerated = SaasPermissionCodes.All.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = GetDeclaredCodes()
            .Where(item => !enumerated.Contains(item.Code))
            .Select(item => $"{item.Name} = {item.Code}")
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"以下权限码常量没有出现在自称「全部权限码」的 All 中：{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    /// <summary>
    /// 每个权限定义都声明了作用侧（平台 / 租户 / 两侧），没有未声明的 0
    /// </summary>
    [Fact]
    public void Definitions_ShouldAllDeclareSide()
    {
        var undeclared = SaasPermissionDefinitions.All
            .Where(definition => !definition.Side.IsDeclared())
            .Select(definition => definition.PermissionCode)
            .ToList();

        Assert.True(undeclared.Count == 0, $"以下权限没有声明作用侧：{string.Join("、", undeclared)}");
    }

    /// <summary>
    /// 作用侧契约：平台运维与目录维护归平台，组织与数据范围归租户，其余两侧都生效
    /// </summary>
    /// <param name="code">权限码</param>
    /// <param name="expected">期望作用侧</param>
    [Theory]
    // 租户目录是平台数据：SysTenant 行都在 0 号，租户持有查看码就能读到全部租户
    [InlineData("saas:tenant:read", PermissionSide.Platform)]
    [InlineData("saas:tenant:create", PermissionSide.Platform)]
    [InlineData("saas:tenant:initdb", PermissionSide.Platform)]
    [InlineData("saas:tenant-edition:read", PermissionSide.Platform)]
    [InlineData("saas:cache:clear", PermissionSide.Platform)]
    [InlineData("saas:task:read", PermissionSide.Platform)]
    // 目录维护是平台的，查看两侧都要（授权界面要列出可授的权限、菜单）
    [InlineData("saas:permission:create", PermissionSide.Platform)]
    [InlineData("saas:permission:read", PermissionSide.Both)]
    [InlineData("saas:menu:read", PermissionSide.Both)]
    [InlineData("saas:department:read", PermissionSide.Tenant)]
    [InlineData("saas:role-data-scope:update", PermissionSide.Both)]
    [InlineData("saas:user-data-scope:update", PermissionSide.Tenant)]
    [InlineData("saas:tenant-member:read", PermissionSide.Both)]
    [InlineData("saas:tenant-member:update", PermissionSide.Tenant)]
    [InlineData("saas:user:read", PermissionSide.Both)]
    [InlineData("saas:role:create", PermissionSide.Both)]
    public void Definitions_ShouldDeclareExpectedSide(string code, PermissionSide expected)
    {
        var definition = SaasPermissionDefinitions.All.Single(item => string.Equals(item.PermissionCode, code, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(expected, definition.Side);
    }

    /// <summary>
    /// 作用侧判定：两侧在平台与租户都生效，单侧只在自己那侧生效；未声明的不在任何一侧生效
    /// </summary>
    [Fact]
    public void Side_ShouldBeEffectiveOnlyInDeclaredContext()
    {
        Assert.True(PermissionSide.Both.IsEffectiveIn(isPlatformContext: true));
        Assert.True(PermissionSide.Both.IsEffectiveIn(isPlatformContext: false));
        Assert.True(PermissionSide.Platform.IsEffectiveIn(isPlatformContext: true));
        Assert.False(PermissionSide.Platform.IsEffectiveIn(isPlatformContext: false));
        Assert.False(PermissionSide.Tenant.IsEffectiveIn(isPlatformContext: true));
        Assert.True(PermissionSide.Tenant.IsEffectiveIn(isPlatformContext: false));
        Assert.False(default(PermissionSide).IsEffectiveIn(isPlatformContext: true));
        Assert.False(default(PermissionSide).IsTenantEffective());
        Assert.False(default(PermissionSide).IsDeclared());
    }

    private static IEnumerable<FieldInfo> EnumerateCodeFields(Type nestedType)
    {
        return nestedType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Where(field => !string.Equals(field.Name, "Group", StringComparison.Ordinal));
    }

    private static SaasPermissionDefinition FindDefinition(string permissionCode)
    {
        return SaasPermissionDefinitions.All.Single(definition =>
            string.Equals(definition.PermissionCode, permissionCode, StringComparison.Ordinal));
    }

    private static IReadOnlyList<(string Name, string Code)> GetDeclaredCodes()
    {
        return
        [
            .. typeof(SaasPermissionCodes)
                .GetNestedTypes(BindingFlags.Public)
                .SelectMany(nested => EnumerateCodeFields(nested)
                    .Select(field => ($"{nested.Name}.{field.Name}", (string)field.GetRawConstantValue()!)))
        ];
    }

    private static bool IsWellFormedPermissionCode(string code)
    {
        var segments = code.Split(':');
        if (segments.Length != 3)
        {
            return false;
        }

        if (!string.Equals(segments[0], SaasPermissionCodes.Module, StringComparison.Ordinal))
        {
            return false;
        }

        return segments.All(IsLowerKebabSegment);
    }

    private static bool IsLowerKebabSegment(string segment)
    {
        if (segment.Length == 0 || segment[0] == '-' || segment[^1] == '-')
        {
            return false;
        }

        return segment.All(static character =>
            character is >= 'a' and <= 'z'
            || character is >= '0' and <= '9'
            || character == '-');
    }
}
