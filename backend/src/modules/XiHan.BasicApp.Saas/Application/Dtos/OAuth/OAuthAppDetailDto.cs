// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// OAuth 应用详情 DTO
/// </summary>
public sealed class OAuthAppDetailDto : OAuthAppListItemDto
{
    /// <summary>
    /// 重定向 URI
    /// </summary>
    public string? RedirectUris { get; set; }

    /// <summary>
    /// 应用 Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 应用主页
    /// </summary>
    public string? Homepage { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建者主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 修改者主键
    /// </summary>
    public long? ModifiedId { get; set; }

    /// <summary>
    /// 修改者
    /// </summary>
    public string? ModifiedBy { get; set; }
}
