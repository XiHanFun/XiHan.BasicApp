#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ApiLogDetailDto
// Guid:5479bd75-6c99-4e4d-ae37-6ee663d21d94
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// API 日志详情 DTO
/// </summary>
public sealed class ApiLogDetailDto : ApiLogListItemDto
{
    /// <summary>
    /// 创建者主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    public string? CreatedBy { get; set; }
}
