// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 本次调用改了要加载的插件（作用域）
/// </summary>
/// <remarks>
/// 启停、改授予、卸载的操作标记它；调用成功提交之后由 <see cref="Middleware.PluginReconcileMiddleware"/> 让本节点立即对齐
/// </remarks>
public sealed class PluginChanges
{
    /// <summary>
    /// 是否要对齐
    /// </summary>
    public bool Requested { get; private set; }

    /// <summary>
    /// 标记本次调用提交后要对齐
    /// </summary>
    public void Request() => Requested = true;
}
