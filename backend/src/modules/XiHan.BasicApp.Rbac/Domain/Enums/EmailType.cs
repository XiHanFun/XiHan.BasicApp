#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailType
// Guid:73ae8b23-9e8c-48b9-a2a4-df824c3e4164
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Domain.Enums;

/// <summary>
/// 邮件类型枚举
/// </summary>
public enum EmailType
{
    /// <summary>
    /// 系统邮件
    /// </summary>
    System = 0,

    /// <summary>
    /// 验证邮件
    /// </summary>
    Verification = 1,

    /// <summary>
    /// 通知邮件
    /// </summary>
    Notification = 2,

    /// <summary>
    /// 营销邮件
    /// </summary>
    Marketing = 3,

    /// <summary>
    /// 自定义邮件
    /// </summary>
    Custom = 99
}
