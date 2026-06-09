#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PagePreferenceDtos
// Guid:8d4f0125-6e70-4c1d-8f4b-3a5e9c2d7b84
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 页面偏好 DTO
/// </summary>
public sealed class PagePreferenceDto
{
    /// <summary>
    /// 页面码
    /// </summary>
    public string PageCode { get; set; } = string.Empty;

    /// <summary>
    /// 偏好载荷（JSON；无则为空）
    /// </summary>
    public string? Payload { get; set; }
}

/// <summary>
/// 保存页面偏好入参
/// </summary>
public sealed class PagePreferenceSaveDto
{
    /// <summary>
    /// 页面码
    /// </summary>
    public string PageCode { get; set; } = string.Empty;

    /// <summary>
    /// 偏好载荷（JSON）
    /// </summary>
    public string? Payload { get; set; }
}
