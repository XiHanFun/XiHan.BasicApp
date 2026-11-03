// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 提供商、提示词与助手的启停状态
/// </summary>
/// <remarks>
/// 4.x 有 <c>IsEnabled</c> 与 <c>Status</c> 两个开关、解析时两个都要开，6.0 合成这一个（方案 N4）
/// </remarks>
public enum AiStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用
    /// </summary>
    Disabled = 1,
}
