#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFileStorage.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 文件存储状态枚举
/// </summary>
public enum FileStorageStatus
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
    /// 同步中
    /// </summary>
    [Description("同步中")]
    Syncing = 3,

    /// <summary>
    /// 同步失败
    /// </summary>
    [Description("同步失败")]
    SyncFailed = 4,

    /// <summary>
    /// 待验证
    /// </summary>
    [Description("待验证")]
    PendingVerification = 5,

    /// <summary>
    /// 验证失败
    /// </summary>
    [Description("验证失败")]
    VerificationFailed = 6,

    /// <summary>
    /// 已过期
    /// </summary>
    [Description("已过期")]
    Expired = 7,

    /// <summary>
    /// 已删除
    /// </summary>
    [Description("已删除")]
    Deleted = 8,

    /// <summary>
    /// 不可用
    /// </summary>
    [Description("不可用")]
    Unavailable = 9
}

/// <summary>
/// 文件存储类型枚举
/// </summary>
public enum FileStorageType
{
    /// <summary>
    /// 本地存储
    /// </summary>
    [Description("本地存储")]
    Local = 0,

    /// <summary>
    /// 阿里云OSS
    /// </summary>
    [Description("阿里云OSS")]
    AliyunOss = 1,

    /// <summary>
    /// 腾讯云COS
    /// </summary>
    [Description("腾讯云COS")]
    TencentCos = 2,

    /// <summary>
    /// MinIO
    /// </summary>
    [Description("MinIO")]
    Minio = 3,

    /// <summary>
    /// FTP
    /// </summary>
    [Description("FTP")]
    Ftp = 10,

    /// <summary>
    /// SFTP
    /// </summary>
    [Description("SFTP")]
    Sftp = 11,

    /// <summary>
    /// WebDAV
    /// </summary>
    [Description("WebDAV")]
    WebDav = 12,

    /// <summary>
    /// 自定义存储
    /// </summary>
    [Description("自定义存储")]
    Custom = 99
}
