// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Workflow.Application.Mappers;

/// <summary>
/// 引擎时间换算
/// </summary>
/// <remarks>
/// 引擎时间取自框架时钟，缺省是服务器本地时间，落库再读出后丢失 Kind；而 API 对 <see cref="DateTime"/> 的约定是
/// 「存储为 UTC、按用户时区输出」，直接交给 DTO 会被当成 UTC 再换算一次（本地 +8 时区就多出 8 小时）。
/// 工作流 DTO 的时间一律用 <see cref="DateTimeOffset"/>，按时钟的时间类型在这里还原。
/// </remarks>
public static class WorkflowTimes
{
    /// <summary>
    /// 引擎时间转带偏移的时间（未标注 Kind 时按时钟口径：UTC 时钟按 UTC，否则按服务器本地时间）
    /// </summary>
    /// <param name="value">引擎时间</param>
    /// <param name="clockKind">框架时钟的时间类型</param>
    /// <returns>带偏移的时间</returns>
    public static DateTimeOffset ToOffset(DateTime value, DateTimeKind clockKind)
    {
        return new DateTimeOffset(value.Kind != DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, clockKind == DateTimeKind.Utc ? DateTimeKind.Utc : DateTimeKind.Local));
    }

    /// <summary>
    /// 可空引擎时间转带偏移的时间
    /// </summary>
    /// <param name="value">引擎时间</param>
    /// <param name="clockKind">框架时钟的时间类型</param>
    /// <returns>带偏移的时间；为空时返回空</returns>
    public static DateTimeOffset? ToOffset(DateTime? value, DateTimeKind clockKind)
    {
        return value is { } time ? ToOffset(time, clockKind) : null;
    }
}
