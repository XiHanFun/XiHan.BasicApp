// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 短信服务商类型
/// </summary>
public enum SmsProviderType
{
    /// <summary>
    /// 阿里云
    /// </summary>
    [Description("阿里云")]
    Aliyun = 0,

    /// <summary>
    /// 腾讯云
    /// </summary>
    [Description("腾讯云")]
    TencentCloud = 1
}
