// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块提供的能力
/// </summary>
public static class AICapabilities
{
    /// <summary>
    /// AI 能力：提供商、提示词、知识库与助手
    /// </summary>
    public const string AI = AppInfo.CapabilityPrefix + "ai";

    /// <summary>
    /// 能力版本
    /// </summary>
    public const int Version = 1;
}
