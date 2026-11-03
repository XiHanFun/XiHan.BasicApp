// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 知识文档的来源类型
/// </summary>
public enum KnowledgeSourceType
{
    /// <summary>
    /// 粘贴的文本
    /// </summary>
    PasteText = 0,

    /// <summary>
    /// 上传的文件（前端读成文本提交）
    /// </summary>
    UploadFile = 1,
}

/// <summary>
/// 知识文档的索引状态
/// </summary>
public enum KnowledgeIndexStatus
{
    /// <summary>
    /// 待处理（进程在索引途中中断即停在这里，可以重建）
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已索引
    /// </summary>
    Indexed = 1,

    /// <summary>
    /// 索引失败
    /// </summary>
    Failed = 2,
}
