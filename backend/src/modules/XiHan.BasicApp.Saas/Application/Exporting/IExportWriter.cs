// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// 导出写出器（按格式将行流写入输出流）
/// </summary>
public interface IExportWriter
{
    /// <summary>
    /// 支持的格式
    /// </summary>
    ExportFormat Format { get; }

    /// <summary>
    /// 流式写出（表头 + 行流）
    /// </summary>
    /// <param name="output">输出流</param>
    /// <param name="columns">列定义（表头 + 顺序）</param>
    /// <param name="rows">行流（已投影为字符串）</param>
    /// <param name="onProcessed">每批已写出累计行数回调（用于进度回写，可空）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteAsync(
        Stream output,
        IReadOnlyList<ExportColumnDto> columns,
        IAsyncEnumerable<IReadOnlyList<string>> rows,
        Func<int, Task>? onProcessed,
        CancellationToken cancellationToken = default);
}
