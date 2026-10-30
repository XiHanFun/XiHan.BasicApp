// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Host;

/// <summary>
/// 宿主的错误码：<c>HOST_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </summary>
public static class HostErrors
{
    /// <summary>
    /// <c>seed --only</c> 指定了不存在的种子。
    /// </summary>
    public const string SeederUnknown = "HOST_VALIDATION_001";

    /// <summary>
    /// <c>schema</c> 命令的动作不认识。
    /// </summary>
    /// <remarks>
    /// 参数 <c>action</c>；目前只有 <c>upgrade</c>。
    /// </remarks>
    public const string SchemaActionUnknown = "HOST_VALIDATION_002";
}
