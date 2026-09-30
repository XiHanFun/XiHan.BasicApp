// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 可配置字段安全的实体登记表
/// </summary>
/// <remarks>
/// 字段安全规则按「实体 + 字段」生效：只有查询服务在返回前按某实体打码、按它门控排序和过滤，
/// 给这个实体配的规则才真正起作用。所以两边必须一一对应——登记了就要落地，落地了就要登记（有测试钉住）。
/// 各模块在自己的服务注册里登记本模块的实体，实体名取类名（如 <c>SysUser</c>）。
/// </remarks>
public sealed class FieldSecurityEntityOptions
{
    private readonly Dictionary<string, Type> _entities = new(StringComparer.Ordinal);

    /// <summary>
    /// 已登记实体（实体名 → 实体类型）
    /// </summary>
    public IReadOnlyDictionary<string, Type> Entities => _entities;

    /// <summary>
    /// 登记一个实体；重复登记同一类型无副作用，不同类型撞名直接报错。
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns>当前登记表，便于连写</returns>
    /// <exception cref="InvalidOperationException">不同模块的两个实体同名。</exception>
    public FieldSecurityEntityOptions Add<TEntity>()
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_entities.TryGetValue(type.Name, out var existing) && existing != type)
        {
            throw new InvalidOperationException($"字段安全实体名「{type.Name}」重复：{existing.FullName} 与 {type.FullName}。");
        }

        _entities[type.Name] = type;
        return this;
    }
}
