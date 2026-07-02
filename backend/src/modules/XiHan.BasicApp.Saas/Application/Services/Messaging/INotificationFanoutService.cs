#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:INotificationFanoutService
// Guid:8c1b5e2a-7d94-4f36-a1c8-52e9b0d47f61
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 系统通知多渠道扇出服务
/// </summary>
/// <remarks>
/// 发布通知后按 <see cref="SysNotification.DeliveryChannels"/> 扇出到 邮箱/短信/机器人（站内信由发布链路本身承载）：
/// - 邮箱/短信：收件人经偏好门控（渠道感知）后逐用户落 SysEmail/SysSms 行（发布事务内），由发件箱异步发送；
/// - 机器人：通知级广播（无用户维度），UoW 提交后经框架 Bot 管道直发，失败仅记日志不回滚发布。
/// </remarks>
public interface INotificationFanoutService
{
    /// <summary>
    /// 按通知的投递渠道执行多渠道扇出（应在发布成功后、发布事务内调用）
    /// </summary>
    /// <param name="notification">已发布的系统通知</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task FanoutAsync(SysNotification notification, CancellationToken cancellationToken = default);
}
