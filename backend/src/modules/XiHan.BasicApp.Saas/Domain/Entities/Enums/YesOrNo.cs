#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:YesOrNo
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 是或否枚举
/// </summary>
public enum YesOrNo
{
    /// <summary>
    /// 否
    /// </summary>
    [Description("否")]
    No = 0,

    /// <summary>
    /// 是
    /// </summary>
    [Description("是")]
    Yes = 1
}
