// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 权限码登记的条件资源：该权限的条件可以引用数据行属性 <c>resource.&lt;名&gt;</c>
/// </summary>
/// <remarks>
/// 鉴权发生在模型绑定之前，那一刻读不到行；行属性子句先记为「待定」，
/// 模型绑定之后由 <see cref="PermissionConditionRowFilter"/> 按 <see cref="KeySelector"/> 取出主键、读出数据行补算。
/// 实体的公开标量属性按 camelCase 映射，标了 <see cref="ExcludeFromConditionsAttribute"/> 的不映射；
/// 与目录里请求类属性（如 <c>resource.path</c>）同名的属性不映射。
/// </remarks>
public sealed class PermissionResourceRegistration
{
    private static readonly HashSet<string> ReservedNames = SaasConditionAttributes.All
        .Select(descriptor => descriptor.Name)
        .Concat(SaasConditionAttributes.All.SelectMany(descriptor => descriptor.Aliases ?? []))
        .Where(name => name.StartsWith("resource.", StringComparison.OrdinalIgnoreCase))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly IReadOnlyDictionary<string, PropertyInfo> _properties;
    private readonly Func<ISqlSugarClientResolver, IReadOnlyCollection<long>, CancellationToken, Task<IReadOnlyList<object>>> _loader;

    private PermissionResourceRegistration(
        string permissionCode,
        Type entityType,
        Func<IReadOnlyDictionary<string, object?>, IEnumerable<long>?> keySelector,
        IReadOnlyDictionary<string, PropertyInfo> properties,
        IReadOnlyList<ConditionAttributeDescriptor> attributes,
        Func<ISqlSugarClientResolver, IReadOnlyCollection<long>, CancellationToken, Task<IReadOnlyList<object>>> loader)
    {
        PermissionCode = permissionCode;
        EntityType = entityType;
        KeySelector = keySelector;
        _properties = properties;
        Attributes = attributes;
        _loader = loader;
    }

    /// <summary>
    /// 权限码
    /// </summary>
    public string PermissionCode { get; }

    /// <summary>
    /// 实体类型
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// 从动作参数（模型绑定之后）取主键；取不到返回空
    /// </summary>
    public Func<IReadOnlyDictionary<string, object?>, IEnumerable<long>?> KeySelector { get; }

    /// <summary>
    /// 行属性登记项
    /// </summary>
    public IReadOnlyList<ConditionAttributeDescriptor> Attributes { get; }

    /// <summary>
    /// 登记一个实体
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="permissionCode">权限码</param>
    /// <param name="keySelector">主键取法，见 <see cref="PermissionResourceKeys"/></param>
    /// <returns>登记项</returns>
    public static PermissionResourceRegistration Create<TEntity>(
        string permissionCode,
        Func<IReadOnlyDictionary<string, object?>, IEnumerable<long>?> keySelector)
        where TEntity : class, IEntityBase<long>, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        ArgumentNullException.ThrowIfNull(keySelector);

        var properties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        var attributes = new List<ConditionAttributeDescriptor>();
        foreach (var property in typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0 || property.IsDefined(typeof(ExcludeFromConditionsAttribute), inherit: true))
            {
                continue;
            }

            var kind = KindOf(property.PropertyType);
            var name = $"resource.{char.ToLowerInvariant(property.Name[0])}{property.Name[1..]}";
            if (kind is null || ReservedNames.Contains(name) || !properties.TryAdd(name, property))
            {
                continue;
            }

            attributes.Add(new ConditionAttributeDescriptor(name, kind.Value, DisplayName: property.Name));
        }

        return new PermissionResourceRegistration(
            permissionCode.Trim(),
            typeof(TEntity),
            keySelector,
            properties,
            attributes,
            static async (resolver, ids, cancellationToken) =>
            {
                var rows = await resolver.GetClientForEntity(typeof(TEntity))
                    .Queryable<TEntity>()
                    .In(ids.ToArray())
                    .ToListAsync(cancellationToken);
                return [.. rows.Cast<object>()];
            });
    }

    /// <summary>
    /// 该名称是否是登记的行属性
    /// </summary>
    public bool IsRowAttribute(string canonicalName)
    {
        return _properties.ContainsKey(canonicalName);
    }

    /// <summary>
    /// 从数据行取属性值
    /// </summary>
    public ConditionAttributeValue ReadValue(object row, string canonicalName)
    {
        ArgumentNullException.ThrowIfNull(row);
        return _properties.TryGetValue(canonicalName, out var property)
            ? ConditionAttributeValue.Of(property.GetValue(row))
            : ConditionAttributeValue.Missing;
    }

    /// <summary>
    /// 按主键读数据行（按当前租户的查询过滤）
    /// </summary>
    public Task<IReadOnlyList<object>> LoadAsync(ISqlSugarClientResolver resolver, IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return ids.Count == 0 ? Task.FromResult<IReadOnlyList<object>>([]) : _loader(resolver, ids, cancellationToken);
    }

    /// <summary>
    /// 行的主键
    /// </summary>
    public static long KeyOf(object row)
    {
        return row is IEntityBase<long> entity ? entity.BasicId : 0;
    }

    private static ConditionValueKind? KindOf(Type type)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (target == typeof(string) || target == typeof(Guid) || target.IsEnum)
        {
            return ConditionValueKind.String;
        }

        if (target == typeof(bool))
        {
            return ConditionValueKind.Boolean;
        }

        if (target == typeof(DateTime) || target == typeof(DateTimeOffset))
        {
            return ConditionValueKind.DateTime;
        }

        if (target == typeof(DateOnly))
        {
            return ConditionValueKind.Date;
        }

        if (target == typeof(TimeOnly) || target == typeof(TimeSpan))
        {
            return ConditionValueKind.TimeOfDay;
        }

        return Type.GetTypeCode(target) switch
        {
            TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
                or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal => ConditionValueKind.Number,
            _ => null
        };
    }
}

/// <summary>
/// 条件资源的主键取法
/// </summary>
public static class PermissionResourceKeys
{
    /// <summary>
    /// 取名为 <paramref name="argumentName"/> 的动作参数：<c>long</c>、<c>long?</c>、数字字符串或 <c>long</c> 的集合
    /// </summary>
    public static Func<IReadOnlyDictionary<string, object?>, IEnumerable<long>?> FromArgument(string argumentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argumentName);
        return arguments => arguments.TryGetValue(argumentName, out var value) ? ToKeys(value) : null;
    }

    /// <summary>
    /// 从名为 <paramref name="argumentName"/> 的 DTO 参数里取主键
    /// </summary>
    public static Func<IReadOnlyDictionary<string, object?>, IEnumerable<long>?> FromArgument<TArgument>(
        string argumentName,
        Func<TArgument, IEnumerable<long>?> select)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argumentName);
        ArgumentNullException.ThrowIfNull(select);
        return arguments => arguments.TryGetValue(argumentName, out var value) && value is TArgument argument ? select(argument) : null;
    }

    private static IEnumerable<long>? ToKeys(object? value)
    {
        return value switch
        {
            null => null,
            long key => [key],
            int key => [key],
            string text when long.TryParse(text, out var key) => [key],
            IEnumerable<long> keys => keys,
            _ => null
        };
    }
}
