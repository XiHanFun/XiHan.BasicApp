#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserApiCredential
// Guid:a6e2d8b4-7f31-4c95-b0a8-3d5e9f2c6b17
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户 API 凭证实体
/// 个人级 OpenAPI 调用凭证（AppKey/AppSecret），供服务端签名调用开放接口
/// </summary>
/// <remarks>
/// 关联：
/// - UserId → SysUser（一人多凭证，数量由服务层限制）
///
/// 写入：
/// - AppKey 全局唯一（UX_ApKe），服务层以加密安全随机数生成，不可修改
/// - SecretHash 仅存哈希（与账号密码同栈 IPasswordHasher），明文 Secret 仅创建/滚动时返回一次
/// - 滚动密钥 = 重置 SecretHash，AppKey 保持不变（调用方仅需换 Secret）
///
/// 查询：
/// - 自助凭证列表：IX_UsId（按用户）
/// - 开放接口鉴权：按 AppKey 精确定位（UX_ApKe）
///
/// 删除：
/// - 仅软删；删除后该 AppKey 立即不可用
///
/// 状态：
/// - Status: Enabled=可用 / Disabled=停用（停用后签名校验直接拒绝）
///
/// 场景：
/// - 个人中心「开发者设置」自助管理
/// - OpenAPI 网关 HMAC 签名调用方身份（配合签名算法与 IP 白名单）
/// </remarks>
[SugarTable("SysUserApiCredential", "系统用户API凭证表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_ApKe", nameof(AppKey), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_UsId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
public partial class SysUserApiCredential : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 所属用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 凭证名称（用途备注，便于区分多凭证）
    /// </summary>
    [SugarColumn(ColumnDescription = "凭证名称", Length = 100, IsNullable = false)]
    public virtual string CredentialName { get; set; } = string.Empty;

    /// <summary>
    /// 应用键（调用方身份标识，全局唯一）
    /// </summary>
    [SugarColumn(ColumnDescription = "应用键", Length = 64, IsNullable = false)]
    public virtual string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 应用密钥哈希（严禁明文落库；明文仅创建/滚动时返回一次）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnDescription = "应用密钥哈希", Length = 200, IsNullable = false)]
    public virtual string SecretHash { get; set; } = string.Empty;

    /// <summary>
    /// 最后使用时间（开放接口鉴权成功时刷新）
    /// </summary>
    [SugarColumn(ColumnDescription = "最后使用时间", IsNullable = true)]
    public virtual DateTimeOffset? LastUsedTime { get; set; }

    /// <summary>
    /// 过期时间（为空表示永不过期）
    /// </summary>
    [SugarColumn(ColumnDescription = "过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
