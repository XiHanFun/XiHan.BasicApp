#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUser
// Guid:2b28152c-d6e9-4396-addb-b479254bad0c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:18:56
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户实体
/// RBAC 主体（Subject）：承载身份、资料、安全基础信息；用户的"可访问租户集合"由 SysTenantUser 承载
/// </summary>
/// <remarks>
/// 语义澄清（重要）：
/// - SysUser.TenantId = 用户的"主账号归属租户"（注册地 / 合同所属方），不等同于"用户能进入哪个租户"
/// - 用户实际可访问的租户集合由 SysTenantUser 多对多关系承载（含主租户 + 外部协作租户 + 平台管理租户）
/// - 登录后的"生效租户上下文"在 SysUserSession.TenantId 中记录
///
/// 关联：
/// - 反向：SysTenantUser.UserId（多对多成员关系）、SysUserRole/SysUserPermission/SysUserDepartment（按 TenantId + UserId 分别作用于不同租户上下文）、SysUserSession/SysUserSecurity/SysUserStatistics
///
/// 写入：
/// - TenantId + UserName 在主归属租户内唯一（UX_TeId_UsNa）；Email 全平台唯一（UX_Em，登录身份标识，可空但有值必唯一）
/// - Password 必须先经应用层加密（Argon2/BCrypt），严禁明文落库
/// - IsSystemAccount=true 的账号禁止修改 UserName / 禁止被软删
/// - 创建时必须同步：(1) SysUserSecurity 一对一扩展 (2) 一条 SysTenantUser 记录（TenantId=SysUser.TenantId + MemberType=Owner/Member + InviteStatus=Accepted）；平台账号（TenantId=0）不建成员关系
///
/// 查询：
/// - 登录流程（先登录后选租户）：(1) 输入 Email + Password → WHERE Email=? 全局唯一定位（走 UX_Em）；
///                                平台账号也可用 UserName 登录（WHERE TenantId=0 AND UserName=?）
///                              (2) 登录成功后按成员关系决定落点：超管/平台→控制中心(平台态)；恰一个租户→直进；多个→控制中心选择
///                              (3) 进入租户后可随时通过 SwitchTenant 切换租户 / 返回平台态
/// - 鉴权决策：UserId + 当前会话 TenantId → 查 SysTenantUser 校验成员身份 → 再查 SysUserRole 加载角色
/// - 手机查询走 IX_Ph（非唯一，仅作辅助找回/验证）
/// - 按激活状态筛选：IX_TeId_St_IsAc
///
/// 删除：
/// - 仅支持软删（IsDeleted=1）保留审计
/// - 软删前必须：吊销所有 SysUserSession、吊销所有 Token、将 SysTenantUser 全部置为 Revoked
///
/// 状态：
/// - Status: Yes=启用 / No=禁用（禁用后所有租户下均不可登录）
/// - IsActive: false=未激活（邮箱/手机未验证），true=已激活；与 Status 正交：未激活+启用仍不可登录，已激活+禁用亦不可登录
///
/// 场景：
/// - 登录认证入口（身份校验）
/// - 用户资料维护
/// - 统一身份：同一自然人可通过 SysTenantUser 在多个租户拥有成员身份
/// </remarks>
[SugarTable(TableName = "Sys_User", TableDescription = "系统用户表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_UsNa", nameof(TenantId), OrderByType.Asc, nameof(UserName), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_UsNa", nameof(UserName), OrderByType.Asc)]
[SugarIndex("UX_{table}_Em", nameof(Email), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_Ph", nameof(Phone), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St_IsAc", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc, nameof(IsActive), OrderByType.Asc)]
public partial class SysUser : BasicAppAggregateRoot
{
    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnName = "User_Name", ColumnDescription = "用户名", Length = 50, IsNullable = false)]
    public virtual string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    [SugarColumn(ColumnName = "Real_Name", ColumnDescription = "真实姓名", Length = 50, IsNullable = true)]
    public virtual string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [SugarColumn(ColumnName = "Nick_Name", ColumnDescription = "昵称", Length = 50, IsNullable = true)]
    public virtual string? NickName { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    [SugarColumn(ColumnName = "Avatar", ColumnDescription = "头像", Length = 500, IsNullable = true)]
    public virtual string? Avatar { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(ColumnName = "Email", ColumnDescription = "邮箱（登录身份标识，全平台唯一）", Length = 256, IsNullable = true)]
    public virtual string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [SugarColumn(ColumnName = "Phone", ColumnDescription = "手机号", Length = 20, IsNullable = true)]
    public virtual string? Phone { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    [SugarColumn(ColumnName = "Gender", ColumnDescription = "性别")]
    public virtual UserGender Gender { get; set; } = UserGender.Unknown;

    /// <summary>
    /// 生日
    /// </summary>
    [SugarColumn(ColumnName = "Birthday", ColumnDescription = "生日", IsNullable = true)]
    public virtual DateTimeOffset? Birthday { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 是否已激活（IsActive=false表示邮箱/手机未验证，Status=Disabled表示管理员禁用）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Active", ColumnDescription = "是否已激活")]
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    [SugarColumn(ColumnName = "Last_Login_Time", ColumnDescription = "最后登录时间", IsNullable = true)]
    public virtual DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 最后登录IP
    /// </summary>
    [SugarColumn(ColumnName = "Last_Login_Ip", ColumnDescription = "最后登录IP", Length = 50, IsNullable = true)]
    public virtual string? LastLoginIp { get; set; }

    /// <summary>
    /// 时区
    /// </summary>
    [SugarColumn(ColumnName = "Time_Zone", ColumnDescription = "时区", Length = 50, IsNullable = true)]
    public virtual string? TimeZone { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    [SugarColumn(ColumnName = "Language", ColumnDescription = "语言", Length = 10, IsNullable = true)]
    public virtual string? Language { get; set; } = "zh-CN";

    /// <summary>
    /// 国家/地区
    /// </summary>
    [SugarColumn(ColumnName = "Country", ColumnDescription = "国家/地区", Length = 50, IsNullable = true)]
    public virtual string? Country { get; set; }

    /// <summary>
    /// 是否为系统内置账号（不可修改用户名）
    /// </summary>
    [SugarColumn(ColumnName = "Is_System_Account", ColumnDescription = "是否系统内置账号")]
    public virtual bool IsSystemAccount { get; set; } = false;

    /// <summary>
    /// 用户级数据权限范围覆盖（与 SysRole.DataScope 对称的用户级覆盖；null=不覆盖，按角色 DataScope 生效）
    /// </summary>
    /// <remarks>
    /// - 非空时优先级高于角色 DataScope（如 CEO 角色是部门经理但需看全部数据，可置 All）
    /// - 取值 Custom 时，须在 SysUserDataScope 中枚举可见部门集合
    /// - 禁止依赖枚举数值大小做权限合并，必须按 DataPermissionScope 注释中的显式语义解释
    /// </remarks>
    [SugarColumn(ColumnName = "Data_Scope_Override", ColumnDescription = "数据权限范围覆盖", IsNullable = true)]
    public virtual DataPermissionScope? DataScopeOverride { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
