// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Application.Caching;

/// <summary>
/// 打印模块业务缓存键构造器。
/// </summary>
public static class PrintingCacheKeys
{
    /// <summary>
    /// 打印模板解析缓存键（按请求租户、请求作用域和模板编码隔离）。
    /// </summary>
    /// <param name="tenantId">请求租户；null/0 表示平台上下文。</param>
    /// <param name="scope">请求作用域枚举值。</param>
    /// <param name="templateCode">已经规范化的模板编码。</param>
    /// <returns>业务缓存键。</returns>
    public static string PrintTemplate(long? tenantId, int scope, string templateCode)
    {
        var tenantSegment = tenantId is > 0 ? tenantId.Value.ToString() : "platform";
        return $"tenant:{tenantSegment}:scope:{scope}:code:{templateCode}";
    }

    /// <summary>
    /// 全部打印模板解析缓存匹配模式。
    /// </summary>
    /// <returns>覆盖所有租户、作用域和模板编码的模式。</returns>
    public static string AllPrintTemplatesPattern()
    {
        return "tenant:*:scope:*:code:*";
    }
}
