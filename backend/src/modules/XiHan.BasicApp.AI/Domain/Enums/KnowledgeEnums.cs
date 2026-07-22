// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
