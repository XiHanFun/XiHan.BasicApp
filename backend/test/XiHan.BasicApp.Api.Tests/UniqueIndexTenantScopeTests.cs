// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.Reflection;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 跨模块的唯一索引租户维度守卫。
/// </summary>
/// <remarks>
/// 多租户实体的读写两侧都按租户维度（<c>tenant_id</c> 行过滤），唯一约束却很容易漏掉它。
/// 一旦漏掉，租户 B 建同编码记录会撞上租户 A 的行，而那一行被租户过滤掉、B 根本看不见，
/// 表现为「编码明明没被占用却建不出来」，且没有任何一处能查出原因。
/// <para>
/// 2026-08-27 一次性发现四张表中招：Sys_Workflow_Definition、Sys_CodeGen_DataSource、
/// Sys_CodeGen_Table、Sys_CodeGen_Template。人肉 review 显然兜不住，改成会红的断言。
/// </para>
/// <para>
/// 同时守 <c>IsDeleted</c>：软删除实体的唯一索引若不含它，删掉一条之后同名再建会被拒，
/// 用户看到的是「已经删了却还说重复」。
/// </para>
/// </remarks>
public sealed class UniqueIndexTenantScopeTests
{
    /// <summary>
    /// 承载业务实体的模块程序集。
    /// </summary>
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(BasicApp.Saas.XiHanBasicAppSaasModule).Assembly,
        typeof(BasicApp.AI.XiHanBasicAppAIModule).Assembly,
        typeof(BasicApp.Chat.XiHanBasicAppChatModule).Assembly,
        typeof(BasicApp.CodeGeneration.XiHanBasicAppCodeGenerationModule).Assembly,
        typeof(BasicApp.Printing.XiHanBasicAppPrintingModule).Assembly,
        typeof(BasicApp.Workflow.XiHanBasicAppWorkflowModule).Assembly
    ];

    /// <summary>
    /// 允许全局唯一（不含 TenantId）的索引白名单，元素为「实体名.索引名」。
    /// 列入即声明：该约束的唯一性作用域本就是整个平台，不是租户内。
    /// </summary>
    private static readonly IReadOnlySet<string> GlobalUniqueAllowList =
        new HashSet<string>(StringComparer.Ordinal)
        {
            // OAuth 协议标识（3）：授权/兑换/验签发生在拿到租户上下文之前，作用域只能是全平台
            "SysOAuthApp.UX_{table}_ClId",      // client_id 对外公开，授权请求不带租户
            "SysOAuthCode.UX_{table}_Co",       // 授权码是一次性凭证，兑换时无租户上下文
            "SysOAuthToken.UX_{table}_AcJti",   // JWT ID，用于吊销与防重放，须全局可判重

            // 平台级实体（3）：实体注释明确写着 TenantId = 0、由平台运营管理
            "SysTenant.UX_{table}_TeCo",                    // 租户编码是租户自身的全局标识
            "SysTenantEdition.UX_{table}_EdCo",             // 套餐是平台售卖单元
            "SysTenantEditionPermission.UX_{table}_EdId_PeId", // 已由平台级 EditionId 定域

            // 平台级账号身份（2）：登录先按全局身份定位账号，再按成员关系选租户
            "SysUser.UX_{table}_Em",                 // 实体注释：Email 全平台唯一，是登录身份标识
            "SysUserApiCredential.UX_{table}_ApKe",  // 实体注释：AppKey 全局唯一，验签时不带租户上下文

            // 已由平台级 UserId 定域（3）：SysUser 本身是平台级实体，按 UserId 定域即已足够
            "SysUserNotificationPreference.UX_{table}_UsId",  // 一人一行
            "SysUserSecurity.UX_{table}_UsId",                // 一对一安全扩展
            "SysUserSetting.UX_{table}_UsId_Sc_SeKe"          // 一人一场景一键一行
        };

    /// <summary>
    /// 多租户实体的唯一索引必须包含 TenantId，否则租户之间会互相占用编码。
    /// </summary>
    [Fact]
    public void MultiTenantEntities_UniqueIndexesShouldIncludeTenantId()
    {
        var violations = EnumerateUniqueIndexes()
            .Where(entry => entry.IsMultiTenant)
            .Where(entry => !GlobalUniqueAllowList.Contains(entry.Key))
            .Where(entry => !entry.Fields.Contains(nameof(IMultiTenantEntity.TenantId), StringComparer.Ordinal))
            .Select(entry => $"{entry.Key}（{string.Join(", ", entry.Fields)}）")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个唯一索引作用于多租户实体却不含 TenantId，" +
            $"会让编码在全平台全局唯一、租户互相占名：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 软删除实体的唯一索引必须包含 IsDeleted，否则删除后编码无法复用。
    /// </summary>
    [Fact]
    public void SoftDeletableEntities_UniqueIndexesShouldIncludeIsDeleted()
    {
        var violations = EnumerateUniqueIndexes()
            .Where(entry => entry.IsSoftDeletable)
            .Where(entry => !GlobalUniqueAllowList.Contains(entry.Key))
            .Where(entry => !entry.Fields.Contains(nameof(ISoftDelete.IsDeleted), StringComparer.Ordinal))
            .Select(entry => $"{entry.Key}（{string.Join(", ", entry.Fields)}）")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个唯一索引作用于软删除实体却不含 IsDeleted，" +
            $"删掉一条后同编码再建会被拒：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 白名单不得残留失效条目：索引改名、删除或已补上 TenantId 后必须同步移除。
    /// </summary>
    [Fact]
    public void GlobalUniqueAllowList_ShouldNotContainStaleEntries()
    {
        var live = EnumerateUniqueIndexes()
            .Where(entry => !entry.Fields.Contains(nameof(IMultiTenantEntity.TenantId), StringComparer.Ordinal)
                            || !entry.Fields.Contains(nameof(ISoftDelete.IsDeleted), StringComparer.Ordinal))
            .Select(entry => entry.Key)
            .ToHashSet(StringComparer.Ordinal);

        var stale = GlobalUniqueAllowList
            .Where(key => !live.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(stale.Count == 0,
            $"下列 {stale.Count} 个白名单条目已失效（索引不存在，或已补齐 TenantId 与 IsDeleted），请移除：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, stale)}");
    }

    /// <summary>
    /// 枚举全部模块里持久化实体上声明的唯一索引。
    /// </summary>
    private static IReadOnlyList<UniqueIndexEntry> EnumerateUniqueIndexes()
    {
        return ModuleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetCustomAttributes<SugarTable>(inherit: false).Any())
            .SelectMany(type => type
                .GetCustomAttributes<SugarIndexAttribute>(inherit: false)
                .Where(index => index.IsUnique)
                .Select(index => new UniqueIndexEntry(
                    Key: $"{type.Name}.{index.IndexName}",
                    Fields: index.IndexFields.Keys.ToArray(),
                    IsMultiTenant: type.IsAssignableTo(typeof(IMultiTenantEntity)),
                    IsSoftDeletable: type.IsAssignableTo(typeof(ISoftDelete)))))
            .ToList();
    }

    /// <summary>
    /// 一条唯一索引的检查所需信息。
    /// </summary>
    /// <param name="Key">「实体名.索引名」。</param>
    /// <param name="Fields">索引列名（实体属性名）。</param>
    /// <param name="IsMultiTenant">实体是否是多租户实体。</param>
    /// <param name="IsSoftDeletable">实体是否软删除。</param>
    private sealed record UniqueIndexEntry(
        string Key,
        IReadOnlyList<string> Fields,
        bool IsMultiTenant,
        bool IsSoftDeletable);
}
