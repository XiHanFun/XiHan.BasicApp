// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// 导出执行上下文（执行器构造，传给 Provider）
/// </summary>
/// <remarks>
/// UserId/TenantId 为任务发起人/发起租户；执行器已据此重建 CurrentUser/CurrentTenant 上下文，
/// Provider 内调用既有 QueryService 时权限/数据范围/字段脱敏将原样生效。
/// </remarks>
public sealed class ExportContext
{
    /// <summary>
    /// 业务类型（= pageCode）
    /// </summary>
    public required string BusinessType { get; init; }

    /// <summary>
    /// 导出范围
    /// </summary>
    public ExportScope Scope { get; init; }

    /// <summary>
    /// 查询条件快照（资源自身分页查询 DTO 的 JSON）
    /// </summary>
    public string? QuerySnapshot { get; init; }

    /// <summary>
    /// 导出列（按顺序）
    /// </summary>
    public required IReadOnlyList<ExportColumnDto> Columns { get; init; }

    /// <summary>
    /// 发起人用户ID
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// 发起租户ID
    /// </summary>
    public long TenantId { get; init; }

    /// <summary>
    /// 总行数（Provider 首页查询后回填，供进度计算；未知为 null）
    /// </summary>
    public int? Total { get; set; }
}
