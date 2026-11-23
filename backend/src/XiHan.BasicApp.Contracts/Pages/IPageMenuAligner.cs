// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Pages;

/// <summary>
/// 把菜单的页面节点对齐到当前的页面登记
/// </summary>
/// <remarks>
/// Access 实现（与菜单种子同一套规则：页面菜单归登记，登记里没有的页面节点删除）；运行期页面变了的一方（插件模块）在自己的读写工作单元里调用
/// </remarks>
public interface IPageMenuAligner
{
    /// <summary>
    /// 对齐一次
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<Result> AlignAsync(CancellationToken cancellationToken);
}
