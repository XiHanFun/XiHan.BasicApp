// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统部门继承关系实体扩展
/// </summary>
public partial class SysDepartmentHierarchy : IValidatableObject
{
    /// <summary>
    /// 祖先部门（多条层级记录可指向同一部门，ManyToOne）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(AncestorId))]
    public virtual SysDepartment? Ancestor { get; set; }

    /// <summary>
    /// 后代部门（多条层级记录可指向同一部门，ManyToOne）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(DescendantId))]
    public virtual SysDepartment? Descendant { get; set; }

    /// <summary>
    /// 按部门的父子关系构建完整闭包表：每个部门到自身（深度 0）及到每个祖先各一行
    /// </summary>
    /// <remarks>部门增删改后整表重建、种子建部门都用这一份算法；父级缺失或成环直接报错。</remarks>
    /// <param name="departments">同一租户的全部有效部门</param>
    /// <returns>闭包行（未设置租户）</returns>
    public static IReadOnlyList<SysDepartmentHierarchy> BuildClosure(IReadOnlyList<SysDepartment> departments)
    {
        var departmentMap = departments.ToDictionary(department => department.BasicId);
        var rows = new List<SysDepartmentHierarchy>();

        foreach (var department in departments.OrderBy(department => department.ParentId ?? 0).ThenBy(department => department.Sort).ThenBy(department => department.DepartmentCode, StringComparer.Ordinal))
        {
            var chain = BuildAncestorChain(department, departmentMap);
            for (var depth = 0; depth < chain.Count; depth++)
            {
                var ancestor = chain[depth];
                var pathNodes = chain.Take(depth + 1).Reverse().ToArray();
                rows.Add(new SysDepartmentHierarchy
                {
                    AncestorId = ancestor.BasicId,
                    DescendantId = department.BasicId,
                    Depth = depth,
                    Path = string.Join("/", pathNodes.Select(node => node.BasicId)),
                    PathName = string.Join("/", pathNodes.Select(node => node.DepartmentName))
                });
            }
        }

        return rows;
    }

    /// <summary>
    /// 校验实体自身的业务规则
    /// </summary>
    /// <param name="validationContext">校验上下文</param>
    /// <returns>校验失败项集合，全部通过时为空集合</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Depth < 0)
        {
            yield return new ValidationResult("Depth 不能为负数。", [nameof(Depth)]);
        }

        if (Depth == 0 && AncestorId != DescendantId)
        {
            yield return new ValidationResult("Depth=0 时 AncestorId 必须等于 DescendantId（自环记录）。",
                [nameof(Depth), nameof(AncestorId), nameof(DescendantId)]);
        }

        if (Depth > 0 && AncestorId == DescendantId)
        {
            yield return new ValidationResult("Depth>0 时 AncestorId 不能等于 DescendantId。",
                [nameof(Depth), nameof(AncestorId), nameof(DescendantId)]);
        }
    }

    private static IReadOnlyList<SysDepartment> BuildAncestorChain(SysDepartment department, IReadOnlyDictionary<long, SysDepartment> departmentMap)
    {
        var chain = new List<SysDepartment>();
        var visited = new HashSet<long>();
        var cursor = department;

        while (true)
        {
            if (!visited.Add(cursor.BasicId))
            {
                throw new InvalidOperationException("部门层级存在环路，不能重建闭包表。");
            }

            chain.Add(cursor);
            if (!cursor.ParentId.HasValue)
            {
                return chain;
            }

            if (!departmentMap.TryGetValue(cursor.ParentId.Value, out var parent))
            {
                throw new InvalidOperationException("部门层级存在缺失父级，不能重建闭包表。");
            }

            cursor = parent;
        }
    }
}
