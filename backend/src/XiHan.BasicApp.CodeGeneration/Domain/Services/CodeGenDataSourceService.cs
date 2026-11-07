// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.DataSources;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Domain.Services;

/// <summary>
/// 数据源的规则与外部库连接
/// </summary>
/// <remarks>
/// 口令与连接串经 <see cref="ISecretProtector"/> 加解密，解不开即 <c>CODEGENERATION_UNAVAILABLE_001</c>，不当明文用（方案 C2）
/// 外部库经框架的运行时数据源连接（<see cref="DataSourceRegistry"/>，名字 <c>codegen-&lt;ID&gt;</c>，每个数据源一条健康项）：首次使用时登记，
/// 修改、启停与删除之后注销，下次使用按新定义重建。连接串优先用显式的，没有就按主机、端口、库名、账号拼出，附加参数追加在拼出的连接串之后
/// </remarks>
/// <param name="dataSources">数据源仓储</param>
/// <param name="tables">表配置仓储</param>
/// <param name="secrets">密钥保护</param>
/// <param name="registry">框架的运行时数据源</param>
/// <param name="outbound">出站地址策略</param>
/// <param name="outboundOptions">出站地址策略的配置（放行的主机）</param>
/// <param name="options">代码生成配置</param>
public sealed class CodeGenDataSourceService(IRepository<SysCodeGenDataSource, Guid> dataSources, IRepository<SysCodeGenTable, Guid> tables, ISecretProtector secrets, DataSourceRegistry registry, OutboundAddressPolicy outbound, OutboundAddressOptions outboundOptions, CodeGenerationOptions options)
{
    private static readonly string[] _hostKeys = ["Host", "Server", "Data Source", "DataSource", "Address", "Addr", "Network Address"];

    /// <summary>
    /// 口令的加密用途
    /// </summary>
    public const string PasswordPurpose = "XiHan.BasicApp.CodeGeneration.DataSourcePassword";

    /// <summary>
    /// 连接串的加密用途
    /// </summary>
    public const string ConnectionStringPurpose = "XiHan.BasicApp.CodeGeneration.DataSourceConnectionString";

    /// <summary>
    /// 运行时数据源名字的前缀
    /// </summary>
    public const string RegistryPrefix = "codegen-";

    /// <summary>
    /// 不存在
    /// </summary>
    /// <param name="id">数据源 ID</param>
    public static Error NotFound(Guid id)
        => Error.NotFound(CodeGenerationErrors.DataSourceNotFound, CodeGenerationMessages.DataSourceNotFound, MessageArguments.Of(("id", id)));

    /// <summary>
    /// 去掉首尾空白，空白即 <see langword="null"/>
    /// </summary>
    /// <param name="value">原值</param>
    public static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// 运行时数据源的名字
    /// </summary>
    /// <param name="id">数据源 ID</param>
    public static string RegistryName(Guid id) => RegistryPrefix + id.ToString("N", CultureInfo.InvariantCulture);

    /// <summary>
    /// SqlSugar 的数据库类型名
    /// </summary>
    /// <param name="type">数据库类型</param>
    public static string DatabaseTypeName(CodeGenDatabaseType type) => type switch
    {
        CodeGenDatabaseType.MySql => "MySql",
        CodeGenDatabaseType.SqlServer => "SqlServer",
        CodeGenDatabaseType.PostgreSql => "PostgreSQL",
        CodeGenDatabaseType.Oracle => "Oracle",
        CodeGenDatabaseType.Sqlite => "Sqlite",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    /// <summary>
    /// 加密一个口令或连接串
    /// </summary>
    /// <param name="purpose">用途</param>
    /// <param name="plaintext">明文，空白即不存</param>
    /// <returns>密文；空白为 <see langword="null"/></returns>
    public string? Protect(string purpose, string? plaintext)
        => string.IsNullOrWhiteSpace(plaintext) ? null : secrets.Protect(purpose, plaintext.Trim());

    /// <summary>
    /// 按 ID 取数据源
    /// </summary>
    /// <param name="id">数据源 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SysCodeGenDataSource>> FindAsync(Guid id, CancellationToken cancellationToken)
        => (await dataSources.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var source) ? source : NotFound(id);

    /// <summary>
    /// 名称是否已被别的数据源占用
    /// </summary>
    /// <param name="name">名称</param>
    /// <param name="except">排除的数据源（修改时是它自己）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<bool> NameTakenAsync(string name, Guid? except, CancellationToken cancellationToken)
    {
        var excluded = except ?? Guid.Empty;
        return await dataSources.CountAsync(EntityQuery.All<SysCodeGenDataSource>().Where(new Specification<SysCodeGenDataSource>(s => s.SourceName == name && s.Id != excluded)), cancellationToken).ConfigureAwait(false) > 0;
    }

    /// <summary>
    /// 是否有表配置引用这个数据源
    /// </summary>
    /// <param name="id">数据源 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<bool> InUseAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid? source = id;
        return await tables.CountAsync(EntityQuery.All<SysCodeGenTable>().Where(new Specification<SysCodeGenTable>(t => t.DataSourceId == source)), cancellationToken).ConfigureAwait(false) > 0;
    }

    /// <summary>
    /// 取消别的数据源的缺省标记
    /// </summary>
    /// <remarks>
    /// 缺省数据源最多一个
    /// </remarks>
    /// <param name="except">保留缺省的数据源</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result> ClearOtherDefaultsAsync(Guid except, CancellationToken cancellationToken)
    {
        var others = await dataSources.ListAsync(EntityQuery.All<SysCodeGenDataSource>().Where(new Specification<SysCodeGenDataSource>(s => s.IsDefault && s.Id != except)), cancellationToken).ConfigureAwait(false);
        foreach (var other in others)
        {
            other.IsDefault = false;
            var updated = await dataSources.UpdateAsync(other, cancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated.Error!;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 数据源的连接定义
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义；口令或连接串解不开、目标地址不被允许为失败</returns>
    public async Task<Result<DataSourceDefinition>> DefinitionOfAsync(SysCodeGenDataSource source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        var connection = ConnectionStringOf(source);
        if (!connection.TryGetValue(out var text))
        {
            return connection.Error!;
        }

        var target = await CheckTargetAsync(source, text, cancellationToken).ConfigureAwait(false);
        return target.IsSuccess ? new DataSourceDefinition(RegistryName(source.Id), DatabaseTypeName(source.DatabaseType), text) : target.Error!;
    }

    /// <summary>
    /// 数据源的目标过出站地址策略
    /// </summary>
    /// <remarks>
    /// 连接由驱动发起，用不上策略的连接回调，所以这里先按连接串里的主机解析后判（附加参数也能写主机，判的是拼好的连接串）
    /// 驱动会再解析一次，DNS 重绑定的窗口还在；这里防的是把内网地址直接填进来
    /// </remarks>
    private async Task<Result> CheckTargetAsync(SysCodeGenDataSource source, string connection, CancellationToken cancellationToken)
    {
        if (source.DatabaseType == CodeGenDatabaseType.Sqlite)
        {
            return options.AllowSqliteDataSource ? Result.Success() : Error.Validation(CodeGenerationErrors.SqliteSourceDisabled, CodeGenerationMessages.SqliteSourceDisabled);
        }

        var builder = new DbConnectionStringBuilder();
        try
        {
            builder.ConnectionString = connection;
        }
        catch (ArgumentException)
        {
            return Denied(source.Host);
        }

        var hosts = _hostKeys.Where(builder.ContainsKey).SelectMany(key => HostsIn(Convert.ToString(builder[key], CultureInfo.InvariantCulture))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (hosts.Count == 0)
        {
            return Denied(source.Host);
        }

        foreach (var host in hosts)
        {
            if (outboundOptions.AllowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            IPAddress[] resolved;
            try
            {
                resolved = IPAddress.TryParse(host, out var literal) ? [literal] : await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException)
            {
                continue;
            }

            if (resolved.Any(address => !outbound.IsAllowed(address)))
            {
                return Denied(host);
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 从连接串的主机项取主机名
    /// </summary>
    /// <remarks>
    /// 各驱动的写法：<c>a,b</c>（PostgreSQL 多主机）、<c>host,1433</c> 与 <c>tcp:host\实例</c>（SQL Server）、<c>host:1521/库</c>（Oracle）、<c>[::1]</c>
    /// </remarks>
    private static IEnumerable<string> HostsIn(string? value)
    {
        foreach (var part in (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.All(char.IsAsciiDigit))
            {
                continue;
            }

            var host = part.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase) ? part[4..] : part;
            if (IPAddress.TryParse(host, out _))
            {
                yield return host;
                continue;
            }

            if (host.StartsWith('['))
            {
                yield return host[1..host.IndexOf(']', StringComparison.Ordinal)];
                continue;
            }

            var end = host.IndexOfAny([':', '/', '\\']);
            yield return end < 0 ? host : host[..end];
        }
    }

    private static Error Denied(string host)
        => Error.Validation(CodeGenerationErrors.DataSourceTargetDenied, CodeGenerationMessages.DataSourceTargetDenied, MessageArguments.Of(("host", host)));

    /// <summary>
    /// 按定义连上数据源
    /// </summary>
    /// <remarks>
    /// 停用的数据源不能用（不回落到主库）；连接按定义在框架的运行时数据源里登记一次、之后复用
    /// </remarks>
    /// <param name="id">数据源 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<(SysCodeGenDataSource Source, IDataSource Connection)>> ConnectAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var source))
        {
            return found.Error!;
        }

        if (source.Status != CodeGenStatus.Active)
        {
            return Error.Conflict(CodeGenerationErrors.DataSourceDisabled, CodeGenerationMessages.DataSourceDisabled, MessageArguments.Of(("name", source.SourceName)));
        }

        var definition = await DefinitionOfAsync(source, cancellationToken).ConfigureAwait(false);
        if (!definition.TryGetValue(out var defined))
        {
            return definition.Error!;
        }

        var connected = await registry.GetOrAddAsync(defined, cancellationToken).ConfigureAwait(false);
        return connected.TryGetValue(out var connection) ? (source, connection) : connected.Error!;
    }

    /// <summary>
    /// 测试一次连接
    /// </summary>
    /// <remarks>
    /// 按当前定义临时建一个连接、开一次再关（不登记、不复用），停用的数据源也能测
    /// </remarks>
    /// <param name="definition">连接定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连得上为成功，否则是框架给的原因</returns>
    public async Task<Result> TestAsync(DataSourceDefinition definition, CancellationToken cancellationToken)
        => await registry.TestAsync(definition, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 注销数据源的连接
    /// </summary>
    /// <remarks>
    /// 修改、启停与删除之后调用，下次使用按新定义重建；没登记过的不报错
    /// </remarks>
    /// <param name="ids">数据源 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ForgetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count > 0)
        {
            _ = await registry.RemoveAsync([.. ids.Select(RegistryName)], cancellationToken).ConfigureAwait(false);
        }
    }

    private Result<string> ConnectionStringOf(SysCodeGenDataSource source)
    {
        if (source.ConnectionString is not null)
        {
            return secrets.TryUnprotect(ConnectionStringPurpose, source.ConnectionString, out var plain)
                ? plain
                : Unreadable(source);
        }

        var password = string.Empty;
        if (source.Password is not null && !secrets.TryUnprotect(PasswordPurpose, source.Password, out password))
        {
            return Unreadable(source);
        }

        var timeout = source.ConnectionTimeout <= 0 ? 30 : source.ConnectionTimeout;
        var built = source.DatabaseType switch
        {
            CodeGenDatabaseType.MySql => $"Server={source.Host};Port={source.Port};Database={source.DatabaseName};Uid={source.UserName};Pwd={password};Connection Timeout={timeout};",
            CodeGenDatabaseType.SqlServer => $"Server={source.Host},{source.Port};Database={source.DatabaseName};User Id={source.UserName};Password={password};Connect Timeout={timeout};TrustServerCertificate=true;",
            CodeGenDatabaseType.PostgreSql => $"Host={source.Host};Port={source.Port};Database={source.DatabaseName};Username={source.UserName};Password={password};Timeout={timeout};",
            CodeGenDatabaseType.Oracle => $"Data Source={source.Host}:{source.Port}/{source.DatabaseName};User Id={source.UserName};Password={password};Connection Timeout={timeout};",
            CodeGenDatabaseType.Sqlite => $"Data Source={source.DatabaseName};",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source.DatabaseType, null),
        };
        var extra = Trimmed(source.ExtraParams);
        return extra is null ? built : built + extra.TrimStart(';');
    }

    private static Error Unreadable(SysCodeGenDataSource source)
        => Error.Unavailable(CodeGenerationErrors.SecretUnreadable, CodeGenerationMessages.SecretUnreadable, MessageArguments.Of(("name", source.SourceName)));
}
