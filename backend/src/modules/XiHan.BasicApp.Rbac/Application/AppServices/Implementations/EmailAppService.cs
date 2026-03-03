#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailAppService
// Guid:be6f084f-fcc7-4457-89f0-f724d85716f9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.AppServices.Implementations;

/// <summary>
/// 邮件应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class EmailAppService
    : CrudApplicationServiceBase<SysEmail, EmailDto, long, EmailCreateDto, EmailUpdateDto, BasicAppPRDto>,
        IEmailAppService
{
    private readonly IEmailRepository _emailRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EmailAppService(IEmailRepository emailRepository)
        : base(emailRepository)
    {
        _emailRepository = emailRepository;
    }

    /// <summary>
    /// 获取待发送邮件
    /// </summary>
    public async Task<IReadOnlyList<EmailDto>> GetPendingAsync(int maxCount = 100, long? tenantId = null)
    {
        var entities = await _emailRepository.GetPendingEmailsAsync(maxCount, tenantId);
        return entities.Select(static entity => entity.Adapt<EmailDto>()!).ToArray();
    }

    /// <summary>
    /// 创建邮件
    /// </summary>
    public override async Task<EmailDto> CreateAsync(EmailCreateDto input)
    {
        input.ValidateAnnotations();
        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新邮件
    /// </summary>
    public override async Task<EmailDto> UpdateAsync(long id, EmailUpdateDto input)
    {
        input.ValidateAnnotations();
        return await base.UpdateAsync(id, input);
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    protected override Task<SysEmail> MapDtoToEntityAsync(EmailCreateDto createDto)
    {
        var entity = new SysEmail
        {
            TenantId = createDto.TenantId,
            SendUserId = createDto.SendUserId,
            ReceiveUserId = createDto.ReceiveUserId,
            EmailType = createDto.EmailType,
            FromEmail = createDto.FromEmail.Trim(),
            FromName = createDto.FromName,
            ToEmail = createDto.ToEmail.Trim(),
            CcEmail = createDto.CcEmail,
            BccEmail = createDto.BccEmail,
            Subject = createDto.Subject.Trim(),
            Content = createDto.Content,
            IsHtml = createDto.IsHtml,
            ScheduledTime = createDto.ScheduledTime,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    protected override Task MapDtoToEntityAsync(EmailUpdateDto updateDto, SysEmail entity)
    {
        entity.SendUserId = updateDto.SendUserId;
        entity.ReceiveUserId = updateDto.ReceiveUserId;
        entity.EmailType = updateDto.EmailType;
        entity.FromEmail = updateDto.FromEmail.Trim();
        entity.FromName = updateDto.FromName;
        entity.ToEmail = updateDto.ToEmail.Trim();
        entity.CcEmail = updateDto.CcEmail;
        entity.BccEmail = updateDto.BccEmail;
        entity.Subject = updateDto.Subject.Trim();
        entity.Content = updateDto.Content;
        entity.IsHtml = updateDto.IsHtml;
        entity.EmailStatus = updateDto.EmailStatus;
        entity.ScheduledTime = updateDto.ScheduledTime;
        entity.SendTime = updateDto.SendTime;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }
}
