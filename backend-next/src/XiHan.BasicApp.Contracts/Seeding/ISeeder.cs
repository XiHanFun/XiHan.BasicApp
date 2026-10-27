// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Seeding;

/// <summary>
/// 一个种子：按 <see cref="Order"/>（阶段 + 号段，见 <see cref="SeedOrders"/>）排序，各自在一次独立调用（完整管道、自己的工作单元）里执行。
/// 目录类种子每次对齐元数据，运营数据只首写；种子必须可重复执行。
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// 种子名，用于日志与 <c>seed --only</c> 筛选。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 执行顺序；小的先跑。
    /// </summary>
    int Order { get; }

    /// <summary>
    /// 执行种子；失败返回 <see cref="Result"/>，由运行器中断后续种子。
    /// </summary>
    ValueTask<Result> SeedAsync(InvocationContext context);
}
