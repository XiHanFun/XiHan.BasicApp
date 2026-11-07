// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenHistories;

/// <summary>
/// 生成历史的一行
/// </summary>
/// <param name="Id">ID</param>
/// <param name="TableId">表配置 ID</param>
/// <param name="TableName">表名</param>
/// <param name="BatchNumber">批次号</param>
/// <param name="GenerationStatus">结果</param>
/// <param name="DeliveryType">交付方式</param>
/// <param name="GeneratedAt">生成时间</param>
/// <param name="Duration">耗时（毫秒）</param>
/// <param name="FileCount">产物文件数</param>
/// <param name="TotalSize">产物总大小（字节）</param>
/// <param name="GenPath">写进项目时写到的根目录</param>
/// <param name="PackageExpiresAt">产物包到期时间；没有产物包为空</param>
/// <param name="OperatorName">操作人</param>
public sealed record CodegenHistoryResponse(
    Guid Id,
    Guid TableId,
    string TableName,
    string? BatchNumber,
    CodeGenGenerationStatus GenerationStatus,
    CodeGenDeliveryType DeliveryType,
    DateTimeOffset GeneratedAt,
    long Duration,
    int FileCount,
    long TotalSize,
    string? GenPath,
    DateTimeOffset? PackageExpiresAt,
    string? OperatorName)
{
    /// <summary>
    /// 由实体建出
    /// </summary>
    /// <param name="history">生成历史</param>
    public static CodegenHistoryResponse From(SysCodeGenHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);
        return new(
            history.Id,
            history.TableId,
            history.TableName,
            history.BatchNumber,
            history.GenerationStatus,
            history.DeliveryType,
            history.GeneratedAt,
            history.Duration,
            history.FileCount,
            history.TotalSize,
            history.GenPath,
            history.FileId is null ? null : history.GeneratedAt.Add(CodeGenGenerationService.PackageLifetime),
            history.OperatorName);
    }
}

/// <summary>
/// 表配置在生成时的快照
/// </summary>
/// <param name="TableName">表名</param>
/// <param name="TableComment">表注释</param>
/// <param name="ClassName">类名</param>
/// <param name="Namespace">命名空间</param>
/// <param name="ModuleName">包名</param>
/// <param name="BusinessName">业务名</param>
/// <param name="TemplateType">生成模式</param>
/// <param name="Scope">生成范围</param>
/// <param name="Target">生成目标</param>
/// <param name="EnabledActions">勾选的操作</param>
public sealed record CodegenTableSnapshot(
    string TableName,
    string? TableComment,
    string ClassName,
    string? Namespace,
    string? ModuleName,
    string? BusinessName,
    CodeGenTemplateType TemplateType,
    CodeGenScope Scope,
    CodeGenTarget Target,
    string? EnabledActions);

/// <summary>
/// 生成历史的详情
/// </summary>
/// <param name="History">生成历史</param>
/// <param name="GeneratedFiles">产物文件（包内路径）</param>
/// <param name="UsedTemplates">用到的模板编码</param>
/// <param name="TableSnapshot">表配置快照</param>
/// <param name="ErrorMessage">失败原因（错误码与生成时的文案）</param>
/// <param name="OperatorId">操作人 ID</param>
/// <param name="OperatorIp">操作人 IP</param>
/// <param name="Remark">备注</param>
public sealed record CodegenHistoryDetailResponse(
    CodegenHistoryResponse History,
    IReadOnlyList<string> GeneratedFiles,
    IReadOnlyList<string> UsedTemplates,
    CodegenTableSnapshot? TableSnapshot,
    string? ErrorMessage,
    string? OperatorId,
    string? OperatorIp,
    string? Remark)
{
    /// <summary>
    /// 由实体建出
    /// </summary>
    /// <param name="history">生成历史</param>
    public static CodegenHistoryDetailResponse From(SysCodeGenHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);
        return new(
            CodegenHistoryResponse.From(history),
            ListOf(history.GeneratedFiles),
            ListOf(history.UsedTemplates),
            history.TableSnapshot is null ? null : JsonSerializer.Deserialize(history.TableSnapshot, CodeGenerationJsonContext.Default.CodegenTableSnapshot),
            history.ErrorMessage,
            history.OperatorId,
            history.OperatorIp,
            history.Remark);
    }

    private static List<string> ListOf(string? json)
        => json is null ? [] : JsonSerializer.Deserialize(json, CodeGenerationJsonContext.Default.ListString) ?? [];
}

/// <summary>
/// 按 ID 取一条生成历史的请求
/// </summary>
/// <param name="Id">生成历史 ID</param>
public sealed record CodegenHistoryIdRequest(Guid Id);
