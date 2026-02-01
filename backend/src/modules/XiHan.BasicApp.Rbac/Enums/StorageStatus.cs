#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:StorageStatus
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 存储状态枚举
/// </summary>
public enum StorageStatus
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
    /// 同步中
    /// </summary>
    Syncing = 3,

    /// <summary>
    /// 同步失败
    /// </summary>
    SyncFailed = 4,

    /// <summary>
    /// 待验证
    /// </summary>
    PendingVerification = 5,

    /// <summary>
    /// 验证失败
    /// </summary>
    VerificationFailed = 6,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired = 7,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 8,

    /// <summary>
    /// 不可用
    /// </summary>
    Unavailable = 9
}
