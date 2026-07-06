#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:KnowledgeEnums
// Guid:1a2870aa-9d67-48d3-89a6-78cbe64c89c6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.AI.Domain.Enums;

/// <summary>
/// 知识文档来源类型
/// </summary>
public enum KnowledgeSourceType
{
    /// <summary>
    /// 粘贴文本
    /// </summary>
    [Description("粘贴文本")]
    PasteText = 0,

    /// <summary>
    /// 上传文件（前端读取为文本后提交）
    /// </summary>
    [Description("上传文件")]
    UploadFile = 1
}

/// <summary>
/// 知识文档索引状态
/// </summary>
public enum KnowledgeIndexStatus
{
    /// <summary>
    /// 待索引
    /// </summary>
    [Description("待索引")]
    Pending = 0,

    /// <summary>
    /// 已索引
    /// </summary>
    [Description("已索引")]
    Indexed = 1,

    /// <summary>
    /// 索引失败
    /// </summary>
    [Description("失败")]
    Failed = 2
}
