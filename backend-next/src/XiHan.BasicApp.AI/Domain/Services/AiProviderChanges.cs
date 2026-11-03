// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI.Domain.Services;

/// <summary>
/// 本次调用对提供商的改动
/// </summary>
/// <remarks>
/// 作用域服务：提供商的写操作记下，<c>AiProviderCatalogMiddleware</c> 在调用成功、工作单元提交之后据此重载 Provider 目录
/// </remarks>
public sealed class AiProviderChanges
{
    /// <summary>
    /// 是否改过
    /// </summary>
    public bool Changed { get; private set; }

    /// <summary>
    /// 记下改过
    /// </summary>
    public void Mark() => Changed = true;
}
