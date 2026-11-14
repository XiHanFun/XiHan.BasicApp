// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户成员（平台库，表 <c>sys_tenant_member</c>）
/// </summary>
/// <remarks>
/// 注册地不在该租户的用户以成员身份进入它（切换租户、在其中被模仿）；注册地在该租户的用户天然可进入，不记成员行
/// 落平台库、不实现 <c>IMultiTenant</c>，按目录键 <see cref="TenantId"/>（会话与令牌里带的就是它）而不是主键指向租户：切换租户、续签与模仿判能否进入时调用方还在原来那一侧，成员行要在哪一侧都查得到
/// 一个用户在一个租户里至多一行，部分唯一索引 <c>ux_sys_tenant_member_tenant_id_user_id</c> 只约束未删除的行，移除后可以重新加入
/// 成员类型不带权限，权限仍由 Access 的角色决定；运维支持成员只能是平台账号，不占席位、不能成为所有者
/// 生效期在每次判定时现算（切换、续签、模仿、列成员），没有作业去改状态；移除后已在该租户里的会话到下次续签才被拒
/// Identity 经 Contracts 的 <c>ITenantMembershipResolver</c> 判能否进入、谁是所有者，移除成员时经 <c>ITenantInitializer</c> 让 Access 解除他在该租户的角色：Identity 不引用 Tenancy，Tenancy 也不引用 Access
/// 删除用户或租户都不连带删除成员行
/// </remarks>
[SugarTable(TableName = "sys_tenant_member", TableDescription = "租户成员（平台库，表 sys_tenant_member）")]
[ActiveUniqueIndex(nameof(TenantId), nameof(UserId), Name = "ux_sys_tenant_member_tenant_id_user_id")]
[SugarIndex("ix_sys_tenant_member_user_id", nameof(UserId), OrderByType.Asc)]
public sealed class SysTenantMember : AppAuditedEntity
{
    /// <summary>
    /// 租户 ID（目录键）
    /// </summary>
    [SugarColumn(ColumnName = "tenant_id", Length = 512, IsNullable = false, ColumnDescription = "租户 ID（目录键）")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 用户 ID
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户 ID")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 成员类型
    /// </summary>
    [SugarColumn(ColumnName = "member_type", IsNullable = false, ColumnDescription = "成员类型")]
    public TenantMemberType MemberType { get; set; } = TenantMemberType.Member;

    /// <summary>
    /// 生效时间（UTC）
    /// </summary>
    /// <remarks>
    /// 为空即加入即生效
    /// </remarks>
    [SugarColumn(ColumnName = "effective_at", IsNullable = true, ColumnDescription = "生效时间（UTC）")]
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 到期时间（UTC）
    /// </summary>
    /// <remarks>
    /// 为空即不到期；到期后不能再进入该租户
    /// </remarks>
    [SugarColumn(ColumnName = "expires_at", IsNullable = true, ColumnDescription = "到期时间（UTC）")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 在 <paramref name="now"/> 是否生效
    /// </summary>
    public bool IsInEffect(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}

/// <summary>
/// 租户成员的类型
/// </summary>
/// <remarks>
/// 成员类型不带任何权限，权限仍由 Access 的角色决定。租户的所有者不是成员类型，是 <see cref="SysTenant.AdminUserId"/>（每租户唯一）
/// </remarks>
public enum TenantMemberType
{
    /// <summary>
    /// 普通成员
    /// </summary>
    /// <remarks>
    /// 注册地在别处的租户内用户：能切换进去、能在其中被模仿、占席位、可接任所有者
    /// </remarks>
    Member = 0,

    /// <summary>
    /// 运维支持成员
    /// </summary>
    /// <remarks>
    /// 平台运营方跨租户进入做运维、客服与风控：只能是平台账号，平台账号也只能以这种身份加入；
    /// 能切换进去，但不占席位、不能成为所有者，也不算租户内的用户（不进模仿候选、不收租户的群发、不在聊天目录）；用户管理列表带标记列出、账号只读、能配角色
    /// </remarks>
    Support = 1,
}
