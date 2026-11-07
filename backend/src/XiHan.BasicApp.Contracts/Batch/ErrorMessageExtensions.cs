// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Batch;

/// <summary>
/// 错误的文案
/// </summary>
public static class ErrorMessageExtensions
{
    /// <summary>
    /// 按这次调用的区域解析错误的文案
    /// </summary>
    /// <remarks>
    /// <see cref="Error.Message"/> 用的是进程默认区域，批量结果要给调用方看，所以走 <see cref="MessageScope.Current"/>
    /// </remarks>
    public static string LocalizedMessage(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error.Text ?? error.MessageKey!.L(error.Details);
    }
}
