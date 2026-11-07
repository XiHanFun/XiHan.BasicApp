// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenDatasources;

/// <summary>
/// 数据源的列表项
/// </summary>
/// <remarks>
/// 不带账号、口令与连接串（照 4.x 的列表项）
/// </remarks>
/// <param name="Id">主键</param>
/// <param name="SourceName">名称</param>
/// <param name="SourceDescription">描述</param>
/// <param name="DatabaseType">数据库类型</param>
/// <param name="Host">主机</param>
/// <param name="Port">端口</param>
/// <param name="DatabaseName">库名</param>
/// <param name="IsDefault">是否缺省数据源</param>
/// <param name="LastTestAt">最近一次测试连接的时间（UTC）</param>
/// <param name="LastTestResult">最近一次测试连接是否成功</param>
/// <param name="LastTestMessage">最近一次测试连接的结果说明</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
/// <param name="ConcurrencyStamp">并发戳：修改与启停时原样交回</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record CodegenDatasourceResponse(
    Guid Id,
    string SourceName,
    string? SourceDescription,
    CodeGenDatabaseType DatabaseType,
    string Host,
    int Port,
    string DatabaseName,
    bool IsDefault,
    DateTimeOffset? LastTestAt,
    bool LastTestResult,
    string? LastTestMessage,
    CodeGenStatus Status,
    int Order,
    string? Remark,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="source">数据源</param>
    public static CodegenDatasourceResponse From(SysCodeGenDataSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new CodegenDatasourceResponse(
            source.Id,
            source.SourceName,
            source.SourceDescription,
            source.DatabaseType,
            source.Host,
            source.Port,
            source.DatabaseName,
            source.IsDefault,
            source.LastTestAt,
            source.LastTestResult,
            source.LastTestMessage,
            source.Status,
            source.Order,
            source.Remark,
            source.ConcurrencyStamp,
            source.CreatedAt,
            source.ModifiedAt);
    }
}

/// <summary>
/// 数据源的详情
/// </summary>
/// <remarks>
/// 口令与连接串不回显：只说有没有显式连接串（4.x 回 <c>******</c> 占位，编辑后原样提交会把占位当连接串存进去，按缺陷修，方案 C2）
/// </remarks>
/// <param name="Id">主键</param>
/// <param name="SourceName">名称</param>
/// <param name="SourceDescription">描述</param>
/// <param name="DatabaseType">数据库类型</param>
/// <param name="Host">主机</param>
/// <param name="Port">端口</param>
/// <param name="DatabaseName">库名</param>
/// <param name="UserName">账号</param>
/// <param name="HasConnectionString">是否设了显式连接串</param>
/// <param name="ExtraParams">拼连接串时附加的参数</param>
/// <param name="ConnectionTimeout">连接超时（秒）</param>
/// <param name="IsDefault">是否缺省数据源</param>
/// <param name="LastTestAt">最近一次测试连接的时间（UTC）</param>
/// <param name="LastTestResult">最近一次测试连接是否成功</param>
/// <param name="LastTestMessage">最近一次测试连接的结果说明</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
/// <param name="ConcurrencyStamp">并发戳：修改与启停时原样交回</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="CreatedBy">创建人</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
/// <param name="ModifiedBy">修改人</param>
public sealed record CodegenDatasourceDetailResponse(
    Guid Id,
    string SourceName,
    string? SourceDescription,
    CodeGenDatabaseType DatabaseType,
    string Host,
    int Port,
    string DatabaseName,
    string UserName,
    bool HasConnectionString,
    string? ExtraParams,
    int ConnectionTimeout,
    bool IsDefault,
    DateTimeOffset? LastTestAt,
    bool LastTestResult,
    string? LastTestMessage,
    CodeGenStatus Status,
    int Order,
    string? Remark,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? ModifiedAt,
    string? ModifiedBy)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="source">数据源</param>
    public static CodegenDatasourceDetailResponse From(SysCodeGenDataSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new CodegenDatasourceDetailResponse(
            source.Id,
            source.SourceName,
            source.SourceDescription,
            source.DatabaseType,
            source.Host,
            source.Port,
            source.DatabaseName,
            source.UserName,
            source.ConnectionString is not null,
            source.ExtraParams,
            source.ConnectionTimeout,
            source.IsDefault,
            source.LastTestAt,
            source.LastTestResult,
            source.LastTestMessage,
            source.Status,
            source.Order,
            source.Remark,
            source.ConcurrencyStamp,
            source.CreatedAt,
            source.CreatedBy,
            source.ModifiedAt,
            source.ModifiedBy);
    }
}

/// <summary>
/// 数据源下拉选项
/// </summary>
/// <param name="Items">全部数据源，按排序号与名称</param>
public sealed record CodegenDatasourceOptionsResponse(IReadOnlyList<CodegenDatasourceResponse> Items);

/// <summary>
/// 测试连接的结果
/// </summary>
/// <param name="Success">是否连得上</param>
/// <param name="Message">结果说明（按调用方的区域）</param>
/// <param name="ElapsedMilliseconds">耗时（毫秒）</param>
public sealed record CodegenDatasourceTestResponse(bool Success, string? Message, long ElapsedMilliseconds);

/// <summary>
/// <c>codegen-datasources.get</c> 与 <c>test-connection</c> 的请求
/// </summary>
/// <param name="Id">数据源 ID</param>
public sealed record CodegenDatasourceIdRequest(Guid Id);

/// <summary>
/// <c>codegen-datasources.options</c> 的请求
/// </summary>
public sealed record ListCodegenDatasourceOptionsRequest;

/// <summary>
/// <c>codegen-datasources.create</c> 的请求
/// </summary>
/// <remarks>
/// 口令与显式连接串加密存储；连接超时为 0 即 30 秒（照 4.x）。设为缺省时其余数据源取消缺省
/// </remarks>
/// <param name="SourceName">名称（唯一）</param>
/// <param name="DatabaseType">数据库类型</param>
/// <param name="Host">主机</param>
/// <param name="Port">端口</param>
/// <param name="DatabaseName">库名（SQLite 是库文件的路径）</param>
/// <param name="UserName">账号</param>
/// <param name="Password">口令</param>
/// <param name="ConnectionString">显式连接串，给了即优先用它</param>
/// <param name="SourceDescription">描述</param>
/// <param name="ExtraParams">拼连接串时附加的参数</param>
/// <param name="ConnectionTimeout">连接超时（秒）</param>
/// <param name="IsDefault">是否缺省数据源</param>
/// <param name="Status">初始状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record CreateCodegenDatasourceRequest(
    [Required, StringLength(100, MinimumLength = 1)] string SourceName,
    CodeGenDatabaseType DatabaseType,
    [Required, StringLength(200, MinimumLength = 1)] string Host,
    [Range(0, 65535)] int Port,
    [Required, StringLength(200, MinimumLength = 1)] string DatabaseName,
    [Required, StringLength(100, MinimumLength = 1)] string UserName,
    [StringLength(500)] string? Password = null,
    [StringLength(1000)] string? ConnectionString = null,
    [StringLength(500)] string? SourceDescription = null,
    [StringLength(500)] string? ExtraParams = null,
    [Range(0, 3600)] int ConnectionTimeout = 30,
    bool IsDefault = false,
    CodeGenStatus Status = CodeGenStatus.Active,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>codegen-datasources.update</c> 的请求
/// </summary>
/// <remarks>
/// 口令与显式连接串留空即不改（照 4.x）；启停走 <c>set-status</c>。并发戳与当前的不同即冲突（框架仓储判定）
/// </remarks>
/// <param name="Id">数据源 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="SourceName">名称（唯一）</param>
/// <param name="DatabaseType">数据库类型</param>
/// <param name="Host">主机</param>
/// <param name="Port">端口</param>
/// <param name="DatabaseName">库名</param>
/// <param name="UserName">账号</param>
/// <param name="Password">口令，留空即不改</param>
/// <param name="ConnectionString">显式连接串，留空即不改</param>
/// <param name="SourceDescription">描述</param>
/// <param name="ExtraParams">拼连接串时附加的参数</param>
/// <param name="ConnectionTimeout">连接超时（秒）</param>
/// <param name="IsDefault">是否缺省数据源</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record UpdateCodegenDatasourceRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    [Required, StringLength(100, MinimumLength = 1)] string SourceName,
    CodeGenDatabaseType DatabaseType,
    [Required, StringLength(200, MinimumLength = 1)] string Host,
    [Range(0, 65535)] int Port,
    [Required, StringLength(200, MinimumLength = 1)] string DatabaseName,
    [Required, StringLength(100, MinimumLength = 1)] string UserName,
    [StringLength(500)] string? Password = null,
    [StringLength(1000)] string? ConnectionString = null,
    [StringLength(500)] string? SourceDescription = null,
    [StringLength(500)] string? ExtraParams = null,
    [Range(0, 3600)] int ConnectionTimeout = 30,
    bool IsDefault = false,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>codegen-datasources.set-status</c> 的请求
/// </summary>
/// <param name="Id">数据源 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Status">目标状态</param>
/// <param name="Remark">操作备注：给了即替换备注，不给保留原备注</param>
public sealed record SetCodegenDatasourceStatusRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    CodeGenStatus Status,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>codegen-datasources.delete</c> 的请求：集合入参
/// </summary>
/// <param name="Ids">数据源 ID，1 到 100 个</param>
public sealed record CodegenDatasourceIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
