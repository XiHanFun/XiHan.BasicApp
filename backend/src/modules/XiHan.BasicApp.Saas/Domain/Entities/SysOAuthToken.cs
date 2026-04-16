#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthToken
// Guid:7c28152c-d6e9-4396-addb-b479254bad31
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统 OAuth 令牌实体
/// Token 生命周期中心：承载 AccessTokenJti、RefreshToken、过期时间、轮换关系、重放检测；通过 SessionId 关联会话
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表只管"Token 凭据本身"；"设备/IP/在线状态"交由 SysUserSession
/// - AccessToken 本体不入库，仅存 JTI（黑名单/撤销校验使用）
///
/// 关联：
/// - ClientId → SysOAuthApp；UserId → SysUser；SessionId → SysUserSession
///
/// 写入：
/// - AccessTokenJti 全局唯一（UX_AcJti）
/// - RefreshToken 使用一次后必须轮换（旧值作废 + 写新 Token）
/// - IsRevoked=true 的 Token 立即写入黑名单缓存（Redis）
/// - 过期时间字段 AccessToken/RefreshToken 分别管理（后者远大于前者）
///
/// 查询：
/// - 鉴权 JWT 校验：按 AccessTokenJti 查 + 校验未撤销、未过期
/// - 刷新 Token：按 RefreshToken 查 + 校验使用次数
/// - 按会话批量吊销：IX_SeId + UPDATE IsRevoked=true
///
/// 删除：
/// - 硬删（空间敏感）或按过期时间归档
/// - 用户登出 / 强制下线时批量吊销同会话的所有 Token
///
/// 状态：
/// - IsRevoked: false=有效 / true=已吊销
///
/// 场景：
/// - JWT 鉴权 + 黑名单
/// - RefreshToken 轮换与重放检测
/// - 强制多端下线、会话吊销
/// </remarks>
[SugarTable("SysOAuthToken", "系统OAuth令牌表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_AcJti", nameof(AccessTokenJti), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_ReTo", nameof(RefreshToken), OrderByType.Asc)]
[SugarIndex("IX_{table}_SeId", nameof(SessionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_ClId", nameof(ClientId), OrderByType.Asc)]
[SugarIndex("IX_{table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsRe", nameof(IsRevoked), OrderByType.Asc)]
[SugarIndex("IX_{table}_AcToExTi", nameof(AccessTokenExpiresTime), OrderByType.Asc)]
public partial class SysOAuthToken : BasicAppCreationEntity
{
    /// <summary>
    /// 会话ID（关联 SysUserSession，用于多端控制与撤销）
    /// </summary>
    [SugarColumn(ColumnDescription = "会话ID", IsNullable = true)]
    public virtual long? SessionId { get; set; }

    /// <summary>
    /// 访问令牌 JTI（JWT ID，用于黑名单与重放检测；非 JWT 时可与 AccessToken 同值）
    /// </summary>
    [SugarColumn(ColumnDescription = "访问令牌JTI", Length = 200, IsNullable = false)]
    public virtual string AccessTokenJti { get; set; } = string.Empty;

    /// <summary>
    /// 访问令牌（敏感信息；JWT 场景可不存正文仅用 JTI，兼容 opaque token 时保留）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnDescription = "访问令牌", Length = 1000, IsNullable = true)]
    public virtual string? AccessToken { get; set; }

    /// <summary>
    /// 刷新令牌（敏感信息；仅在此表存储，Session 不存）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnDescription = "刷新令牌", Length = 1000, IsNullable = true)]
    public virtual string? RefreshToken { get; set; }

    /// <summary>
    /// 被替换后的新 Token 的 RefreshToken 或 JTI（轮换检测：若已撤销且存在则视为重放，需撤销整会话）
    /// </summary>
    [SugarColumn(ColumnDescription = "被替换为的Token标识", Length = 200, IsNullable = true)]
    public virtual string? ReplacedByToken { get; set; }

    /// <summary>
    /// 令牌类型
    /// </summary>
    [SugarColumn(ColumnDescription = "令牌类型", Length = 20, IsNullable = false)]
    public virtual string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 客户端ID
    /// </summary>
    [SugarColumn(ColumnDescription = "客户端ID", Length = 100, IsNullable = false)]
    public virtual string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = true)]
    public virtual long? UserId { get; set; }

    /// <summary>
    /// 授权类型
    /// </summary>
    [SugarColumn(ColumnDescription = "授权类型")]
    public virtual GrantType GrantType { get; set; } = GrantType.AuthorizationCode;

    /// <summary>
    /// 权限范围
    /// </summary>
    [SugarColumn(ColumnDescription = "权限范围", Length = 500, IsNullable = true)]
    public virtual string? Scopes { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态", IsNullable = true)]
    public virtual YesOrNo? State { get; set; }

    /// <summary>
    /// 访问令牌过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "访问令牌过期时间")]
    public virtual DateTimeOffset AccessTokenExpiresTime { get; set; }

    /// <summary>
    /// 刷新令牌过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "刷新令牌过期时间", IsNullable = true)]
    public virtual DateTimeOffset? RefreshTokenExpiresTime { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已撤销")]
    public virtual bool IsRevoked { get; set; } = false;

    /// <summary>
    /// 撤销时间
    /// </summary>
    [SugarColumn(ColumnDescription = "撤销时间", IsNullable = true)]
    public virtual DateTimeOffset? RevokedTime { get; set; }
}
