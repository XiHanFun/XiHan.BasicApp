#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigDetailDto
// Guid:00fc5a91-74cf-4ec6-9b59-60dbff6d87ff
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统配置详情 DTO
/// </summary>
public sealed class ConfigDetailDto : ConfigListItemDto
{
    /// <summary>
    /// 当前配置值（加密项不回传明文，返回 null，前端以「已加密」提示替代）
    /// </summary>
    public string? ConfigValue { get; set; }

    /// <summary>
    /// 默认值（加密项不回传明文，返回 null）
    /// </summary>
    public string? DefaultValue { get; set; }

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
