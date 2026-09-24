// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 数据范围里的一个部门
/// </summary>
/// <param name="DepartmentId">部门主键</param>
/// <param name="IncludeChildren">是否含下级部门</param>
public sealed record DataScopeDepartmentItem(long DepartmentId, bool IncludeChildren);

/// <summary>
/// 设置数据范围的结果
/// </summary>
/// <param name="ScopeChanged">档位是否改变</param>
/// <param name="GrantedDepartmentIds">本次新授予或改了含下级的部门</param>
/// <param name="RevokedDepartmentIds">本次撤销的部门</param>
public sealed record DataScopeSetResult(
    bool ScopeChanged,
    IReadOnlyList<long> GrantedDepartmentIds,
    IReadOnlyList<long> RevokedDepartmentIds);

/// <summary>
/// 数据范围部门明细的规则
/// </summary>
/// <remarks>
/// 档位与部门明细一起提交、一起落地：只有自定义档位带部门，且至少一个；其它档位不带部门，已有的部门明细随之撤销。
/// </remarks>
public static class DataScopeDepartments
{
    /// <summary>
    /// 校验并归一部门明细：同一部门重复出现时以最后一条为准
    /// </summary>
    /// <param name="scope">目标档位（null 表示成员跟随角色）</param>
    /// <param name="departments">部门明细</param>
    /// <returns>部门主键 → 是否含下级</returns>
    public static IReadOnlyDictionary<long, bool> Normalize(DataPermissionScope? scope, IReadOnlyList<DataScopeDepartmentItem> departments)
    {
        ArgumentNullException.ThrowIfNull(departments);

        if (departments.Any(item => item.DepartmentId <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(departments), "部门主键必须大于 0。");
        }

        var map = departments
            .GroupBy(item => item.DepartmentId)
            .ToDictionary(group => group.Key, group => group.Last().IncludeChildren);

        if (scope == DataPermissionScope.Custom)
        {
            if (map.Count == 0)
            {
                throw new InvalidOperationException("自定义数据范围至少选择一个部门。");
            }
        }
        else if (map.Count > 0)
        {
            throw new InvalidOperationException("只有自定义数据范围才能指定部门。");
        }

        return map;
    }
}
