// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Batch;

/// <summary>
/// 批量操作（删除、状态、授权、撤销）的逐项结果：对已有对象按标识的写操作只提供集合入参，单条是集合的特例。
/// </summary>
/// <param name="Succeeded">成功的对象 ID。</param>
/// <param name="Failed">失败的对象与错误码。</param>
public sealed record BatchResult(IReadOnlyList<Guid> Succeeded, IReadOnlyList<BatchFailure> Failed)
{
    /// <summary>
    /// 全部成功。
    /// </summary>
    public static BatchResult AllSucceeded(IReadOnlyList<Guid> ids) => new(ids, []);
}
