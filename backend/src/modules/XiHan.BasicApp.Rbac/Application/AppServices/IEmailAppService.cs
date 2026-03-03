#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IEmailAppService
// Guid:9016588a-7a66-4f16-8470-cc6bff0bf846
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:37:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.AppServices;

/// <summary>
/// 邮件应用服务
/// </summary>
public interface IEmailAppService
    : ICrudApplicationService<EmailDto, long, EmailCreateDto, EmailUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 获取待发送邮件
    /// </summary>
    Task<IReadOnlyList<EmailDto>> GetPendingAsync(int maxCount = 100, long? tenantId = null);
}
