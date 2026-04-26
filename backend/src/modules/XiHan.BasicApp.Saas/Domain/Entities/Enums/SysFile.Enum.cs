#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFile.Enum
// Guid:3e20740f-3b70-4e21-a308-7a901089f07f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 文件状态枚举
/// </summary>
public enum FileStatus
{
    /// <summary>
    /// 正常
    /// </summary>
    [Description("正常")]
    Normal = 0,

    /// <summary>
    /// 上传中
    /// </summary>
    [Description("上传中")]
    Uploading = 1,

    /// <summary>
    /// 上传失败
    /// </summary>
    [Description("上传失败")]
    UploadFailed = 2,

    /// <summary>
    /// 处理中（转码、压缩等）
    /// </summary>
    [Description("处理中（转码、压缩等）")]
    Processing = 3,

    /// <summary>
    /// 已删除
    /// </summary>
    [Description("已删除")]
    Deleted = 4,

    /// <summary>
    /// 已归档
    /// </summary>
    [Description("已归档")]
    Archived = 5,

    /// <summary>
    /// 已过期
    /// </summary>
    [Description("已过期")]
    Expired = 6,

    /// <summary>
    /// 损坏
    /// </summary>
    [Description("损坏")]
    Corrupted = 7,

    /// <summary>
    /// 违规（审核不通过）
    /// </summary>
    [Description("违规（审核不通过）")]
    Violation = 8
}

/// <summary>
/// 文件类型枚举
/// </summary>
public enum FileType
{
    /// <summary>
    /// 图片
    /// </summary>
    [Description("图片")]
    Image = 0,

    /// <summary>
    /// 文档
    /// </summary>
    [Description("文档")]
    Document = 1,

    /// <summary>
    /// 视频
    /// </summary>
    [Description("视频")]
    Video = 2,

    /// <summary>
    /// 音频
    /// </summary>
    [Description("音频")]
    Audio = 3,

    /// <summary>
    /// 压缩包
    /// </summary>
    [Description("压缩包")]
    Archive = 4,

    /// <summary>
    /// 其他
    /// </summary>
    [Description("其他")]
    Other = 99
}
