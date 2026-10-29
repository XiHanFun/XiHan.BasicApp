// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime;

/// <summary>
/// 运行时基座的错误码。
/// </summary>
/// <remarks>
/// 格式 <c>RUNTIME_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </remarks>
public static class RuntimeErrors
{
    /// <summary>
    /// 数据库绑定 <c>XiHan:Bindings:db</c> 缺失或为空：数据库是启动硬要求。
    /// </summary>
    public const string DatabaseBindingMissing = "RUNTIME_VALIDATION_001";

    /// <summary>
    /// 数据库绑定的 <c>provider</c> 属性不是支持的库类型。
    /// </summary>
    public const string DatabaseProviderUnsupported = "RUNTIME_VALIDATION_002";
}
