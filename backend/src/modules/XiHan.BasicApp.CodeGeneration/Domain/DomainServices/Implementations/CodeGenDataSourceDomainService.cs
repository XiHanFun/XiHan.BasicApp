#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenDataSourceDomainService
// Guid:0355a32d-e532-417c-b8d6-bb0b0a51939e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.Diagnostics;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Utils.Security.Cryptography;

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成数据源领域服务实现
/// </summary>
/// <remarks>
/// 密钥处理：<see cref="SysCodeGenDataSource.Password"/> 与 <see cref="SysCodeGenDataSource.ConnectionString"/>
/// 入库前用 <see cref="AesHelper"/> 对称加密（框架内暂无标准加密服务，按手册 C 采用最简 helper）。
/// 读取出库（连接测试）时解密；详情 DTO 输出由 Mapper 脱敏，绝不回传明文密钥。
/// </remarks>
public sealed class CodeGenDataSourceDomainService : ICodeGenDataSourceDomainService
{
    /// <summary>
    /// 密钥加密口令（at-rest 加密，固定盐；非按记录加盐方案）
    /// </summary>
    private const string SecretPassword = "XiHan.CodeGen.DataSource.Secret.v1";

    private readonly ICodeGenDataSourceRepository _dataSourceRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenDataSourceDomainService(ICodeGenDataSourceRepository dataSourceRepository)
    {
        _dataSourceRepository = dataSourceRepository;
    }

    /// <inheritdoc />
    public async Task<CodeGenDataSourceCommandResult> CreateDataSourceAsync(CodeGenDataSourceCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureEnum(command.DatabaseType, nameof(command.DatabaseType));
        EnsureEnum(command.Status, nameof(command.Status));

        var sourceName = Required(command.SourceName, 100, nameof(command.SourceName), "数据源名称不能超过 100 个字符。");
        if (await _dataSourceRepository.ExistsNameAsync(sourceName, null, cancellationToken))
        {
            throw new InvalidOperationException("数据源名称已存在。");
        }

        var dataSource = new SysCodeGenDataSource
        {
            SourceName = sourceName,
            SourceDescription = Optional(command.SourceDescription, 500, nameof(command.SourceDescription), "数据源描述不能超过 500 个字符。"),
            DatabaseType = command.DatabaseType,
            Host = Required(command.Host, 200, nameof(command.Host), "主机地址不能超过 200 个字符。"),
            Port = command.Port,
            DatabaseName = Required(command.DatabaseName, 100, nameof(command.DatabaseName), "数据库名称不能超过 100 个字符。"),
            UserName = Required(command.UserName, 100, nameof(command.UserName), "用户名不能超过 100 个字符。"),
            Password = EncryptSecret(command.Password),
            ConnectionString = EncryptSecret(command.ConnectionString),
            ExtraParams = Optional(command.ExtraParams, 500, nameof(command.ExtraParams), "额外参数不能超过 500 个字符。"),
            ConnectionTimeout = command.ConnectionTimeout <= 0 ? 30 : command.ConnectionTimeout,
            IsDefault = command.IsDefault,
            Status = command.Status,
            Sort = command.Sort,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        if (command.IsDefault)
        {
            await ClearExistingDefaultAsync(null, cancellationToken);
        }

        return new CodeGenDataSourceCommandResult(await _dataSourceRepository.AddAsync(dataSource, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<CodeGenDataSourceCommandResult> UpdateDataSourceAsync(CodeGenDataSourceUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "数据源主键必须大于 0。");
        EnsureEnum(command.DatabaseType, nameof(command.DatabaseType));

        var sourceName = Required(command.SourceName, 100, nameof(command.SourceName), "数据源名称不能超过 100 个字符。");
        if (await _dataSourceRepository.ExistsNameAsync(sourceName, command.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("数据源名称已存在。");
        }

        var dataSource = await GetDataSourceOrThrowAsync(command.BasicId, cancellationToken);

        dataSource.SourceName = sourceName;
        dataSource.SourceDescription = Optional(command.SourceDescription, 500, nameof(command.SourceDescription), "数据源描述不能超过 500 个字符。");
        dataSource.DatabaseType = command.DatabaseType;
        dataSource.Host = Required(command.Host, 200, nameof(command.Host), "主机地址不能超过 200 个字符。");
        dataSource.Port = command.Port;
        dataSource.DatabaseName = Required(command.DatabaseName, 100, nameof(command.DatabaseName), "数据库名称不能超过 100 个字符。");
        dataSource.UserName = Required(command.UserName, 100, nameof(command.UserName), "用户名不能超过 100 个字符。");
        // 密钥：留空表示不修改（前端脱敏字段未回填），否则加密覆盖
        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            dataSource.Password = EncryptSecret(command.Password);
        }
        if (!string.IsNullOrWhiteSpace(command.ConnectionString))
        {
            dataSource.ConnectionString = EncryptSecret(command.ConnectionString);
        }
        dataSource.ExtraParams = Optional(command.ExtraParams, 500, nameof(command.ExtraParams), "额外参数不能超过 500 个字符。");
        dataSource.ConnectionTimeout = command.ConnectionTimeout <= 0 ? 30 : command.ConnectionTimeout;
        dataSource.Sort = command.Sort;
        dataSource.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        if (command.IsDefault && !dataSource.IsDefault)
        {
            await ClearExistingDefaultAsync(dataSource.BasicId, cancellationToken);
        }
        dataSource.IsDefault = command.IsDefault;

        return new CodeGenDataSourceCommandResult(await _dataSourceRepository.UpdateAsync(dataSource, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<CodeGenDataSourceCommandResult> UpdateDataSourceStatusAsync(CodeGenDataSourceStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "数据源主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));

        var dataSource = await GetDataSourceOrThrowAsync(command.BasicId, cancellationToken);
        dataSource.Status = command.Status;
        dataSource.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? dataSource.Remark;

        return new CodeGenDataSourceCommandResult(await _dataSourceRepository.UpdateAsync(dataSource, cancellationToken));
    }

    /// <inheritdoc />
    public async Task DeleteDataSourceAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataSource = await GetDataSourceOrThrowAsync(id, cancellationToken);
        if (!await _dataSourceRepository.DeleteAsync(dataSource, cancellationToken))
        {
            throw new InvalidOperationException("数据源删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<CodeGenDataSourceConnectionInfo> GetConnectionInfoAsync(long dataSourceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataSource = await GetDataSourceOrThrowAsync(dataSourceId, cancellationToken);
        if (dataSource.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException($"数据源「{dataSource.SourceName}」已停用，无法用于读取库表结构。");
        }

        return new CodeGenDataSourceConnectionInfo(
            dataSourceId.ToString(),
            MapDbType(dataSource.DatabaseType),
            BuildConnectionString(dataSource),
            dataSource.SourceName);
    }

    /// <inheritdoc />
    public async Task<CodeGenDataSourceConnectionTestResult> TestConnectionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataSource = await GetDataSourceOrThrowAsync(id, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        bool success;
        string message;
        try
        {
            var connectionString = BuildConnectionString(dataSource);
            var dbType = MapDbType(dataSource.DatabaseType);

            // 临时只读连接探测：独立 SqlSugarClient，不复用应用主库作用域，仅做开连接/取时间探测
            using var probeClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = dbType,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

            await Task.Run(() =>
            {
                probeClient.Ado.Open();
                probeClient.Ado.Close();
            }, cancellationToken);

            success = true;
            message = "连接成功。";
        }
        catch (Exception ex)
        {
            success = false;
            message = $"连接失败：{ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
        }

        // 回写最后测试结果
        dataSource.LastTestTime = DateTimeOffset.UtcNow;
        dataSource.LastTestResult = success;
        dataSource.LastTestMessage = message.Length > 500 ? message[..500] : message;
        await _dataSourceRepository.UpdateAsync(dataSource, cancellationToken);

        return new CodeGenDataSourceConnectionTestResult(success, message, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// 加密密钥（空白返回 null）
    /// </summary>
    private static string? EncryptSecret(string? plainText)
    {
        return string.IsNullOrWhiteSpace(plainText) ? null : AesHelper.Encrypt(plainText.Trim(), SecretPassword);
    }

    /// <summary>
    /// 解密密钥（空白返回 null；解密失败按明文兼容返回原值，避免历史明文数据导致连接测试不可用）
    /// </summary>
    private static string? DecryptSecret(string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            return null;
        }

        try
        {
            return AesHelper.Decrypt(cipherText, SecretPassword);
        }
        catch
        {
            return cipherText;
        }
    }

    /// <summary>
    /// 组装连接字符串：优先显式连接串（解密），否则按主机/端口/库/账号拼装
    /// </summary>
    private static string BuildConnectionString(SysCodeGenDataSource dataSource)
    {
        var explicitConnectionString = DecryptSecret(dataSource.ConnectionString);
        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            return explicitConnectionString;
        }

        var password = DecryptSecret(dataSource.Password) ?? string.Empty;
        var timeout = dataSource.ConnectionTimeout <= 0 ? 30 : dataSource.ConnectionTimeout;

        return dataSource.DatabaseType switch
        {
            DatabaseType.MySql =>
                $"Server={dataSource.Host};Port={dataSource.Port};Database={dataSource.DatabaseName};Uid={dataSource.UserName};Pwd={password};Connection Timeout={timeout};",
            DatabaseType.SqlServer =>
                $"Server={dataSource.Host},{dataSource.Port};Database={dataSource.DatabaseName};User Id={dataSource.UserName};Password={password};Connect Timeout={timeout};TrustServerCertificate=true;",
            DatabaseType.PostgreSql =>
                $"Host={dataSource.Host};Port={dataSource.Port};Database={dataSource.DatabaseName};Username={dataSource.UserName};Password={password};Timeout={timeout};",
            DatabaseType.Oracle =>
                $"Data Source={dataSource.Host}:{dataSource.Port}/{dataSource.DatabaseName};User Id={dataSource.UserName};Password={password};Connection Timeout={timeout};",
            DatabaseType.Sqlite =>
                $"Data Source={dataSource.DatabaseName};",
            _ => throw new NotSupportedException($"暂不支持的数据库类型：{dataSource.DatabaseType}。")
        };
    }

    /// <summary>
    /// 数据源数据库类型 → SqlSugar DbType
    /// </summary>
    private static DbType MapDbType(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.MySql => DbType.MySql,
            DatabaseType.SqlServer => DbType.SqlServer,
            DatabaseType.PostgreSql => DbType.PostgreSQL,
            DatabaseType.Oracle => DbType.Oracle,
            DatabaseType.Sqlite => DbType.Sqlite,
            _ => throw new NotSupportedException($"暂不支持的数据库类型：{databaseType}。")
        };
    }

    /// <summary>
    /// 置空现有默认源（保证唯一默认源）
    /// </summary>
    private async Task ClearExistingDefaultAsync(long? excludeId, CancellationToken cancellationToken)
    {
        var existingDefault = await _dataSourceRepository.GetDefaultAsync(cancellationToken);
        if (existingDefault is null)
        {
            return;
        }

        if (excludeId.HasValue && existingDefault.BasicId == excludeId.Value)
        {
            return;
        }

        existingDefault.IsDefault = false;
        await _dataSourceRepository.UpdateAsync(existingDefault, cancellationToken);
    }

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private async Task<SysCodeGenDataSource> GetDataSourceOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "数据源主键必须大于 0。");
        return await _dataSourceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("数据源不存在。");
    }
}
