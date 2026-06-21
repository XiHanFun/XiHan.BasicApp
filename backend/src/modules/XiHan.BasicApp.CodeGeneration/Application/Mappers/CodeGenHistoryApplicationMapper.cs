#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenHistoryApplicationMapper
// Guid:c0de9e00-0905-4a00-9000-000000000905
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Application.Mappers;

/// <summary>
/// 代码生成历史应用层映射器
/// </summary>
public static class CodeGenHistoryApplicationMapper
{
    /// <summary>
    /// 映射代码生成历史列表项
    /// </summary>
    /// <param name="history">代码生成历史实体</param>
    /// <returns>代码生成历史列表项 DTO</returns>
    public static CodeGenHistoryListItemDto ToListItemDto(SysCodeGenHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return new CodeGenHistoryListItemDto
        {
            BasicId = history.BasicId,
            TableId = history.TableId,
            TableName = history.TableName,
            BatchNumber = history.BatchNumber,
            GenStatus = history.GenStatus,
            GenType = history.GenType,
            GenTime = history.GenTime,
            Duration = history.Duration,
            FileCount = history.FileCount,
            TotalSize = history.TotalSize,
            OperatorName = history.OperatorName
        };
    }

    /// <summary>
    /// 映射代码生成历史详情
    /// </summary>
    /// <param name="history">代码生成历史实体</param>
    /// <returns>代码生成历史详情 DTO</returns>
    public static CodeGenHistoryDetailDto ToDetailDto(SysCodeGenHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        var item = ToListItemDto(history);
        return new CodeGenHistoryDetailDto
        {
            BasicId = item.BasicId,
            TableId = item.TableId,
            TableName = item.TableName,
            BatchNumber = item.BatchNumber,
            GenStatus = item.GenStatus,
            GenType = item.GenType,
            GenTime = item.GenTime,
            Duration = item.Duration,
            FileCount = item.FileCount,
            TotalSize = item.TotalSize,
            OperatorName = item.OperatorName,
            GenPath = history.GenPath,
            DownloadPath = history.DownloadPath,
            GeneratedFiles = history.GeneratedFiles,
            UsedTemplates = history.UsedTemplates,
            TableSnapshot = history.TableSnapshot,
            ErrorMessage = history.ErrorMessage,
            OperatorIp = history.OperatorIp,
            Remark = history.Remark
        };
    }
}
