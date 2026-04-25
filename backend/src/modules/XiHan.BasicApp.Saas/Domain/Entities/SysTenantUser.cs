#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantUser
// Guid:f2a3b4c5-6789-0123-cdef-678901234567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统租户成员实体
/// 用户 × 租户的多对多成员关系表：表达"某用户在某租户拥有成员身份"，是跨租户协作与平台管理员统一建模的入口
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表承载"谁能进入哪个租户"；"进入后有什么角色"由 SysUserRole 承载；"在租户内的哪些部门"由 SysUserDepartment 承载
/// - 语义区分：
///     · SysUser.TenantId       = 用户的主账号归属租户（注册地）
///     · SysTenantUser.TenantId = 用户拥有成员身份的租户（含主租户 + 外部协作租户）
/// - 所有租户访问（含主属）统一走本表，鉴权路径简单一致
///
/// 关联：
/// - UserId → SysUser；TenantId（继承自基类）→ SysTenant
/// - InvitedBy → SysUser（邀请人；系统自动创建时可为 null）
///
/// 写入：
/// - TenantId + UserId 租户内唯一（UX_TeId_UsId），避免重复成员身份
/// - 主账号归属同步：SysUser 创建时应自动写入一条 TenantId=SysUser.TenantId + MemberType=Owner/Member + InviteStatus=Accepted 的记录
/// - 邀请流程：InviteStatus=Pending → 用户接受后改为 Accepted；拒绝/撤销/过期按状态流转
/// - 仅 InviteStatus=Accepted 且 Status=Yes 且当前时间在 Effective~Expiration 范围内方可用于鉴权
/// - MemberType=PlatformAdmin 必须由平台运营账号创建，禁止租户管理员操作
///
/// 查询：
/// - 登录后"我能进入的租户列表"：IX_UsId + WHERE UserId=? AND InviteStatus=Accepted AND Status=Yes
/// - 租户内成员管理：按 TenantId 筛选 + 按 MemberType/InviteStatus 过滤
/// - 待处理邀请：IX_InSt + WHERE InviteStatus=Pending
/// - 即将过期：IX_ExTi 定时扫描
///
/// 删除：
/// - 仅软删；删除成员身份等价于将 InviteStatus=Revoked 更合规（保留审计）
/// - 级联：删除/吊销时应同步：(1) 吊销该用户对应租户的在线会话 (2) 清理 SysUserRole / SysUserDepartment / SysUserPermission 中匹配 (TenantId, UserId) 的记录
///
/// 状态：
/// - MemberType: Owner/Admin/Member/External/Guest/Consultant/PlatformAdmin
/// - InviteStatus: Pending → Accepted/Rejected/Revoked/Expired
/// - Status: Yes=有效 / No=暂停（保留关系但不生效）
///
/// 场景：
/// - 租户创建者自动成为 Owner
/// - 邀请外部供应商/顾问协作（External/Consultant）
/// - 集团总部用户跨子公司访问（PlatformAdmin 或 Admin）
/// - 登录后切换租户（类似 GitHub 切换 Org）
/// </remarks>
[SugarTable("SysTenantUser", "系统租户成员表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_UsId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_MeTy", nameof(MemberType), OrderByType.Asc)]
[SugarIndex("IX_{table}_InSt", nameof(InviteStatus), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_ExTi", nameof(ExpirationTime), OrderByType.Asc)]
public partial class SysTenantUser : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 用户ID（指向 SysUser，与当前 TenantId 共同定位一条租户成员关系）
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 成员类型（Owner/Admin/Member/External/Guest/Consultant/PlatformAdmin）
    /// </summary>
    /// <remarks>
    /// 设计决策：MemberType 表达"成员身份类型"而非"权限级别"。
    /// 服务层禁止直接判断 MemberType 做鉴权（如 if MemberType==Admin），
    /// 必须走 RBAC 权限链（SysUserRole → SysRolePermission → SysPermission）。
    /// MemberType 仅用于：成员列表展示分类、邀请流程控制、PlatformAdmin 创建权限校验。
    /// </remarks>
    [SugarColumn(ColumnDescription = "成员类型")]
    public virtual TenantMemberType MemberType { get; set; } = TenantMemberType.Member;

    /// <summary>
    /// 邀请状态（Pending/Accepted/Rejected/Revoked/Expired）
    /// </summary>
    [SugarColumn(ColumnDescription = "邀请状态")]
    public virtual TenantMemberInviteStatus InviteStatus { get; set; } = TenantMemberInviteStatus.Accepted;

    /// <summary>
    /// 邀请人ID（手动邀请时填写；系统自动创建时可为空）
    /// </summary>
    [SugarColumn(ColumnDescription = "邀请人ID", IsNullable = true)]
    public virtual long? InvitedBy { get; set; }

    /// <summary>
    /// 邀请时间
    /// </summary>
    [SugarColumn(ColumnDescription = "邀请时间", IsNullable = true)]
    public virtual DateTimeOffset? InvitedTime { get; set; }

    /// <summary>
    /// 接受/响应时间（用户接受或拒绝邀请的时间）
    /// </summary>
    [SugarColumn(ColumnDescription = "响应时间", IsNullable = true)]
    public virtual DateTimeOffset? RespondedTime { get; set; }

    /// <summary>
    /// 生效时间（为空表示立即生效）
    /// </summary>
    [SugarColumn(ColumnDescription = "生效时间", IsNullable = true)]
    public virtual DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间（为空表示永不过期；常用于外部协作者/访客的时效控制）
    /// </summary>
    [SugarColumn(ColumnDescription = "失效时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 最近活跃时间（用户在该租户上下文下最近一次登录或操作的时间，用于清理僵尸成员关系）
    /// </summary>
    [SugarColumn(ColumnDescription = "最近活跃时间", IsNullable = true)]
    public virtual DateTimeOffset? LastActiveTime { get; set; }

    /// <summary>
    /// 在该租户的显示名（可覆盖 SysUser.RealName，如外部协作者在不同客户处使用不同称呼）
    /// </summary>
    [SugarColumn(ColumnDescription = "租户内显示名", Length = 100, IsNullable = true)]
    public virtual string? DisplayName { get; set; }

    /// <summary>
    /// 邀请备注（管理员填写的协作说明、权限说明等）
    /// </summary>
    [SugarColumn(ColumnDescription = "邀请备注", Length = 500, IsNullable = true)]
    public virtual string? InviteRemark { get; set; }

    /// <summary>
    /// 状态（Yes=有效 / No=暂停，暂停后不再生效但保留关系）
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
