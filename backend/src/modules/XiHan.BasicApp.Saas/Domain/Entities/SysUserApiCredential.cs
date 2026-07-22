// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// - SecretCipher 可逆加密落库（Data Protection，独立 Purpose）——开放接口 HMAC 验签需还原明文密钥参与运算，
///   故不能用单向哈希；明文 Secret 仍仅创建/滚动时返回一次
/// - 滚动密钥 = 重置 SecretCipher，AppKey 保持不变（调用方仅需换 Secret）
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
[SugarTable(TableName = "Sys_User_Api_Credential", TableDescription = "系统用户API凭证表")]
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
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 凭证名称（用途备注，便于区分多凭证）
    /// </summary>
    [SugarColumn(ColumnName = "Credential_Name", ColumnDescription = "凭证名称", Length = 100, IsNullable = false)]
    public virtual string CredentialName { get; set; } = string.Empty;

    /// <summary>
    /// 应用键（调用方身份标识，全局唯一）
    /// </summary>
    [SugarColumn(ColumnName = "App_Key", ColumnDescription = "应用键", Length = 64, IsNullable = false)]
    public virtual string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 应用密钥密文（可逆加密落库，dp: 前缀；严禁明文落库/回显，明文仅创建/滚动时返回一次）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnName = "Secret_Cipher", ColumnDescription = "应用密钥密文", Length = 500, IsNullable = false)]
    public virtual string SecretCipher { get; set; } = string.Empty;

    /// <summary>
    /// 签名算法（开放接口验签使用；缺省 HMAC-SHA256）
    /// </summary>
    [SugarColumn(ColumnName = "Signature_Algorithm", ColumnDescription = "签名算法")]
    public virtual SignatureType SignatureAlgorithm { get; set; } = SignatureType.HmacSha256;

    /// <summary>
    /// IP 白名单（逗号分隔，支持前缀通配 * 与 *=全放行；为空表示不限制来源 IP）
    /// </summary>
    [SugarColumn(ColumnName = "Ip_Whitelist", ColumnDescription = "IP白名单", Length = 1000, IsNullable = true)]
    public virtual string? IpWhitelist { get; set; }

    /// <summary>
    /// 最后使用时间（开放接口鉴权成功时刷新）
    /// </summary>
    [SugarColumn(ColumnName = "Last_Used_Time", ColumnDescription = "最后使用时间", IsNullable = true)]
    public virtual DateTimeOffset? LastUsedTime { get; set; }

    /// <summary>
    /// 过期时间（为空表示永不过期）
    /// </summary>
    [SugarColumn(ColumnName = "Expiration_Time", ColumnDescription = "过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
