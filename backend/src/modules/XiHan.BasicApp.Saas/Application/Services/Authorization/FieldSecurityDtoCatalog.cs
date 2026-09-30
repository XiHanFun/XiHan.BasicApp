// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Domain.DomainServices;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 返回 DTO 属于哪个字段安全实体
/// </summary>
public interface IFieldSecurityDtoCatalog
{
    /// <summary>
    /// DTO 对应的已登记实体；不是任何登记实体的 DTO 时返回空
    /// </summary>
    Type? EntityOf(Type dtoType);

    /// <summary>
    /// 全部「DTO → 实体」对应关系
    /// </summary>
    IReadOnlyDictionary<Type, Type> Mappings { get; }
}

/// <summary>
/// 从应用层映射器认出 DTO 属于哪个实体：<c>public static XxxDto ToXxx(SysEntity entity, …)</c> 即认定 XxxDto 属于 SysEntity
/// </summary>
/// <remarks>
/// 响应打码在输出边界统一做，不逐个接口接线，所以要能从返回对象的类型反查实体。约定是 DTO 经映射器从实体生成，
/// 映射器的首个参数就是来源实体。同一个 DTO 由两个登记实体映射出来时无法判断按谁的规则打码，构建时直接报错。
/// </remarks>
public sealed class FieldSecurityDtoCatalog : IFieldSecurityDtoCatalog
{
    private const string AssemblyPrefix = "XiHan.BasicApp";

    private readonly Lazy<IReadOnlyDictionary<Type, Type>> _mappings;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldSecurityDtoCatalog(IOptions<FieldSecurityEntityOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _mappings = new Lazy<IReadOnlyDictionary<Type, Type>>(() => Build(options.Value.Entities.Values.ToHashSet()));
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Type, Type> Mappings => _mappings.Value;

    /// <inheritdoc />
    public Type? EntityOf(Type dtoType)
    {
        ArgumentNullException.ThrowIfNull(dtoType);
        return _mappings.Value.GetValueOrDefault(dtoType);
    }

    /// <summary>
    /// 扫描已加载的应用程序集里的映射器
    /// </summary>
    internal static IReadOnlyDictionary<Type, Type> Build(IReadOnlySet<Type> entities)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Concat(entities.Select(entity => entity.Assembly))
            .Where(assembly => !assembly.IsDynamic && (assembly.GetName().Name?.StartsWith(AssemblyPrefix, StringComparison.Ordinal) ?? false))
            .Distinct();

        var mappings = new Dictionary<Type, Type>();
        var conflicts = new List<string>();
        foreach (var mapper in assemblies.SelectMany(assembly => assembly.GetTypes()).Where(IsMapper))
        {
            foreach (var method in mapper.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0 || !entities.Contains(parameters[0].ParameterType) || !IsDto(method.ReturnType))
                {
                    continue;
                }

                var entity = parameters[0].ParameterType;
                if (mappings.TryGetValue(method.ReturnType, out var existing) && existing != entity)
                {
                    conflicts.Add($"{method.ReturnType.Name}：{existing.Name} 与 {entity.Name}");
                    continue;
                }

                mappings[method.ReturnType] = entity;
            }
        }

        return conflicts.Count == 0
            ? mappings
            : throw new InvalidOperationException($"以下 DTO 由多个字段安全实体映射而来，无法判断按谁的规则打码：{string.Join("；", conflicts.Distinct())}");
    }

    private static bool IsMapper(Type type) =>
        type is { IsClass: true, IsAbstract: true, IsSealed: true } && type.Name.EndsWith("Mapper", StringComparison.Ordinal);

    /// <summary>
    /// 多来源 DTO 按实例说明来源，不按类型登记
    /// </summary>
    private static bool IsDto(Type type) =>
        type is { IsClass: true, IsGenericType: false }
        && type != typeof(string)
        && type.Name.EndsWith("Dto", StringComparison.Ordinal)
        && !typeof(IFieldSecurityMultiSourceDto).IsAssignableFrom(type);
}
