// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// CSV 写出器（UTF-8 BOM + CRLF + 单元格转义，零依赖流式，支撑百万级行）。
/// 转义规则与前端 useSchemaExport 一致：含逗号/引号/换行的单元格用双引号包裹，内部引号翻倍。
/// </summary>
public sealed class CsvExportWriter : IExportWriter
{
    private const int ProgressFlushInterval = 1000;

    /// <inheritdoc />
    public ExportFormat Format => ExportFormat.Csv;

    /// <inheritdoc />
    public async Task WriteAsync(
        Stream output,
        IReadOnlyList<ExportColumnDto> columns,
        IAsyncEnumerable<IReadOnlyList<string>> rows,
        Func<int, Task>? onProcessed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        // UTF-8 BOM，确保 Excel 正确识别中文
        var bom = Encoding.UTF8.GetPreamble();
        await output.WriteAsync(bom, cancellationToken);

        await using var writer = new StreamWriter(output, new UTF8Encoding(false), 1 << 16, leaveOpen: true)
        {
            NewLine = "\r\n"
        };

        // 表头
        await writer.WriteLineAsync(BuildLine(columns.Select(column => column.Title)));

        var processed = 0;
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            await writer.WriteLineAsync(BuildLine(row));
            processed++;

            if (processed % ProgressFlushInterval == 0 && onProcessed is not null)
            {
                await writer.FlushAsync(cancellationToken);
                await onProcessed(processed);
            }
        }

        await writer.FlushAsync(cancellationToken);
        if (onProcessed is not null)
        {
            await onProcessed(processed);
        }
    }

    private static string BuildLine(IEnumerable<string> cells)
    {
        return string.Join(",", cells.Select(Escape));
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuote)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
