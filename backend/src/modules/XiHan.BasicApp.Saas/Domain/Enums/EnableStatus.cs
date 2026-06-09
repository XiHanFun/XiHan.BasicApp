#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnableStatus
// Guid:3288f5cb-0c0a-4657-8412-49fc864890a1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 08:31:07
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 启用状态
/// </summary>
public enum EnableStatus
{
    /// <summary>
    /// 禁用
    /// </summary>
    [Description("禁用")]
    Disabled = 0,

    /// <summary>
    /// 启用
    /// </summary>
    [Description("启用")]
    Enabled = 1
}
