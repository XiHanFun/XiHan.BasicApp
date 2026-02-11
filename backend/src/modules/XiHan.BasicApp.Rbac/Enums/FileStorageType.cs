#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileStorageType
// Guid:796848f0-d00c-4a85-8930-ead27d620960
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 文件存储类型枚举
/// </summary>
public enum FileStorageType
{
    /// <summary>
    /// 本地存储
    /// </summary>
    Local = 0,

    /// <summary>
    /// 阿里云OSS
    /// </summary>
    AliyunOss = 1,

    /// <summary>
    /// 腾讯云COS
    /// </summary>
    TencentCos = 2,

    /// <summary>
    /// MinIO
    /// </summary>
    Minio = 3,

    /// <summary>
    /// FTP
    /// </summary>
    Ftp = 10,

    /// <summary>
    /// SFTP
    /// </summary>
    Sftp = 11,

    /// <summary>
    /// WebDAV
    /// </summary>
    WebDav = 12,

    /// <summary>
    /// 自定义存储
    /// </summary>
    Custom = 99
}
