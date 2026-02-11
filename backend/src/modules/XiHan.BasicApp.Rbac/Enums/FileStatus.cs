#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileStatus
// Guid:f818fa1c-3e88-439d-b71c-f56eb100a861
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 文件状态枚举
/// </summary>
public enum FileStatus
{
    /// <summary>
    /// 正常
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 上传中
    /// </summary>
    Uploading = 1,

    /// <summary>
    /// 上传失败
    /// </summary>
    UploadFailed = 2,

    /// <summary>
    /// 处理中（转码、压缩等）
    /// </summary>
    Processing = 3,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 4,

    /// <summary>
    /// 已归档
    /// </summary>
    Archived = 5,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired = 6,

    /// <summary>
    /// 损坏
    /// </summary>
    Corrupted = 7,

    /// <summary>
    /// 违规（审核不通过）
    /// </summary>
    Violation = 8
}
