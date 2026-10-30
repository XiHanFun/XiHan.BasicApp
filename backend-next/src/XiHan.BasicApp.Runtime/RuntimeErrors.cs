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

    /// <summary>
    /// 结构升级的脚本目录不对。
    /// </summary>
    /// <remarks>
    /// 目录不存在、没有版本子目录，或子目录名不是版本号。
    /// </remarks>
    public const string SchemaScriptsInvalid = "RUNTIME_VALIDATION_003";

    /// <summary>
    /// 库的结构版本比应用带的脚本新。
    /// </summary>
    public const string SchemaNewerThanApp = "RUNTIME_VALIDATION_004";

    /// <summary>
    /// 结构升级失败。
    /// </summary>
    public const string SchemaUpgradeFailed = "RUNTIME_UNAVAILABLE_001";

    /// <summary>
    /// 等别的节点升级超时。
    /// </summary>
    public const string SchemaUpgradeLocked = "RUNTIME_UNAVAILABLE_002";

    /// <summary>
    /// 维护模式：别的节点正在升级共享的库。
    /// </summary>
    public const string MaintenanceMode = "RUNTIME_UNAVAILABLE_003";
}
