#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMessageAppService
// Guid:0b36a7b1-fd7f-462f-908f-43c073b88540
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

public interface IMessageAppService : IApplicationService
{
    Task<EmailDetailDto> CreateEmailAsync(EmailCreateDto input, CancellationToken cancellationToken = default);

    Task<EmailDetailDto> UpdateEmailAsync(EmailUpdateDto input, CancellationToken cancellationToken = default);

    Task<EmailDetailDto> UpdateEmailStatusAsync(EmailStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteEmailAsync(long id, CancellationToken cancellationToken = default);

    Task<SmsDetailDto> CreateSmsAsync(SmsCreateDto input, CancellationToken cancellationToken = default);

    Task<SmsDetailDto> UpdateSmsAsync(SmsUpdateDto input, CancellationToken cancellationToken = default);

    Task<SmsDetailDto> UpdateSmsStatusAsync(SmsStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteSmsAsync(long id, CancellationToken cancellationToken = default);
}
