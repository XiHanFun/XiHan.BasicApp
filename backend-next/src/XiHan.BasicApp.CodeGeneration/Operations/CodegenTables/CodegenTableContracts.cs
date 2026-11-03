// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenTableColumns;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenTables;

/// <summary>
/// 表配置
/// </summary>
/// <param name="Id">主键</param>
/// <param name="TableName">库里的表名</param>
/// <param name="TableComment">表注释</param>
/// <param name="ClassName">实体类名</param>
/// <param name="Namespace">包的根命名空间</param>
/// <param name="ModuleName">包名</param>
/// <param name="BusinessName">业务名</param>
/// <param name="FunctionName">功能名</param>
/// <param name="Author">作者</param>
/// <param name="TemplateType">生成模式</param>
/// <param name="DeliveryType">交付方式</param>
/// <param name="Scope">生成范围</param>
/// <param name="Target">生成目标</param>
/// <param name="EnabledActions">勾选的动作（逗号分隔，为空即缺省集合）</param>
/// <param name="PrimaryKeyColumn">主键列名</param>
/// <param name="TreeParentColumn">树表的父级列名</param>
/// <param name="TreeNameColumn">树表的名称列名</param>
/// <param name="MasterTableId">主从模式里主表的表配置 ID</param>
/// <param name="MasterForeignKey">主从模式里指向主表的外键列名</param>
/// <param name="DatabaseType">数据库类型</param>
/// <param name="DataSourceId">数据源 ID，为空即主库</param>
/// <param name="GenerationStatus">最近一次生成的结果</param>
/// <param name="LastGeneratedAt">最近一次成功生成的时间（UTC）</param>
/// <param name="Options">扩展选项（JSON）</param>
/// <param name="Status">状态</param>
/// <param name="Remark">备注</param>
/// <param name="ConcurrencyStamp">并发戳：修改与启停时原样交回</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record CodegenTableResponse(
    Guid Id,
    string TableName,
    string? TableComment,
    string ClassName,
    string? Namespace,
    string? ModuleName,
    string? BusinessName,
    string? FunctionName,
    string? Author,
    CodeGenTemplateType TemplateType,
    CodeGenDeliveryType DeliveryType,
    CodeGenScope Scope,
    CodeGenTarget Target,
    string? EnabledActions,
    string? PrimaryKeyColumn,
    string? TreeParentColumn,
    string? TreeNameColumn,
    Guid? MasterTableId,
    string? MasterForeignKey,
    CodeGenDatabaseType DatabaseType,
    Guid? DataSourceId,
    CodeGenGenerationStatus GenerationStatus,
    DateTimeOffset? LastGeneratedAt,
    string? Options,
    CodeGenStatus Status,
    string? Remark,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="table">表配置</param>
    public static CodegenTableResponse From(SysCodeGenTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        return new CodegenTableResponse(
            table.Id,
            table.TableName,
            table.TableComment,
            table.ClassName,
            table.Namespace,
            table.ModuleName,
            table.BusinessName,
            table.FunctionName,
            table.Author,
            table.TemplateType,
            table.DeliveryType,
            table.Scope,
            table.Target,
            table.EnabledActions,
            table.PrimaryKeyColumn,
            table.TreeParentColumn,
            table.TreeNameColumn,
            table.MasterTableId,
            table.MasterForeignKey,
            table.DatabaseType,
            table.DataSourceId,
            table.GenerationStatus,
            table.LastGeneratedAt,
            table.Options,
            table.Status,
            table.Remark,
            table.ConcurrencyStamp,
            table.CreatedAt,
            table.ModifiedAt);
    }
}

/// <summary>
/// 表配置的详情
/// </summary>
/// <param name="Table">表配置</param>
/// <param name="Columns">列配置，按排序号</param>
public sealed record CodegenTableDetailResponse(CodegenTableResponse Table, IReadOnlyList<CodegenTableColumnResponse> Columns);

/// <summary>
/// 表配置下拉选项
/// </summary>
/// <param name="Items">全部表配置，按表名</param>
public sealed record CodegenTableOptionsResponse(IReadOnlyList<CodegenTableResponse> Items);

/// <summary>
/// 库里的一张表
/// </summary>
/// <param name="Name">表名（分表是折叠后的逻辑名）</param>
/// <param name="Comment">表注释</param>
/// <param name="IsSplit">是否分表</param>
public sealed record CodegenDbTableResponse(string Name, string? Comment, bool IsSplit);

/// <summary>
/// 导入一张表失败的原因
/// </summary>
/// <param name="TableName">表名</param>
/// <param name="Code">错误码</param>
/// <param name="Message">原因（按调用方的区域）</param>
public sealed record CodegenImportFailure(string TableName, string Code, string Message);

/// <summary>
/// 导入的结果
/// </summary>
/// <param name="Succeeded">导入了的表</param>
/// <param name="Failed">没导入的表与原因</param>
public sealed record CodegenImportResponse(IReadOnlyList<string> Succeeded, IReadOnlyList<CodegenImportFailure> Failed);

/// <summary>
/// 同步结构的结果
/// </summary>
/// <param name="AddedCount">新增的列数</param>
/// <param name="UpdatedCount">更新的列数</param>
/// <param name="RemovedCount">移除的列数</param>
/// <param name="AddedColumns">新增的列</param>
/// <param name="RemovedColumns">移除的列</param>
public sealed record CodegenSyncResponse(int AddedCount, int UpdatedCount, int RemovedCount, IReadOnlyList<string> AddedColumns, IReadOnlyList<string> RemovedColumns);

/// <summary>
/// <c>codegen-tables.get</c> 与 <c>sync-schema</c> 的请求
/// </summary>
/// <param name="Id">表配置 ID</param>
public sealed record CodegenTableIdRequest(Guid Id);

/// <summary>
/// <c>codegen-tables.options</c> 的请求
/// </summary>
public sealed record ListCodegenTableOptionsRequest;

/// <summary>
/// <c>codegen-tables.db-tables</c> 的请求
/// </summary>
/// <remarks>
/// 页码换算成跳过与取的条数交给框架读库结构；框架的越界错误原样返回
/// </remarks>
public sealed record ListCodegenDbTablesRequest
{
    /// <summary>
    /// 数据源 ID，为空即主库
    /// </summary>
    public Guid? DataSourceId { get; set; }

    /// <summary>
    /// 关键字（匹配表名与注释）
    /// </summary>
    [StringLength(200)]
    public string? Keyword { get; set; }

    /// <summary>
    /// 页码与页大小
    /// </summary>
    [Required]
    public PageInfo Page { get; set; } = new();
}

/// <summary>
/// <c>codegen-tables.import</c> 的请求
/// </summary>
/// <param name="TableNames">表名（照库表列表返回的原名），1 到 100 张</param>
/// <param name="DataSourceId">数据源 ID，为空即主库</param>
public sealed record ImportCodegenTablesRequest(
    [Required, MinLength(1), MaxLength(100)] IReadOnlyList<string> TableNames,
    Guid? DataSourceId = null);

/// <summary>
/// <c>codegen-tables.update</c> 的请求
/// </summary>
/// <remarks>
/// 照 4.x 整体修改；值变了的被跟踪字段记为人工改过，同步结构时保留。生成模式不能是通用；勾了导入必须勾新建
/// </remarks>
/// <param name="Id">表配置 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="TableName">库里的表名</param>
/// <param name="ClassName">实体类名</param>
/// <param name="TemplateType">生成模式</param>
/// <param name="DeliveryType">交付方式</param>
/// <param name="Scope">生成范围</param>
/// <param name="Target">生成目标</param>
/// <param name="DatabaseType">数据库类型</param>
/// <param name="Status">状态</param>
/// <param name="TableComment">表注释</param>
/// <param name="Namespace">包的根命名空间</param>
/// <param name="ModuleName">包名</param>
/// <param name="BusinessName">业务名</param>
/// <param name="FunctionName">功能名</param>
/// <param name="Author">作者</param>
/// <param name="EnabledActions">勾选的动作（逗号分隔）</param>
/// <param name="PrimaryKeyColumn">主键列名</param>
/// <param name="TreeParentColumn">树表的父级列名</param>
/// <param name="TreeNameColumn">树表的名称列名</param>
/// <param name="MasterTableId">主表的表配置 ID</param>
/// <param name="MasterForeignKey">指向主表的外键列名</param>
/// <param name="DataSourceId">数据源 ID，为空即主库</param>
/// <param name="Options">扩展选项（JSON）</param>
/// <param name="Remark">备注</param>
public sealed record UpdateCodegenTableRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    [Required, StringLength(200, MinimumLength = 1)] string TableName,
    [Required, StringLength(200, MinimumLength = 1)] string ClassName,
    CodeGenTemplateType TemplateType,
    CodeGenDeliveryType DeliveryType,
    CodeGenScope Scope,
    CodeGenTarget Target,
    CodeGenDatabaseType DatabaseType,
    CodeGenStatus Status,
    [StringLength(500)] string? TableComment = null,
    [StringLength(500)] string? Namespace = null,
    [StringLength(100)] string? ModuleName = null,
    [StringLength(100)] string? BusinessName = null,
    [StringLength(100)] string? FunctionName = null,
    [StringLength(100)] string? Author = null,
    [StringLength(200)] string? EnabledActions = null,
    [StringLength(200)] string? PrimaryKeyColumn = null,
    [StringLength(200)] string? TreeParentColumn = null,
    [StringLength(200)] string? TreeNameColumn = null,
    Guid? MasterTableId = null,
    [StringLength(200)] string? MasterForeignKey = null,
    Guid? DataSourceId = null,
    string? Options = null,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>codegen-tables.set-status</c> 的请求
/// </summary>
/// <param name="Id">表配置 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Status">目标状态</param>
/// <param name="Remark">操作备注：给了即替换备注，不给保留原备注</param>
public sealed record SetCodegenTableStatusRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    CodeGenStatus Status,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>codegen-tables.delete</c> 的请求：集合入参
/// </summary>
/// <param name="Ids">表配置 ID，1 到 100 个</param>
public sealed record CodegenTableIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>codegen-tables.preview</c> 的请求
/// </summary>
/// <param name="Id">表配置 ID</param>
/// <param name="TemplateCodes">只用这几份模板；不给按表的生成模式取启用的模板</param>
public sealed record PreviewCodegenTableRequest(Guid Id, IReadOnlyList<string>? TemplateCodes = null);

/// <summary>
/// <c>codegen-tables.generate</c> 的请求
/// </summary>
/// <param name="Id">表配置 ID</param>
/// <param name="DeliveryType">交付方式；不给按表配置的交付方式</param>
/// <param name="TemplateCodes">只用这几份模板；不给按表的生成模式取启用的模板</param>
public sealed record GenerateCodegenTableRequest(Guid Id, CodeGenDeliveryType? DeliveryType = null, IReadOnlyList<string>? TemplateCodes = null);

/// <summary>
/// <c>codegen-tables.generate</c> 的响应
/// </summary>
/// <remarks>
/// 打包的从生成历史的下载端点取产物包（<c>GET /api/codegen-histories/{id}/package</c>）；写进项目的给出写了几个、跳过了哪些（已存在的只首次写的文件）与写到的根目录
/// </remarks>
/// <param name="HistoryId">这次生成的历史 ID</param>
/// <param name="DeliveryType">交付方式</param>
/// <param name="FileCount">产物文件数</param>
/// <param name="Duration">耗时（毫秒）</param>
/// <param name="FileId">产物包的文件 ID；写进项目时为空</param>
/// <param name="WrittenCount">写进项目的文件数</param>
/// <param name="SkippedPaths">跳过的文件（已存在的只首次写的文件）</param>
/// <param name="TargetRoots">写到的根目录</param>
public sealed record CodegenGenerateResponse(Guid HistoryId, CodeGenDeliveryType DeliveryType, int FileCount, long Duration, Guid? FileId, int WrittenCount, IReadOnlyList<string> SkippedPaths, IReadOnlyList<string> TargetRoots);

/// <summary>
/// 一个生成的文件
/// </summary>
/// <param name="Path">产物包里的路径：<c>backend/…</c>（相对 <c>backend-next</c>）、<c>frontend/…</c>（相对 <c>frontend-next</c>）或 <c>_registration/…</c></param>
/// <param name="Side">属于哪一侧</param>
/// <param name="TemplateCode">来源模板编码；引擎拼出的（文案、登记片段）为空</param>
/// <param name="WriteMode">写入方式</param>
/// <param name="Content">内容</param>
public sealed record CodegenArtifactResponse(string Path, CodeGenSide Side, string? TemplateCode, CodeGenWriteMode WriteMode, string Content)
{
    /// <summary>
    /// 从产物投影
    /// </summary>
    /// <param name="artifact">产物</param>
    public static CodegenArtifactResponse From(CodeGenArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        return new CodegenArtifactResponse(artifact.PackagePath, artifact.Side, artifact.TemplateCode, artifact.WriteMode, artifact.Content);
    }
}

/// <summary>
/// <c>codegen-tables.preview</c> 的响应
/// </summary>
/// <param name="Files">生成的文件，按模板的排序号，最后是引擎拼出的文案与登记片段</param>
public sealed record CodegenPreviewResponse(IReadOnlyList<CodegenArtifactResponse> Files);
