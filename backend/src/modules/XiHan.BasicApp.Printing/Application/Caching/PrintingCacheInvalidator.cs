// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Printing.Application.Caching;

/// <summary>
/// 打印模块缓存失效器实现。
/// </summary>
public sealed class PrintingCacheInvalidator : IPrintingCacheInvalidator
{
    private readonly IDistributedCache<PrintTemplateCacheItem, string> _printTemplateCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PrintingCacheInvalidator(IDistributedCache<PrintTemplateCacheItem, string> printTemplateCache)
    {
        _printTemplateCache = printTemplateCache;
    }

    /// <summary>
    /// 失效打印模板解析缓存；模板增删改或启停后调用。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>缓存失效任务。</returns>
    public Task InvalidatePrintTemplateAsync(CancellationToken cancellationToken = default)
    {
        // considerUow:true 把失效动作延迟到事务成功提交，避免其它请求在未提交窗口内重新缓存旧模板。
        return _printTemplateCache.RemoveByPatternAsync(
            PrintingCacheKeys.AllPrintTemplatesPattern(), hideErrors: true, considerUow: true, token: cancellationToken);
    }
}
