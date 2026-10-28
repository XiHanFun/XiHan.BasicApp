// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Settings;

/// <summary>
/// 读系统参数的生效值（Settings 包实现）：当前租户的同键参数优先、再取平台参数；停用的视为不存在；值为空时回落缺省值；加密值解密后返回。
/// 同层的包不互相引用，要读参数的包注入这个接口；没装 Settings 时容器里没有它。
/// </summary>
public interface IConfigValueReader
{
    /// <summary>
    /// 按键取生效值：没有生效的参数或值与缺省值都为空时成功并返回 <c>None</c>，解不开密文时失败。
    /// </summary>
    /// <param name="key">参数键。</param>
    /// <param name="context">调用上下文（决定当前租户）。</param>
    ValueTask<Result<Maybe<string>>> GetAsync(string key, InvocationContext context);
}
