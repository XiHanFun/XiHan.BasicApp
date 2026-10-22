// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Authorization.Conditions;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 权限条件的属性目录、盘点与试算
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限条件")]
public sealed class PermissionConditionInsightQueryService : SaasApplicationService
{
    private readonly IConditionAttributeRegistry _attributes;
    private readonly IPermissionResourceRegistry _resources;
    private readonly PermissionConditionInventoryService _inventory;
    private readonly PermissionConditionTrialService _trial;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionConditionInsightQueryService(
        IConditionAttributeRegistry attributes,
        IPermissionResourceRegistry resources,
        PermissionConditionInventoryService inventory,
        PermissionConditionTrialService trial)
    {
        _attributes = attributes;
        _resources = resources;
        _inventory = inventory;
        _trial = trial;
    }

    /// <summary>
    /// 条件属性目录：登记的属性，加上该权限码登记的数据行属性
    /// </summary>
    /// <param name="permissionCode">所绑权限码；为空只列目录里的属性</param>
    /// <returns>属性列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Read)]
    public Task<List<PermissionConditionAttributeDto>> GetConditionAttributesAsync(string? permissionCode = null)
    {
        var rows = !string.IsNullOrWhiteSpace(permissionCode) && _resources.TryGet(permissionCode, out var registration)
            ? registration.Attributes
            : [];
        var items = _attributes.Attributes
            .Select(descriptor => ToDto(descriptor, isRowAttribute: false))
            .Concat(rows.Select(descriptor => ToDto(descriptor, isRowAttribute: true)))
            .ToList();
        return Task.FromResult(items);
    }

    /// <summary>
    /// 条件盘点：平台上下文扫全部租户，租户上下文只看本租户
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>盘点</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Trial)]
    public Task<PermissionConditionInventoryDto> GetPermissionConditionInventoryAsync(CancellationToken cancellationToken = default)
    {
        return _inventory.BuildAsync(DateTimeOffset.UtcNow, cancellationToken);
    }

    /// <summary>
    /// 单人试算
    /// </summary>
    /// <param name="input">试算输入</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>裁决路径</returns>
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Trial)]
    public Task<PermissionConditionTrialResultDto> GetPermissionConditionTrialAsync(PermissionConditionTrialInputDto input, CancellationToken cancellationToken = default)
    {
        return _trial.TrialAsync(input, cancellationToken);
    }

    /// <summary>
    /// 批量试算：按一条授权找出受影响的人各算一遍
    /// </summary>
    /// <param name="input">试算输入</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>每人的结论</returns>
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Trial)]
    public Task<PermissionConditionBatchTrialResultDto> GetPermissionConditionBatchTrialAsync(PermissionConditionBatchTrialInputDto input, CancellationToken cancellationToken = default)
    {
        return _trial.BatchTrialAsync(input, cancellationToken);
    }

    private static PermissionConditionAttributeDto ToDto(ConditionAttributeDescriptor descriptor, bool isRowAttribute)
    {
        return new PermissionConditionAttributeDto
        {
            Name = descriptor.Name,
            DisplayName = descriptor.DisplayName,
            Kind = descriptor.Kind,
            IsMultiValued = descriptor.IsMultiValued,
            IsFamily = descriptor.IsFamily,
            IsLegacy = string.Equals(descriptor.Name, SaasConditionAttributes.EnvironmentLocation, StringComparison.OrdinalIgnoreCase),
            IsRowAttribute = isRowAttribute,
            Aliases = [.. descriptor.Aliases ?? []],
            Operators = [.. ConditionOperatorRules.AllowedFor(descriptor).Select(@operator => (ConditionOperator)(int)@operator)]
        };
    }
}
