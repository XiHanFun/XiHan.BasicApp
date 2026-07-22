// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
