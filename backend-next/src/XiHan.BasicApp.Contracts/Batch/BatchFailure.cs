// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Batch;

/// <summary>
/// 批量操作里失败的一项
/// </summary>
/// <param name="Id">目标对象 ID</param>
/// <param name="Code">失败的错误码（<c>&lt;包&gt;_&lt;KIND&gt;_&lt;NNN&gt;</c>）</param>
/// <param name="Message">失败原因，已按这次调用的区域本地化，可直接显示</param>
public sealed record BatchFailure(Guid Id, string Code, string Message)
{
    /// <summary>
    /// 由失败结果得出错误码与文案
    /// </summary>
    public static BatchFailure From(Guid id, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new BatchFailure(id, error.Code, error.LocalizedMessage());
    }
}
