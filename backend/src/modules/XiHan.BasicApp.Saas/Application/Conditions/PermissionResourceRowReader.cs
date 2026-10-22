// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 条件资源的数据行读取
/// </summary>
public interface IPermissionResourceRowReader
{
    /// <summary>
    /// 按主键读登记实体的数据行；读不到的主键不返回
    /// </summary>
    Task<IReadOnlyList<object>> ReadAsync(PermissionResourceRegistration registration, IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
}

/// <summary>
/// 经实体所在库的 SqlSugar 客户端读取，查询过滤（租户、软删）照常生效
/// </summary>
public sealed class SqlSugarPermissionResourceRowReader : IPermissionResourceRowReader
{
    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SqlSugarPermissionResourceRowReader(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<object>> ReadAsync(PermissionResourceRegistration registration, IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        return registration.LoadAsync(_clientResolver, ids, cancellationToken);
    }
}
