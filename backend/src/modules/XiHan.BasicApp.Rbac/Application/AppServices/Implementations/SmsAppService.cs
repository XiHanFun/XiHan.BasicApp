#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsAppService
// Guid:3887a998-5f2f-4651-a564-09338886f9e7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:51:00
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
/// 短信应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class SmsAppService
    : CrudApplicationServiceBase<SysSms, SmsDto, long, SmsCreateDto, SmsUpdateDto, BasicAppPRDto>,
        ISmsAppService
{
    private readonly ISmsRepository _smsRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SmsAppService(ISmsRepository smsRepository)
        : base(smsRepository)
    {
        _smsRepository = smsRepository;
    }

    /// <summary>
    /// 获取待发送短信
    /// </summary>
    public async Task<IReadOnlyList<SmsDto>> GetPendingAsync(int maxCount = 100, long? tenantId = null)
    {
        var entities = await _smsRepository.GetPendingSmsAsync(maxCount, tenantId);
        return entities.Select(static entity => entity.Adapt<SmsDto>()!).ToArray();
    }

    /// <summary>
    /// 创建短信
    /// </summary>
    public override async Task<SmsDto> CreateAsync(SmsCreateDto input)
    {
        input.ValidateAnnotations();
        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新短信
    /// </summary>
    public override async Task<SmsDto> UpdateAsync(SmsUpdateDto input)
    {
        input.ValidateAnnotations();
        return await base.UpdateAsync(input);
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    protected override Task<SysSms> MapDtoToEntityAsync(SmsCreateDto createDto)
    {
        var entity = new SysSms
        {
            TenantId = createDto.TenantId,
            SenderId = createDto.SenderId,
            ReceiverId = createDto.ReceiverId,
            SmsType = createDto.SmsType,
            ToPhone = createDto.ToPhone.Trim(),
            Content = createDto.Content.Trim(),
            TemplateId = createDto.TemplateId,
            TemplateParams = createDto.TemplateParams,
            Provider = createDto.Provider,
            ScheduledTime = createDto.ScheduledTime,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    protected override Task MapDtoToEntityAsync(SmsUpdateDto updateDto, SysSms entity)
    {
        entity.SenderId = updateDto.SenderId;
        entity.ReceiverId = updateDto.ReceiverId;
        entity.SmsType = updateDto.SmsType;
        entity.ToPhone = updateDto.ToPhone.Trim();
        entity.Content = updateDto.Content.Trim();
        entity.TemplateId = updateDto.TemplateId;
        entity.TemplateParams = updateDto.TemplateParams;
        entity.Provider = updateDto.Provider;
        entity.SmsStatus = updateDto.SmsStatus;
        entity.ScheduledTime = updateDto.ScheduledTime;
        entity.SendTime = updateDto.SendTime;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }
}
