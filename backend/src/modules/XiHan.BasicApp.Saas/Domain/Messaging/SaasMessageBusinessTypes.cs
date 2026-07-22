// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Messaging;

/// <summary>
/// SaaS 消息业务类型常量（单一事实源）
/// </summary>
/// <remarks>
/// 与 SysEmail/SysSms 行的 BusinessType 列对应：扇出/投递方以常量引用并配合 BusinessId 关联业务实体，
/// 禁止散落魔法字符串。新增业务类型必须在此登记。
/// </remarks>
public static class SaasMessageBusinessTypes
{
    /// <summary>
    /// 系统通知多渠道扇出（BusinessId = SysNotification.BasicId）
    /// </summary>
    public const string Notification = "message.notification";
}
