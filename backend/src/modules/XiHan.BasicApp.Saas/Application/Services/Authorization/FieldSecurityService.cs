// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 字段级安全（FLS）服务端落地
/// </summary>
/// <remarks>
/// Scoped：同一请求内按「用户 + 实体」缓存解析结果，查询门控与脱敏共用一次解析；
/// 导出等后台作业切换主体时键里带着用户，不会串用。
/// </remarks>
public sealed class FieldSecurityService : IFieldSecurityService
{
    private static readonly IReadOnlyDictionary<string, EffectiveFieldRule> EmptyRules =
        new Dictionary<string, EffectiveFieldRule>(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> WritablePropertyCache = new();

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> NavigablePropertyCache = new();

    private static readonly ConcurrentDictionary<PropertyInfo, string> DeclaredSourceCache = new();

    private readonly IFieldSecurityEntityCatalog _catalog;

    private readonly IFieldSecurityDtoCatalog _dtoCatalog;

    private readonly ICurrentUser _currentUser;

    private readonly IDepartmentRepository _departmentRepository;

    private readonly IFieldSecurityEntityReader _entityReader;

    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository;

    private readonly IRoleRepository _roleRepository;

    private readonly Dictionary<(long UserId, string EntityName), IReadOnlyDictionary<string, EffectiveFieldRule>> _resolved = [];

    private readonly Dictionary<long, Subject> _subjects = [];

    private readonly IUserDepartmentRepository _userDepartmentRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldSecurityService(
        IFieldSecurityEntityCatalog catalog,
        IFieldSecurityDtoCatalog dtoCatalog,
        IFieldLevelSecurityRepository fieldLevelSecurityRepository,
        IFieldSecurityEntityReader entityReader,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IUserDepartmentRepository userDepartmentRepository,
        IDepartmentRepository departmentRepository,
        ICurrentUser currentUser)
    {
        _catalog = catalog;
        _dtoCatalog = dtoCatalog;
        _fieldLevelSecurityRepository = fieldLevelSecurityRepository;
        _entityReader = entityReader;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _departmentRepository = departmentRepository;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, EffectiveFieldRule>> ResolveAsync(Type entityType, CancellationToken cancellationToken = default)
    {
        // 先认实体：未登记的实体在这里报错，不因「没登录」「没规则」被悄悄放过
        var entityName = _catalog.GetEntityName(entityType);
        if (!_currentUser.UserId.HasValue)
        {
            return EmptyRules;
        }

        var userId = _currentUser.UserId.Value;
        if (_resolved.TryGetValue((userId, entityName), out var cached))
        {
            return cached;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var rules = await _fieldLevelSecurityRepository.GetEnabledByEntityAsync(entityName, cancellationToken);
        var resolved = EmptyRules;
        if (rules.Count > 0)
        {
            var subject = await GetSubjectAsync(userId, cancellationToken);
            var applicable = rules.Where(rule => rule.TargetType switch
            {
                FieldSecurityTargetType.User => rule.TargetId == userId,
                FieldSecurityTargetType.Role => subject.RoleIds.Contains(rule.TargetId),
                FieldSecurityTargetType.Department => subject.DepartmentIds.Contains(rule.TargetId),
                _ => false
            });
            resolved = Merge(applicable);
        }

        _resolved[(userId, entityName)] = resolved;
        return resolved;
    }

    /// <inheritdoc />
    public async Task GuardQueryAsync(QueryConditions conditions, Type entityType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        var rules = await ResolveAsync(entityType, cancellationToken);
        var protectedFields = rules.Values
            .Where(rule => rule.IsReadProtected)
            .Select(rule => rule.FieldName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (protectedFields.Count == 0)
        {
            return;
        }

        // 排序在 SQL 层按真实值进行、展示却被脱敏，结果顺序会泄露原值；过滤与关键字搜索同理可逐步试探
        _ = conditions.Sorts.RemoveAll(sort => protectedFields.Contains(sort.Field));
        _ = conditions.Filters.RemoveAll(filter => protectedFields.Contains(filter.Field));
        _ = conditions.Keyword?.Fields.RemoveAll(protectedFields.Contains);
    }

    /// <inheritdoc />
    public async Task MaskAsync(object? response, CancellationToken cancellationToken = default)
    {
        // 未登录的请求没有规则可言；对象图照样不走，省掉反射
        if (response is null || !_currentUser.UserId.HasValue)
        {
            return;
        }

        await WalkAsync(response, new HashSet<object>(ReferenceEqualityComparer.Instance), cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnsureCreatableAsync<TInput>(Type entityType, TInput input, CancellationToken cancellationToken = default)
        where TInput : class, new()
    {
        ArgumentNullException.ThrowIfNull(input);
        var readOnly = FieldsOf<TInput>(await ResolveAsync(entityType, cancellationToken), rule => !rule.IsEditable);
        if (readOnly.Count == 0)
        {
            return;
        }

        var blank = new TInput();
        foreach (var (property, _) in readOnly)
        {
            var unset = property.GetValue(blank);
            if (!SameValue(property.GetValue(input), unset))
            {
                throw new InvalidOperationException($"字段「{DisplayNameOf(entityType, property.Name)}」当前用户无权填写。");
            }

            // 空串与 null 都算没填，统一回到入参默认值
            property.SetValue(input, unset);
        }
    }

    /// <inheritdoc />
    public async Task EnsureUpdatableAsync<TInput>(Type entityType, long id, TInput input, CancellationToken cancellationToken = default)
        where TInput : class
    {
        ArgumentNullException.ThrowIfNull(input);
        var guarded = FieldsOf<TInput>(await ResolveAsync(entityType, cancellationToken), rule => !rule.IsEditable || rule.IsReadProtected);
        if (guarded.Count == 0)
        {
            return;
        }

        // 记录不存在交给业务流程按原有语义报错，这里不代为判断
        var current = await _entityReader.FindAsync(entityType, id, cancellationToken);
        if (current is null)
        {
            return;
        }

        foreach (var (property, rule) in guarded)
        {
            var entityProperty = entityType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"实体 {entityType.Name} 没有字段 {property.Name}。");
            var original = entityProperty.GetValue(current);
            var incoming = property.GetValue(input);
            var unchanged = SameValue(incoming, original) || SameValue(incoming, MaskedValueOf(original, rule, property.PropertyType));
            if (!unchanged)
            {
                if (!rule.IsEditable)
                {
                    throw new InvalidOperationException($"字段「{DisplayNameOf(entityType, property.Name)}」当前用户无修改权限。");
                }

                // 只写字段填了新值：照常保存
                continue;
            }

            // 表单交回的是脱敏值或空值时还原成原值，业务流程照常整体保存也不会改掉它
            if (original is not null && !property.PropertyType.IsInstanceOfType(original))
            {
                throw new InvalidOperationException($"入参 {typeof(TInput).Name}.{property.Name} 与实体字段类型不一致，无法还原原值。");
            }

            property.SetValue(input, original);
        }
    }

    /// <summary>
    /// 同一字段多条规则合并：读取方式取最严，部分脱敏保留位数取最少，任一只读即只读
    /// </summary>
    private static IReadOnlyDictionary<string, EffectiveFieldRule> Merge(IEnumerable<SysFieldLevelSecurity> rules)
    {
        return rules
            .GroupBy(rule => rule.FieldName, StringComparer.Ordinal)
            .Select(group =>
            {
                var strategy = group.MaxBy(rule => Strictness(rule.MaskStrategy))!.MaskStrategy;
                var partials = group.Where(rule => rule.MaskStrategy == FieldMaskStrategy.PartialMask).ToArray();
                return new EffectiveFieldRule
                {
                    FieldName = group.Key,
                    MaskStrategy = strategy,
                    MaskKeepHead = strategy == FieldMaskStrategy.PartialMask ? partials.Min(rule => rule.MaskKeepHead ?? 0) : null,
                    MaskKeepTail = strategy == FieldMaskStrategy.PartialMask ? partials.Min(rule => rule.MaskKeepTail ?? 0) : null,
                    MaskReplacement = strategy == FieldMaskStrategy.Redact
                        ? group.Where(rule => rule.MaskStrategy == FieldMaskStrategy.Redact).MinBy(rule => rule.BasicId)!.MaskReplacement
                        : null,
                    IsEditable = group.All(rule => rule.IsEditable)
                };
            })
            .ToDictionary(rule => rule.FieldName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 严格程度：按泄露的信息量从多到少排
    /// </summary>
    private static int Strictness(FieldMaskStrategy strategy) => strategy switch
    {
        FieldMaskStrategy.None => 0,
        FieldMaskStrategy.PartialMask => 1,
        FieldMaskStrategy.Hash => 2,
        FieldMaskStrategy.FullMask => 3,
        FieldMaskStrategy.Redact => 4,
        FieldMaskStrategy.Hidden => 5,
        _ => throw new InvalidOperationException($"读取方式 {strategy} 无效。")
    };

    /// <summary>
    /// 沿对象图下行：集合逐个、字典取值、本应用与框架的类型逐个属性；遇到登记实体的 DTO 按其规则打码
    /// </summary>
    private async Task WalkAsync(object value, HashSet<object> visited, CancellationToken cancellationToken)
    {
        var type = value.GetType();
        if (IsLeaf(type) || !visited.Add(value))
        {
            return;
        }

        if (value is IDictionary dictionary)
        {
            foreach (var item in dictionary.Values)
            {
                if (item is not null)
                {
                    await WalkAsync(item, visited, cancellationToken);
                }
            }

            return;
        }

        if (value is IEnumerable sequence)
        {
            foreach (var item in sequence)
            {
                if (item is not null)
                {
                    await WalkAsync(item, visited, cancellationToken);
                }
            }

            return;
        }

        if (!IsOwnType(type))
        {
            return;
        }

        // 多来源 DTO 按实例说明来源实体；其余按类型从映射器认定
        var entityType = value is IFieldSecurityMultiSourceDto multiSource
            ? multiSource.FieldSecurityEntity
            : _dtoCatalog.EntityOf(type);
        if (entityType is not null)
        {
            var rules = await ResolveAsync(entityType, cancellationToken);
            if (rules.Count > 0)
            {
                MaskInstance(value, rules);
            }
        }

        foreach (var property in NavigablePropertiesOf(type))
        {
            if (property.GetValue(value) is { } child)
            {
                await WalkAsync(child, visited, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 叶子类型：标量、文本、二进制、流、JSON 节点等，不可能再装着 DTO
    /// </summary>
    private static bool IsLeaf(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Guid)
            || underlying == typeof(byte[])
            || underlying == typeof(Uri)
            || typeof(Stream).IsAssignableFrom(underlying)
            || typeof(Delegate).IsAssignableFrom(underlying)
            || typeof(Type).IsAssignableFrom(underlying)
            || underlying.Namespace?.StartsWith("System.Text.Json", StringComparison.Ordinal) == true;
    }

    /// <summary>
    /// 只进入本应用与框架程序集的类型（DTO、分页结果、响应信封）；第三方类型不下行
    /// </summary>
    private static bool IsOwnType(Type type) =>
        type.Assembly.GetName().Name?.StartsWith("XiHan.", StringComparison.Ordinal) == true;

    /// <summary>
    /// 可能装着 DTO 的属性：公开、可读、非索引器、声明类型不是叶子
    /// </summary>
    private static PropertyInfo[] NavigablePropertiesOf(Type type) =>
        NavigablePropertyCache.GetOrAdd(type, static target => target
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0 && !IsLeaf(property.PropertyType))
            .ToArray());

    /// <summary>
    /// 按规则给一个 DTO 打码：属性对应的实体字段取声明的来源（改名、多来源），否则与属性同名
    /// </summary>
    private static void MaskInstance(object item, IReadOnlyDictionary<string, EffectiveFieldRule> rules)
    {
        var multiSource = item as IFieldSecurityMultiSourceDto;
        foreach (var property in WritablePropertiesOf(item.GetType()))
        {
            var source = multiSource is null ? DeclaredSourceOf(property) : multiSource.FieldSecuritySourceOf(property.Name);
            if (source is not null && rules.TryGetValue(source, out var rule) && rule.IsReadProtected)
            {
                property.SetValue(item, MaskedValueOf(property.GetValue(item), rule, property.PropertyType));
            }
        }
    }

    private static string DeclaredSourceOf(PropertyInfo property) =>
        DeclaredSourceCache.GetOrAdd(property, static target =>
            target.GetCustomAttribute<FieldSecuritySourceAttribute>(inherit: true)?.EntityField ?? target.Name);

    /// <summary>
    /// 一个值在该规则下对外呈现的样子：文本按方式脱敏；非文本只能隐藏，呈现为类型默认值
    /// </summary>
    private static object? MaskedValueOf(object? value, EffectiveFieldRule rule, Type presentedType)
    {
        if (!rule.IsReadProtected)
        {
            return value;
        }

        if (presentedType == typeof(string) && rule.MaskStrategy != FieldMaskStrategy.Hidden)
        {
            return FieldMasker.Mask(value as string, rule);
        }

        // 非文本字段（或 DTO 把文本列换成了别的类型）不能按字符打码，一律按隐藏处理，宁严勿漏
        return presentedType.IsValueType && Nullable.GetUnderlyingType(presentedType) is null
            ? Activator.CreateInstance(presentedType)
            : null;
    }

    private static List<(PropertyInfo Property, EffectiveFieldRule Rule)> FieldsOf<TInput>(
        IReadOnlyDictionary<string, EffectiveFieldRule> rules,
        Func<EffectiveFieldRule, bool> predicate)
    {
        if (rules.Count == 0)
        {
            return [];
        }

        var result = new List<(PropertyInfo Property, EffectiveFieldRule Rule)>();
        foreach (var property in WritablePropertiesOf(typeof(TInput)))
        {
            if (rules.TryGetValue(property.Name, out var rule) && predicate(rule))
            {
                result.Add((property, rule));
            }
        }

        return result;
    }

    /// <summary>
    /// 值相等；文本的空串与 null 视为相同（表单清空与未填写一个意思）
    /// </summary>
    private static bool SameValue(object? left, object? right)
    {
        if (left is string or null && right is string or null)
        {
            return string.Equals(
                string.IsNullOrEmpty(left as string) ? null : (string?)left,
                string.IsNullOrEmpty(right as string) ? null : (string?)right,
                StringComparison.Ordinal);
        }

        return Equals(left, right);
    }

    private static PropertyInfo[] WritablePropertiesOf(Type type) =>
        WritablePropertyCache.GetOrAdd(type, static target => target
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property is { CanRead: true, CanWrite: true } && property.GetIndexParameters().Length == 0)
            .ToArray());

    private string DisplayNameOf(Type entityType, string fieldName) =>
        _catalog.Find(_catalog.GetEntityName(entityType))?.FindField(fieldName)?.DisplayName ?? fieldName;

    /// <summary>
    /// 当前用户的规则命中主体：生效且启用的角色，所在部门及其上级部门（启用的）
    /// </summary>
    private async Task<Subject> GetSubjectAsync(long userId, CancellationToken cancellationToken)
    {
        if (_subjects.TryGetValue(userId, out var subject))
        {
            return subject;
        }

        var roleIds = (await _userRoleRepository.GetValidByUserIdAsync(userId, DateTimeOffset.UtcNow, cancellationToken))
            .Select(userRole => userRole.RoleId)
            .Distinct()
            .ToArray();
        var enabledRoleIds = roleIds.Length == 0
            ? new HashSet<long>()
            : (await _roleRepository.GetEnabledByIdsAsync(roleIds, cancellationToken)).Select(role => role.BasicId).ToHashSet();

        var memberDepartmentIds = (await _userDepartmentRepository.GetValidByUserIdAsync(userId, cancellationToken))
            .Select(membership => membership.DepartmentId)
            .ToHashSet();
        var departmentIds = memberDepartmentIds.Count == 0
            ? new HashSet<long>()
            : await ExpandToEnabledAncestorsAsync(memberDepartmentIds, cancellationToken);

        subject = new Subject(enabledRoleIds, departmentIds);
        _subjects[userId] = subject;
        return subject;
    }

    /// <summary>
    /// 部门规则对下级部门同样生效：把所在部门沿上级链展开，只保留启用的部门
    /// </summary>
    private async Task<HashSet<long>> ExpandToEnabledAncestorsAsync(HashSet<long> memberDepartmentIds, CancellationToken cancellationToken)
    {
        var departments = (await _departmentRepository.GetListAsync(_ => true, cancellationToken))
            .ToDictionary(department => department.BasicId);
        var result = new HashSet<long>();
        var visited = new HashSet<long>();
        foreach (var departmentId in memberDepartmentIds)
        {
            long? cursor = departmentId;
            while (cursor is { } id && visited.Add(id) && departments.TryGetValue(id, out var department))
            {
                if (department.Status == EnableStatus.Enabled)
                {
                    _ = result.Add(id);
                }

                cursor = department.ParentId;
            }
        }

        return result;
    }

    private sealed record Subject(IReadOnlySet<long> RoleIds, IReadOnlySet<long> DepartmentIds);
}
