#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysChatMessage.Enum
// Guid:2d6e5f8a-0f7c-4e9d-b1ca-5c6d7e8f9a0b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 聊天消息类型
/// </summary>
public enum ChatMessageType
{
    /// <summary>
    /// 文本消息（含 emoji）
    /// </summary>
    [Description("文本")]
    Text = 1,

    /// <summary>
    /// 图片消息（FileId → SysFile，前端内联预览）
    /// </summary>
    [Description("图片")]
    Image = 2,

    /// <summary>
    /// 文件消息（FileId → SysFile，前端文件卡片下载）
    /// </summary>
    [Description("文件")]
    File = 3,

    /// <summary>
    /// 系统提示（入群/退群/撤回等时间线提示，SenderUserId=0）
    /// </summary>
    [Description("系统提示")]
    System = 99
}
