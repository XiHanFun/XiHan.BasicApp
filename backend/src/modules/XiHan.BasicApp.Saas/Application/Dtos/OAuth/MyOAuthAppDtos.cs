#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MyOAuthAppDtos
// Guid:7e0c5a41-3b28-4d19-9f06-2a8c4d7b1e50
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 我的 OAuth 应用列表项（个人中心开发者设置：仅本人创建的应用）
/// </summary>
public sealed class MyOAuthAppItemDto
{
    /// <summary>主键</summary>
    public long BasicId { get; set; }

    /// <summary>客户端ID（自动生成，全局唯一）</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>应用名称</summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>应用描述</summary>
    public string? AppDescription { get; set; }

    /// <summary>应用主页</summary>
    public string? Homepage { get; set; }

    /// <summary>应用Logo</summary>
    public string? Logo { get; set; }

    /// <summary>授权回调地址（多个用逗号/换行分隔）</summary>
    public string? RedirectUris { get; set; }

    /// <summary>客户端类型：Confidential（机密，有密钥）/ Public（公开，PKCE 无密钥）</summary>
    public string ClientType { get; set; } = "Confidential";

    /// <summary>授权类型（逗号分隔）</summary>
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>权限范围</summary>
    public string? Scopes { get; set; }

    /// <summary>状态</summary>
    public EnableStatus Status { get; set; }

    /// <summary>创建时间</summary>
    public DateTimeOffset CreatedTime { get; set; }
}

/// <summary>
/// 我的 OAuth 应用创建入参（精简字段 + 客户端类型；授权类型/范围/时效用默认值）
/// </summary>
public sealed class MyOAuthAppCreateDto
{
    /// <summary>应用名称</summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>客户端类型：Confidential / Public</summary>
    public string ClientType { get; set; } = "Confidential";

    /// <summary>应用主页</summary>
    public string? Homepage { get; set; }

    /// <summary>应用描述</summary>
    public string? AppDescription { get; set; }

    /// <summary>授权回调地址（必填，多个用逗号/换行分隔）</summary>
    public string RedirectUris { get; set; } = string.Empty;

    /// <summary>应用Logo</summary>
    public string? Logo { get; set; }
}

/// <summary>
/// 我的 OAuth 应用更新入参（仅可改展示字段与回调地址）
/// </summary>
public sealed class MyOAuthAppUpdateDto
{
    /// <summary>主键</summary>
    public long BasicId { get; set; }

    /// <summary>应用名称</summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>应用主页</summary>
    public string? Homepage { get; set; }

    /// <summary>应用描述</summary>
    public string? AppDescription { get; set; }

    /// <summary>授权回调地址（必填，多个用逗号/换行分隔）</summary>
    public string RedirectUris { get; set; } = string.Empty;

    /// <summary>应用Logo</summary>
    public string? Logo { get; set; }
}

/// <summary>
/// 我的 OAuth 应用状态入参（启用/停用）
/// </summary>
public sealed class MyOAuthAppStatusDto
{
    /// <summary>主键</summary>
    public long BasicId { get; set; }

    /// <summary>状态</summary>
    public EnableStatus Status { get; set; }
}

/// <summary>
/// 我的 OAuth 应用密钥响应（明文密钥仅创建/重置机密客户端时返回一次；公开客户端为空）
/// </summary>
public sealed class MyOAuthAppSecretDto
{
    /// <summary>主键</summary>
    public long BasicId { get; set; }

    /// <summary>客户端ID</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>客户端类型：Confidential / Public</summary>
    public string ClientType { get; set; } = "Confidential";

    /// <summary>明文客户端密钥（仅机密客户端返回一次；公开客户端为空字符串）</summary>
    public string ClientSecret { get; set; } = string.Empty;
}
