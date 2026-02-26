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
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统 OAuth 应用实体
/// </summary>
[SugarTable("Sys_OAuth_App", "系统 OAuth 应用表")]
[SugarIndex("IX_SysOAuthApp_ClId", nameof(ClientId), OrderByType.Asc, true)]
[SugarIndex("IX_SysOAuthApp_ApNa", nameof(AppName), OrderByType.Asc)]
public partial class SysOAuthApp : RbacAggregateRoot<long>
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
