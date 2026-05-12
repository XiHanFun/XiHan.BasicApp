#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysStorageConfig.Enum
// Guid:3b7c8d9e-1a2b-4c3d-5e6f-7a8b9c0d1e2f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 存储配置类型枚举
/// </summary>
public enum StorageConfigType
{
    /// <summary>
    /// 本地存储
    /// </summary>
    [Description("本地存储")]
    Local = 0,

    /// <summary>
    /// AWS S3
    /// </summary>
    [Description("AWS S3")]
    S3 = 1,

    /// <summary>
    /// 阿里云OSS
    /// </summary>
    [Description("阿里云OSS")]
    OSS = 2,

    /// <summary>
    /// 腾讯云COS
    /// </summary>
    [Description("腾讯云COS")]
    COS = 3,

    /// <summary>
    /// MinIO
    /// </summary>
    [Description("MinIO")]
    MinIO = 4
}
