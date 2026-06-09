#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationPageQueryDto
// Guid:4ca74164-6d94-4354-9d2b-6a74b498cf80
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 操作分页查询 DTO
/// </summary>
public sealed class OperationPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（操作编码、名称、描述）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 操作类型代码
    /// </summary>
    public OperationTypeCode? OperationTypeCode { get; set; }

    /// <summary>
    /// 操作分类
    /// </summary>
    public OperationCategory? Category { get; set; }

    /// <summary>
    /// HTTP 方法
    /// </summary>
    public HttpMethodType? HttpMethod { get; set; }

    /// <summary>
    /// 是否危险操作
    /// </summary>
    public bool? IsDangerous { get; set; }

    /// <summary>
    /// 是否需要审计
    /// </summary>
    public bool? IsRequireAudit { get; set; }

    /// <summary>
    /// 是否全局操作
    /// </summary>
    public bool? IsGlobal { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
