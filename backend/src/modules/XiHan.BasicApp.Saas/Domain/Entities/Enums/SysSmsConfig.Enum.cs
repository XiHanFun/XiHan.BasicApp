#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSmsConfig.Enum
// Guid:0d7b3f28-6a51-4c94-8e07-2b9f5c1d8a63
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
