#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageDto
// Guid:cb2fd758-c44a-44ab-b45f-bf6c5335dcd7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:33:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 消息发送结果 DTO
/// </summary>
public class MessageDispatchResultDto
{
    /// <summary>
    /// 站内通知创建数量
    /// </summary>
    public int NotificationCount { get; set; }

    /// <summary>
    /// 邮件创建数量
    /// </summary>
    public int EmailCount { get; set; }

    /// <summary>
    /// 短信创建数量
    /// </summary>
    public int SmsCount { get; set; }

    /// <summary>
    /// 总创建数量
    /// </summary>
    public int TotalCount => NotificationCount + EmailCount + SmsCount;
}
