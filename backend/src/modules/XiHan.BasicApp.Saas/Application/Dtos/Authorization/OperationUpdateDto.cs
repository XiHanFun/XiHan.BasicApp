// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 操作定义更新 DTO
/// </summary>
public sealed class OperationUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 操作名称
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型代码
    /// </summary>
    public OperationTypeCode OperationTypeCode { get; set; } = OperationTypeCode.Read;

    /// <summary>
    /// 操作分类
    /// </summary>
    public OperationCategory Category { get; set; } = OperationCategory.Crud;

    /// <summary>
    /// HTTP 方法
    /// </summary>
    public HttpMethodType? HttpMethod { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 操作图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 操作颜色
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 是否危险操作
    /// </summary>
    public bool IsDangerous { get; set; }

    /// <summary>
    /// 是否需要审计
    /// </summary>
    public bool IsRequireAudit { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
