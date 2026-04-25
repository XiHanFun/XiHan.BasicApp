#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthApp
// Guid:5c28152c-d6e9-4396-addb-b479254bad29
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统 OAuth 应用实体
/// OAuth2/OIDC 客户端注册信息：承载 ClientId/Secret、回调地址、授权模式、作用域
/// </summary>
/// <remarks>
/// 关联：
/// - 反向：SysOAuthCode.ClientId、SysOAuthToken.ClientId
///
/// 写入：
/// - ClientId 全局唯一（UX_ClId），通常使用 UUID 或自动生成
/// - ClientSecret 严禁明文落库（本表用 JsonIgnore 阻止序列化，但业务层仍需确保加密存储）
/// - RedirectUris 建议 JSON 数组存多个回调；校验阶段必须严格匹配
/// - AppType 区分机密/公开客户端（SPA 不应下发 ClientSecret）
///
/// 查询：
/// - 授权请求入口：按 ClientId 精确定位
/// - 租户应用列表：IX_TeId_St
///
/// 删除：
/// - 仅软删；删除前应吊销所有相关 Token/Code
///
/// 状态：
/// - Status: Yes=可用 / No=停用（停用后授权请求直接拒绝）
///
/// 场景：
/// - 第三方接入：企业内其他系统接入本平台 SSO
/// - 移动/SPA 应用授权
/// - 对外开放 API 平台
/// </remarks>
[SugarTable("SysOAuthApp", "系统OAuth应用表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_ClId", nameof(ClientId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_ApNa", nameof(AppName), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_ApTy", nameof(AppType), OrderByType.Asc)]
public partial class SysOAuthApp : BasicAppAggregateRoot
{
    /// <summary>
    /// 应用名称
    /// </summary>
    [SugarColumn(ColumnDescription = "应用名称", Length = 100, IsNullable = false)]
    public virtual string AppName { get; set; } = string.Empty;

    /// <summary>
    /// 应用描述
    /// </summary>
    [SugarColumn(ColumnDescription = "应用描述", Length = 500, IsNullable = true)]
    public virtual string? AppDescription { get; set; }

    /// <summary>
    /// 客户端ID
    /// </summary>
    [SugarColumn(ColumnDescription = "客户端ID", Length = 100, IsNullable = false)]
    public virtual string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 客户端密钥
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnDescription = "客户端密钥", Length = 200, IsNullable = false)]
    public virtual string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 应用类型
    /// </summary>
    [SugarColumn(ColumnDescription = "应用类型")]
    public virtual OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    /// <summary>
    /// 支持的授权类型（多个用逗号分隔）
    /// </summary>
    [SugarColumn(ColumnDescription = "支持的授权类型", Length = 200, IsNullable = false)]
    public virtual string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 重定向URI（多个用逗号分隔）
    /// </summary>
    [SugarColumn(ColumnDescription = "重定向URI", Length = 1000, IsNullable = true)]
    public virtual string? RedirectUris { get; set; }

    /// <summary>
    /// 权限范围（多个用逗号分隔）
    /// </summary>
    [SugarColumn(ColumnDescription = "权限范围", Length = 500, IsNullable = true)]
    public virtual string? Scopes { get; set; }

    /// <summary>
    /// 访问令牌有效期（秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "访问令牌有效期（秒）")]
    public virtual int AccessTokenLifetime { get; set; } = 3600;

    /// <summary>
    /// 刷新令牌有效期（秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "刷新令牌有效期（秒）")]
    public virtual int RefreshTokenLifetime { get; set; } = 2592000;

    /// <summary>
    /// 授权码有效期（秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "授权码有效期（秒）")]
    public virtual int AuthorizationCodeLifetime { get; set; } = 300;

    /// <summary>
    /// 应用Logo
    /// </summary>
    [SugarColumn(ColumnDescription = "应用Logo", Length = 500, IsNullable = true)]
    public virtual string? Logo { get; set; }

    /// <summary>
    /// 应用主页
    /// </summary>
    [SugarColumn(ColumnDescription = "应用主页", Length = 200, IsNullable = true)]
    public virtual string? Homepage { get; set; }

    /// <summary>
    /// 是否跳过授权
    /// </summary>
    [SugarColumn(ColumnDescription = "是否跳过授权")]
    public virtual bool SkipConsent { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
