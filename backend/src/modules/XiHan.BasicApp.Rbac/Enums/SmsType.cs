#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsType
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 短信类型枚举
/// </summary>
public enum SmsType
{
    /// <summary>
    /// 验证码
    /// </summary>
    VerificationCode = 0,

    /// <summary>
    /// 通知短信
    /// </summary>
    Notification = 1,

    /// <summary>
    /// 营销短信
    /// </summary>
    Marketing = 2,

    /// <summary>
    /// 自定义短信
    /// </summary>
    Custom = 99
}
