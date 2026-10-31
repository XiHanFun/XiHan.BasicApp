// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 一个操作怎样用字段安全实体。
/// </summary>
public enum FieldSecurityUse
{
    /// <summary>
    /// 读取：查询门控，响应里的记录套掩码。
    /// </summary>
    Read,

    /// <summary>
    /// 新建：只读字段不能填，响应套掩码。
    /// </summary>
    Create,

    /// <summary>
    /// 修改：只读字段不能改、交回脱敏值即保留原值，响应套掩码。
    /// </summary>
    Update,
}

/// <summary>
/// 一个操作与字段安全实体的绑定。
/// </summary>
/// <remarks>
/// Access 的字段安全中间件按它套规则：请求与响应都按接口的 JSON 字段名处理，名字与实体登记的字段同名。
/// </remarks>
/// <param name="Entity">实体码。</param>
/// <param name="OperationId">操作 ID。</param>
/// <param name="Use">用法。</param>
/// <param name="Json">请求、响应所在包的序列化上下文。</param>
/// <param name="Path">响应里记录所在的属性；为空即响应本身或分页的 <c>items</c>。</param>
/// <param name="KeywordFields">关键字搜索会匹配的字段（其中有受保护的即拒绝关键字搜索）。</param>
/// <param name="Original">修改时取改动前的记录（未脱敏的响应对象，找不到为空）。</param>
public sealed record FieldSecurityBinding(
    string Entity,
    string OperationId,
    FieldSecurityUse Use,
    JsonSerializerContext Json,
    string? Path,
    IReadOnlyList<string> KeywordFields,
    Func<object, InvocationContext, ValueTask<object?>>? Original);

/// <summary>
/// 一个包的字段安全实体的登记。
/// </summary>
/// <remarks>
/// 实体的字段由响应类型的 JSON 元数据生成（与接口字段同名，可空性取自注解）；租户、并发戳、软删除等框架列与主键不进目录，
/// 嵌套对象与集合也不进（只登记标量）。字段显示名的文案键是给定前缀加 snake_case 的字段名，前端没有这个键时显示字段名。
/// </remarks>
public sealed class FieldSecurityBuilder
{
    private static readonly HashSet<string> _frameworkFields = new(StringComparer.Ordinal)
    {
        "id", "tenantId", "ownerTenantId", "concurrencyStamp", "isDeleted", "deletedAt", "deletedBy",
    };

    private readonly string _package;
    private readonly JsonSerializerContext _json;
    private readonly List<FieldSecurityEntityBuilder> _entities = [];

    internal FieldSecurityBuilder(string package, JsonSerializerContext json)
    {
        _package = package;
        _json = json;
    }

    /// <summary>
    /// 登记一个实体。
    /// </summary>
    /// <typeparam name="TResponse">实体的响应类型（字段由它生成）。</typeparam>
    /// <param name="code">实体码（<c>&lt;包&gt;.&lt;资源&gt;</c>）。</param>
    /// <param name="titleKey">实体显示名的文案键。</param>
    /// <param name="fieldTitlePrefix">字段显示名文案键的前缀（如 <c>tenancy.tenants.fields.</c>）。</param>
    /// <returns>实体的构建器。</returns>
    public FieldSecurityEntityBuilder Entity<TResponse>(string code, string titleKey, string fieldTitlePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);
        ArgumentNullException.ThrowIfNull(fieldTitlePrefix);
        var entity = new FieldSecurityEntityBuilder(this, code, titleKey, fieldTitlePrefix, TypeInfoOf(typeof(TResponse)));
        _entities.Add(entity);
        return entity;
    }

    internal JsonSerializerContext Json => _json;

    internal JsonTypeInfo TypeInfoOf(Type type)
        => _json.GetTypeInfo(type) ?? throw new InvalidOperationException($"{type.Name} is not in {_json.GetType().Name}; add it to the package's JSON context.");

    internal void Register(IServiceCollection services)
    {
        services.AddFieldSecurityCatalog(_package, [.. _entities.Select(static e => e.Describe())]);
        foreach (var binding in _entities.SelectMany(static e => e.Bindings))
        {
            services.AddSingleton(binding);
        }
    }

    internal static bool IsFrameworkField(string name) => _frameworkFields.Contains(name);
}

/// <summary>
/// 一个字段安全实体的登记。
/// </summary>
/// <remarks>
/// 声明哪些操作读、建、改它；规则的套用都在 Access 的字段安全中间件里。
/// </remarks>
public sealed class FieldSecurityEntityBuilder
{
    private readonly FieldSecurityBuilder _owner;
    private readonly string _code;
    private readonly string _titleKey;
    private readonly string _fieldTitlePrefix;
    private readonly JsonTypeInfo _response;
    private readonly HashSet<string> _excluded = new(StringComparer.Ordinal);
    private readonly HashSet<string> _included = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _titles = new(StringComparer.Ordinal);
    private readonly List<string> _keyword = [];
    private readonly List<(string Operation, FieldSecurityUse Use, string? Path, Func<object, InvocationContext, ValueTask<object?>>? Original)> _operations = [];

    internal FieldSecurityEntityBuilder(FieldSecurityBuilder owner, string code, string titleKey, string fieldTitlePrefix, JsonTypeInfo response)
    {
        _owner = owner;
        _code = code;
        _titleKey = titleKey;
        _fieldTitlePrefix = fieldTitlePrefix;
        _response = response;
    }

    /// <summary>
    /// 读取这个实体的操作（单条或分页）。
    /// </summary>
    /// <param name="operations">操作 ID。</param>
    /// <returns>本构建器。</returns>
    public FieldSecurityEntityBuilder Reads(params string[] operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        _operations.AddRange(operations.Select(static o => (o, FieldSecurityUse.Read, (string?)null, (Func<object, InvocationContext, ValueTask<object?>>?)null)));
        return this;
    }

    /// <summary>
    /// 读取这个实体的操作（记录在响应的某个属性里）。
    /// </summary>
    /// <param name="operation">操作 ID。</param>
    /// <param name="path">响应里记录所在的属性（JSON 名，单条或数组）。</param>
    /// <returns>本构建器。</returns>
    public FieldSecurityEntityBuilder ReadsAt(string operation, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _operations.Add((operation, FieldSecurityUse.Read, path, null));
        return this;
    }

    /// <summary>
    /// 新建这个实体的操作。
    /// </summary>
    /// <param name="operations">操作 ID。</param>
    /// <returns>本构建器。</returns>
    public FieldSecurityEntityBuilder Creates(params string[] operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        _operations.AddRange(operations.Select(static o => (o, FieldSecurityUse.Create, (string?)null, (Func<object, InvocationContext, ValueTask<object?>>?)null)));
        return this;
    }

    /// <summary>
    /// 新建这个实体的操作（响应里记录在某个属性里）。
    /// </summary>
    /// <param name="operation">操作 ID。</param>
    /// <param name="path">响应里记录所在的属性（JSON 名）。</param>
    /// <returns>本构建器。</returns>
    public FieldSecurityEntityBuilder CreatesAt(string operation, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _operations.Add((operation, FieldSecurityUse.Create, path, null));
        return this;
    }

    /// <summary>
    /// 修改这个实体的操作。
    /// </summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <param name="operation">操作 ID。</param>
    /// <param name="original">按请求取改动前的记录（未脱敏的、与实体同形的响应对象，找不到为空，交给操作自己报找不到）。</param>
    /// <param name="path">响应里记录所在的属性（JSON 名）；为空即响应本身。</param>
    /// <returns>本构建器。</returns>
    public FieldSecurityEntityBuilder Updates<TRequest>(string operation, Func<TRequest, InvocationContext, ValueTask<object?>> original, string? path = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(original);
        _operations.Add((operation, FieldSecurityUse.Update, path, (request, context) => original((TRequest)request, context)));
        return this;
    }

    /// <summary>
    /// 关键字搜索会匹配的字段。
    /// </summary>
    /// <param name="fields">字段名。</param>
    /// <returns>本构建器。</returns>
    public FieldSecurityEntityBuilder Keyword(params string[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        _keyword.AddRange(fields);
        return this;
    }

    /// <summary>
    /// 不进目录的字段。
    /// </summary>
    /// <param name="fields">字段名。</param>
    /// <returns>本构建器。</returns>
    public FieldSecurityEntityBuilder Except(params string[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        _excluded.UnionWith(fields);
        return this;
    }

    /// <summary>
    /// 与框架列同名、在这个实体里却是业务字段的（如租户的 <c>tenantId</c> 是租户代码）。
    /// </summary>
    /// <param name="fields">字段名。</param>
    /// <returns>本构建器。</returns>
    public FieldSecurityEntityBuilder Include(params string[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        _included.UnionWith(fields);
        return this;
    }

    /// <summary>
    /// 单个字段的显示名文案键（不按前缀拼）。
    /// </summary>
    /// <param name="field">字段名。</param>
    /// <param name="titleKey">文案键。</param>
    /// <returns>本构建器。</returns>
    public FieldSecurityEntityBuilder Title(string field, string titleKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);
        _titles[field] = titleKey;
        return this;
    }

    /// <summary>
    /// 接着登记同一包的下一个实体。
    /// </summary>
    /// <typeparam name="TResponse">实体的响应类型。</typeparam>
    /// <param name="code">实体码。</param>
    /// <param name="titleKey">实体显示名的文案键。</param>
    /// <param name="fieldTitlePrefix">字段显示名文案键的前缀。</param>
    /// <returns>下一个实体的构建器。</returns>
    public FieldSecurityEntityBuilder Entity<TResponse>(string code, string titleKey, string fieldTitlePrefix) => _owner.Entity<TResponse>(code, titleKey, fieldTitlePrefix);

    internal IEnumerable<FieldSecurityBinding> Bindings
        => _operations.Select(o => new FieldSecurityBinding(_code, o.Operation, o.Use, _owner.Json, o.Path, _keyword, o.Original));

    internal FieldSecurityEntityDescriptor Describe()
    {
        var fields = _response.Properties
            .Where(p => (_included.Contains(p.Name) || !FieldSecurityBuilder.IsFrameworkField(p.Name)) && !_excluded.Contains(p.Name) && IsScalar(p.PropertyType))
            .Select(p => new FieldSecurityFieldDescriptor(
                p.Name,
                _titles.GetValueOrDefault(p.Name) ?? _fieldTitlePrefix + Snake(p.Name),
                p.PropertyType == typeof(string),
                Nullable.GetUnderlyingType(p.PropertyType) is not null || (!p.PropertyType.IsValueType && p.IsGetNullable)))
            .ToList();
        foreach (var field in _keyword.Concat(_titles.Keys).Concat(_excluded).Concat(_included))
        {
            if (_response.Properties.All(p => !string.Equals(p.Name, field, StringComparison.Ordinal)))
            {
                throw new ArgumentException($"Field '{field}' is not a property of {_response.Type.Name} ('{_code}').");
            }
        }

        return new FieldSecurityEntityDescriptor(_code, _titleKey, fields);
    }

    private static bool IsScalar(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(DateTimeOffset)
            || t == typeof(DateOnly) || t == typeof(TimeOnly) || t == typeof(TimeSpan) || t == typeof(Guid);
    }

    private static string Snake(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        foreach (var c in name)
        {
            if (char.IsUpper(c))
            {
                if (builder.Length > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
                continue;
            }

            builder.Append(c);
        }

        return builder.ToString();
    }
}

/// <summary>
/// 字段安全实体的声明式登记。
/// </summary>
public static class FieldSecurityRegistrationExtensions
{
    /// <summary>
    /// 登记一个包的字段安全实体与操作绑定。
    /// </summary>
    /// <remarks>
    /// 实体进字段安全的目录（<see cref="FieldSecurityCatalogSection"/>），操作绑定（<see cref="FieldSecurityBinding"/>）交给 Access 的中间件。
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <param name="package">包名（实体码的首段）。</param>
    /// <param name="json">该包的序列化上下文（响应、请求类型都在里面）。</param>
    /// <param name="build">登记实体。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFieldSecurity(this IServiceCollection services, string package, JsonSerializerContext json, Action<FieldSecurityBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(package);
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(build);
        var builder = new FieldSecurityBuilder(package, json);
        build(builder);
        builder.Register(services);
        return services;
    }
}
