// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限中心详情聚合 DTO
/// </summary>
public sealed class PermissionCenterDetailDto
{
    /// <summary>
    /// 权限基础详情
    /// </summary>
    public PermissionDetailDto Permission { get; set; } = new();

    /// <summary>
    /// 绑定资源
    /// </summary>
    public ResourceDetailDto? Resource { get; set; }

    /// <summary>
    /// 绑定操作
    /// </summary>
    public OperationDetailDto? Operation { get; set; }

    /// <summary>
    /// ABAC 条件规则
    /// </summary>
    public List<PermissionConditionListItemDto> Conditions { get; set; } = [];

    /// <summary>
    /// 权限委托记录
    /// </summary>
    public List<PermissionDelegationListItemDto> Delegations { get; set; } = [];

    /// <summary>
    /// 权限申请记录
    /// </summary>
    public List<PermissionRequestListItemDto> Requests { get; set; } = [];

    /// <summary>
    /// 字段级安全策略
    /// </summary>
    public List<FieldLevelSecurityListItemDto> FieldSecurities { get; set; } = [];

    /// <summary>
    /// 权限变更历史
    /// </summary>
    public List<PermissionChangeLogListItemDto> ChangeLogs { get; set; } = [];

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTimeOffset GeneratedTime { get; set; }
}
