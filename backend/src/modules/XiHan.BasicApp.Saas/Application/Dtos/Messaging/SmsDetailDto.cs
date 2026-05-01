#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsDetailDto
// Guid:1c09c75f-49eb-48cb-b723-2733e303d422
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统短信详情 DTO
/// </summary>
public sealed class SmsDetailDto : SmsListItemDto
{
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
