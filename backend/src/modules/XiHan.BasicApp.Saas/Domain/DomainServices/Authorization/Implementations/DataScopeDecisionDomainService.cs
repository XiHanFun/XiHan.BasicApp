#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataScopeDecisionDomainService
// Guid:8dd8f115-31b4-4bfc-b67f-07d66d3c2a71
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 数据范围裁决领域服务
/// </summary>
public sealed class DataScopeDecisionDomainService : IDataScopeDecisionDomainService
{
    /// <inheritdoc />
    public DataScopeDecision Decide(
        IEnumerable<DataScopeGrantSnapshot> grants,
        IEnumerable<long> userDepartmentIds,
        DateTimeOffset now)
    {
        var effectiveGrants = grants
            .Where(grant => grant.IsEnabled && grant.Period.IsActive(now))
            .ToList();

        if (effectiveGrants.Count == 0)
        {
            return DataScopeDecision.SelfOnly();
        }

        if (effectiveGrants.Any(grant => grant.Scope == DataPermissionScope.All))
        {
            return DataScopeDecision.All();
        }

        var directDepartmentIds = new HashSet<long>();
        var departmentAndChildrenIds = new HashSet<long>();
        var normalizedUserDepartmentIds = userDepartmentIds.Where(id => id > 0).Distinct().ToList();

        foreach (var grant in effectiveGrants)
        {
            switch (grant.Scope)
            {
                case DataPermissionScope.DepartmentOnly:
                    directDepartmentIds.UnionWith(normalizedUserDepartmentIds);
                    break;

                case DataPermissionScope.DepartmentAndChildren:
                    departmentAndChildrenIds.UnionWith(normalizedUserDepartmentIds);
                    break;

                case DataPermissionScope.Custom:
                    if (grant.IncludeChildren)
                    {
                        departmentAndChildrenIds.UnionWith(grant.DepartmentIds.Where(id => id > 0));
                    }
                    else
                    {
                        directDepartmentIds.UnionWith(grant.DepartmentIds.Where(id => id > 0));
                    }

                    break;
            }
        }

        if (directDepartmentIds.Count == 0 && departmentAndChildrenIds.Count == 0)
        {
            return DataScopeDecision.SelfOnly();
        }

        return DataScopeDecision.Restricted(
            directDepartmentIds.OrderBy(id => id).ToArray(),
            departmentAndChildrenIds.OrderBy(id => id).ToArray());
    }
}
