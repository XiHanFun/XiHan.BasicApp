// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 数据范围授权快照
/// </summary>
public sealed record DataScopeGrantSnapshot(
    long SourceId,
    AuthorizationGrantSource Source,
    DataPermissionScope Scope,
    IReadOnlyCollection<long> DepartmentIds,
    bool IncludeChildren,
    EffectivePeriod Period,
    bool IsEnabled = true);

/// <summary>
/// 数据范围裁决结果
/// </summary>
public sealed record DataScopeDecision(
    bool AllowsAllData,
    bool AllowsSelfData,
    IReadOnlyCollection<long> DepartmentIds,
    IReadOnlyCollection<long> DepartmentAndChildrenIds)
{
    /// <summary>
    /// 全部数据范围
    /// </summary>
    public static DataScopeDecision All()
    {
        return new DataScopeDecision(true, false, [], []);
    }

    /// <summary>
    /// 仅本人数据范围
    /// </summary>
    public static DataScopeDecision SelfOnly()
    {
        return new DataScopeDecision(false, true, [], []);
    }

    /// <summary>
    /// 受限部门数据范围
    /// </summary>
    public static DataScopeDecision Restricted(
        IReadOnlyCollection<long> departmentIds,
        IReadOnlyCollection<long> departmentAndChildrenIds)
    {
        return new DataScopeDecision(false, false, departmentIds, departmentAndChildrenIds);
    }
}
