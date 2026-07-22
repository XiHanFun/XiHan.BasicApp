// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Application.Mappers;

/// <summary>
/// 代码生成数据源应用层映射器（手写静态映射，对齐 Saas 约定）
/// </summary>
/// <remarks>
/// 详情/列表输出时密钥（Password/ConnectionString）一律脱敏：列表 DTO 不含明细字段；
/// 详情 DTO 的 ConnectionString 返回脱敏占位（有值→"******"，无值→null），绝不回传明文。
/// </remarks>
public static class CodeGenDataSourceApplicationMapper
{
    /// <summary>密钥脱敏占位</summary>
    private const string SecretMask = "******";

    /// <summary>
    /// 创建 DTO → 创建命令
    /// </summary>
    public static CodeGenDataSourceCreateCommand ToCreateCommand(CodeGenDataSourceCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new CodeGenDataSourceCreateCommand(
            input.SourceName,
            input.SourceDescription,
            input.DatabaseType,
            input.Host,
            input.Port,
            input.DatabaseName,
            input.UserName,
            input.Password,
            input.ConnectionString,
            input.ExtraParams,
            input.ConnectionTimeout,
            input.IsDefault,
            input.Status,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 更新 DTO → 更新命令
    /// </summary>
    public static CodeGenDataSourceUpdateCommand ToUpdateCommand(CodeGenDataSourceUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new CodeGenDataSourceUpdateCommand(
            input.BasicId,
            input.SourceName,
            input.SourceDescription,
            input.DatabaseType,
            input.Host,
            input.Port,
            input.DatabaseName,
            input.UserName,
            input.Password,
            input.ConnectionString,
            input.ExtraParams,
            input.ConnectionTimeout,
            input.IsDefault,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 状态更新 DTO → 状态命令
    /// </summary>
    public static CodeGenDataSourceStatusChangeCommand ToStatusCommand(CodeGenDataSourceStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new CodeGenDataSourceStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 实体 → 列表项 DTO
    /// </summary>
    public static CodeGenDataSourceListItemDto ToListItemDto(SysCodeGenDataSource entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new CodeGenDataSourceListItemDto
        {
            BasicId = entity.BasicId,
            SourceName = entity.SourceName,
            SourceDescription = entity.SourceDescription,
            DatabaseType = entity.DatabaseType,
            Host = entity.Host,
            Port = entity.Port,
            DatabaseName = entity.DatabaseName,
            IsDefault = entity.IsDefault,
            LastTestTime = entity.LastTestTime,
            LastTestResult = entity.LastTestResult,
            LastTestMessage = entity.LastTestMessage,
            Status = entity.Status,
            Sort = entity.Sort,
            Remark = entity.Remark,
            CreatedTime = entity.CreatedTime
        };
    }

    /// <summary>
    /// 实体 → 详情 DTO（密钥脱敏）
    /// </summary>
    public static CodeGenDataSourceDetailDto ToDetailDto(SysCodeGenDataSource entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var item = ToListItemDto(entity);
        return new CodeGenDataSourceDetailDto
        {
            BasicId = item.BasicId,
            SourceName = item.SourceName,
            SourceDescription = item.SourceDescription,
            DatabaseType = item.DatabaseType,
            Host = item.Host,
            Port = item.Port,
            DatabaseName = item.DatabaseName,
            IsDefault = item.IsDefault,
            LastTestTime = item.LastTestTime,
            LastTestResult = item.LastTestResult,
            LastTestMessage = item.LastTestMessage,
            Status = item.Status,
            Sort = item.Sort,
            Remark = item.Remark,
            CreatedTime = item.CreatedTime,
            UserName = entity.UserName,
            // 连接串含密钥：脱敏输出，绝不回传明文/密文
            ConnectionString = string.IsNullOrWhiteSpace(entity.ConnectionString) ? null : SecretMask,
            ExtraParams = entity.ExtraParams,
            ConnectionTimeout = entity.ConnectionTimeout,
            CreatedId = entity.CreatedId,
            CreatedBy = entity.CreatedBy,
            ModifiedId = entity.ModifiedId,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// 连接测试领域结果 → 连接测试结果 DTO
    /// </summary>
    public static CodeGenConnectionTestResultDto ToConnectionTestResultDto(CodeGenDataSourceConnectionTestResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new CodeGenConnectionTestResultDto
        {
            Success = result.Success,
            Message = result.Message,
            ElapsedMilliseconds = result.ElapsedMilliseconds
        };
    }
}
