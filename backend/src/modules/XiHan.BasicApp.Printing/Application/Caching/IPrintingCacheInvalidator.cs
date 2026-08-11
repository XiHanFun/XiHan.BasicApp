// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Application.Caching;

/// <summary>
/// 打印模块缓存失效器。
/// </summary>
public interface IPrintingCacheInvalidator
{
    /// <summary>
    /// 失效打印模板解析缓存；模板增删改或启停后调用。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>缓存失效任务。</returns>
    Task InvalidatePrintTemplateAsync(CancellationToken cancellationToken = default);
}
