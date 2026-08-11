// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Chat.Domain.Entities;

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
    /// 语音消息（单个音频附件，附件带时长，气泡内播放）
    /// </summary>
    [Description("语音")]
    Voice = 3,

    /// <summary>
    /// 文件消息（FileId → SysFile，前端文件卡片下载）
    /// </summary>
    [Description("文件")]
    File = 4,

    /// <summary>
    /// AI 助手回复（SenderUserId 为助手主键，内容按 Markdown 渲染，不可撤回/编辑）
    /// </summary>
    [Description("助手回复")]
    Assistant = 5,

    /// <summary>
    /// 系统提示（入群/退群/撤回等时间线提示，SenderUserId=0）
    /// </summary>
    [Description("系统提示")]
    System = 99
}
