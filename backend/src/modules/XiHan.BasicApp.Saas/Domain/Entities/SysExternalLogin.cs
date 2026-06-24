#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysExternalLogin
// Guid:a1b2c3d4-5e6f-7890-abcd-ef1234567810
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统第三方登录绑定实体
/// 本地用户与外部身份提供商（WeChat/DingTalk/Google/GitHub 等）的绑定关系
/// </summary>
/// <remarks>
/// 关联：
/// - UserId → SysUser
///
/// 写入：
/// - Provider + ProviderKey + TenantId 唯一（UX_Pr_PrKe_TeId），保证同租户内一个第三方身份只绑一人
/// - ProviderKey 是第三方系统中的用户唯一标识（OpenId/UnionId 等）
/// - 同一用户可绑定多个 Provider（微信 + 钉钉 + 邮箱）
/// - 仅存身份关联（Provider/ProviderKey/邮箱/头像），不存第三方 Access/Refresh Token（当前无代用户调用第三方 API 的场景）
///
/// 查询：
/// - 第三方回调登录：按 (Provider, ProviderKey, TenantId) 精确定位本地用户
/// - 用户已绑定账号列表：IX_UsId
///
/// 删除：
/// - 仅软删（回填 DeletedTime）；解绑前校验防止失去唯一登录方式
///
/// 场景：
/// - 单点登录（SSO）：企业微信 / 钉钉 / 飞书扫码
/// - 社交登录：微信 / 支付宝 / GitHub / Google
/// - 账号找回：通过已绑定邮箱/手机号辅助登录
/// </remarks>
[SugarTable(TableName = "Sys_External_Login", TableDescription = "系统第三方登录绑定表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_Pr_PrKe_TeId", nameof(Provider), OrderByType.Asc, nameof(ProviderKey), OrderByType.Asc, nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_UsId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
public partial class SysExternalLogin : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 关联的内部用户ID
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 提供商名称（google、github、qq 等）
    /// </summary>
    [SugarColumn(ColumnName = "Provider", ColumnDescription = "提供商名称", Length = 50, IsNullable = false)]
    public virtual string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 提供商用户唯一标识
    /// </summary>
    [SugarColumn(ColumnName = "Provider_Key", ColumnDescription = "提供商用户标识", Length = 200, IsNullable = false)]
    public virtual string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 提供商显示名称
    /// </summary>
    [SugarColumn(ColumnName = "Provider_Display_Name", ColumnDescription = "提供商显示名称", Length = 200, IsNullable = true)]
    public virtual string? ProviderDisplayName { get; set; }

    /// <summary>
    /// 三方邮箱
    /// </summary>
    [SugarColumn(ColumnName = "Email", ColumnDescription = "三方邮箱", Length = 200, IsNullable = true)]
    public virtual string? Email { get; set; }

    /// <summary>
    /// 三方头像
    /// </summary>
    [SugarColumn(ColumnName = "Avatar_Url", ColumnDescription = "三方头像", Length = 500, IsNullable = true)]
    public virtual string? AvatarUrl { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    [SugarColumn(ColumnName = "Last_Login_Time", ColumnDescription = "最后登录时间", IsNullable = true)]
    public virtual DateTimeOffset? LastLoginTime { get; set; }
}
