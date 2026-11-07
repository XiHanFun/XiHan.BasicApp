// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成模块提供的能力
/// </summary>
public static class CodeGenerationCapabilities
{
    /// <summary>
    /// 代码生成能力：数据源、表与列配置、模板、生成与零代码运行时
    /// </summary>
    public const string CodeGeneration = AppInfo.CapabilityPrefix + "codegeneration";

    /// <summary>
    /// 能力版本
    /// </summary>
    public const int Version = 1;
}
