// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// SqlSugar 方式一的配置回调。
/// </summary>
/// <remarks>
/// 由 <c>SqlSugarPersistenceFeature(SqlSugarRuntime.Configure)</c> 在装配时对默认连接调用一次，带分片的调用再对 <c>shard</c> 连接调用一次。
/// 这里只负责库类型、连接串、操作人字段与 snake_case 命名；协议约定（主键、并发戳、UTC 时间、可空列）由框架随后施加。
/// </remarks>
public static class SqlSugarRuntime
{
    /// <summary>
    /// 数据库绑定名（<c>XiHan:Bindings:db</c>）。
    /// </summary>
    /// <remarks>
    /// 主值是连接串，属性 <c>provider</c> 是库类型（缺省 PostgreSQL）。
    /// </remarks>
    public const string BindingName = "db";

    /// <summary>
    /// 配置一个连接。
    /// </summary>
    /// <remarks>
    /// 租户分片的绑定由框架按租户属性 <c>ConnectionString</c> / <c>DatabaseProvider</c> 推出后交到这里。
    /// </remarks>
    /// <exception cref="FrameworkException">没有绑定即启动失败（数据库是硬要求，不做回退）。</exception>
    public static void Configure(Maybe<BindingValue> binding, ConnectionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (!binding.TryGetValue(out var value) || string.IsNullOrWhiteSpace(value.Value))
        {
            throw new FrameworkException(RuntimeErrors.DatabaseBindingMissing, $"Binding '{BindingName}' (XiHan:Bindings:{BindingName}) must provide the platform database connection string.");
        }

        value.Properties.TryGetValue(TenantShardKeys.ProviderProperty, out var provider);
        config.DbType = DatabaseProviders.Parse(provider);
        config.ConnectionString = value.Value;
        config.IsAutoCloseConnection = true;
        config.ConfigureExternalServices ??= new ConfigureExternalServices();
        config.ConfigureExternalServices.EntityNameService = ApplyTableName;
        config.ConfigureExternalServices.EntityService = ApplyColumnName;
        // 差异日志只对宿主按 DiffLogBuffer.IsLogged 开启的实体触发（SqlSugarPersistenceFeature.DiffLog）
        config.AopEvents = new AopEvents { DataExecuting = AuditorStamp.OnDataExecuting, OnDiffLogEvent = DiffLogBuffer.OnDiffLog };
    }

    private static void ApplyTableName(Type type, EntityInfo entity)
    {
        // 显式 [SugarTable] 的名字已由 SqlSugar 写进 DbTableName，与类型名不同即不改
        if (string.Equals(entity.DbTableName, type.Name, StringComparison.Ordinal))
        {
            entity.DbTableName = SnakeCaseNaming.Of(type.Name);
        }
    }

    private static void ApplyColumnName(System.Reflection.PropertyInfo property, EntityColumnInfo column)
    {
        if (string.Equals(column.DbColumnName, property.Name, StringComparison.Ordinal))
        {
            column.DbColumnName = SnakeCaseNaming.Of(property.Name);
        }

        // 建表（Development 的 InitTables）时字符串列缺省 varchar(255)，装不下契约允许的 256 字符：没写长度与类型的字符串列一律 512。
        // 写了 [LongText] / [StringLength] / [MaxLength] 的列留给框架约定：它在这个回调之后施加、只处理长度仍为 0 的列，这里先填了长度会把长文本建成 varchar(512)
        if (property.PropertyType == typeof(string) && column.Length == 0 && string.IsNullOrEmpty(column.DataType) && !HasLengthAnnotation(property))
        {
            column.Length = DefaultStringLength;
        }
    }

    private static bool HasLengthAnnotation(System.Reflection.PropertyInfo property) =>
        property.IsDefined(typeof(LongTextAttribute), inherit: true)
        || property.IsDefined(typeof(StringLengthAttribute), inherit: true)
        || property.IsDefined(typeof(MaxLengthAttribute), inherit: true);

    /// <summary>
    /// 没有显式长度的字符串列的建表长度。
    /// </summary>
    public const int DefaultStringLength = 512;
}
