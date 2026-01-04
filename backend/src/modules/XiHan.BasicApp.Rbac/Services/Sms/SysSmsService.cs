#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSmsService
// Guid:y1z2a3b4-c5d6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.Sms;
using XiHan.BasicApp.Rbac.Services.Sms.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Sms;

/// <summary>
/// 系统短信服务实现
/// </summary>
public class SysSmsService : CrudApplicationServiceBase<SysSms, SmsDto, XiHanBasicAppIdType, CreateSmsDto, UpdateSmsDto>, ISysSmsService
{
    private readonly ISysSmsRepository _smsRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysSmsService(ISysSmsRepository smsRepository) : base(smsRepository)
    {
        _smsRepository = smsRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据短信状态获取短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetByStatusAsync(SmsStatus smsStatus)
    {
        var smsList = await _smsRepository.GetByStatusAsync(smsStatus);
        return smsList.ToDto();
    }

    /// <summary>
    /// 根据短信类型获取短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetByTypeAsync(SmsType smsType)
    {
        var smsList = await _smsRepository.GetByTypeAsync(smsType);
        return smsList.ToDto();
    }

    /// <summary>
    /// 根据手机号获取短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetByToPhoneAsync(string toPhone)
    {
        var smsList = await _smsRepository.GetByToPhoneAsync(toPhone);
        return smsList.ToDto();
    }

    /// <summary>
    /// 根据发送者ID获取短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetBySenderIdAsync(XiHanBasicAppIdType senderId)
    {
        var smsList = await _smsRepository.GetBySenderIdAsync(senderId);
        return smsList.ToDto();
    }

    /// <summary>
    /// 根据接收者ID获取短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetByReceiverIdAsync(XiHanBasicAppIdType receiverId)
    {
        var smsList = await _smsRepository.GetByReceiverIdAsync(receiverId);
        return smsList.ToDto();
    }

    /// <summary>
    /// 获取待发送的短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetPendingSmsAsync(int count = 100)
    {
        var smsList = await _smsRepository.GetPendingSmsAsync(count);
        return smsList.ToDto();
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<SmsDto> MapToEntityDtoAsync(SysSms entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 SmsDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysSms> MapToEntityAsync(SmsDto dto)
    {
        var entity = new SysSms
        {
            TenantId = dto.TenantId,
            SenderId = dto.SenderId,
            ReceiverId = dto.ReceiverId,
            SmsType = dto.SmsType,
            ToPhone = dto.ToPhone,
            Content = dto.Content,
            TemplateId = dto.TemplateId,
            TemplateParams = dto.TemplateParams,
            Provider = dto.Provider,
            SmsStatus = dto.SmsStatus,
            ScheduledTime = dto.ScheduledTime,
            SendTime = dto.SendTime,
            ProviderMessageId = dto.ProviderMessageId,
            RetryCount = dto.RetryCount,
            MaxRetryCount = dto.MaxRetryCount,
            ErrorMessage = dto.ErrorMessage,
            Cost = dto.Cost,
            BusinessType = dto.BusinessType,
            BusinessId = dto.BusinessId,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 SmsDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(SmsDto dto, SysSms entity)
    {
        entity.SmsStatus = dto.SmsStatus;
        entity.ScheduledTime = dto.ScheduledTime;
        entity.SendTime = dto.SendTime;
        entity.ProviderMessageId = dto.ProviderMessageId;
        entity.RetryCount = dto.RetryCount;
        entity.ErrorMessage = dto.ErrorMessage;
        entity.Cost = dto.Cost;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysSms> MapToEntityAsync(CreateSmsDto createDto)
    {
        var entity = new SysSms
        {
            TenantId = createDto.TenantId,
            SenderId = createDto.SenderId,
            ReceiverId = createDto.ReceiverId,
            SmsType = createDto.SmsType,
            ToPhone = createDto.ToPhone,
            Content = createDto.Content,
            TemplateId = createDto.TemplateId,
            TemplateParams = createDto.TemplateParams,
            Provider = createDto.Provider,
            ScheduledTime = createDto.ScheduledTime,
            MaxRetryCount = createDto.MaxRetryCount,
            BusinessType = createDto.BusinessType,
            BusinessId = createDto.BusinessId,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateSmsDto updateDto, SysSms entity)
    {
        if (updateDto.SmsStatus.HasValue) entity.SmsStatus = updateDto.SmsStatus.Value;
        if (updateDto.ScheduledTime.HasValue) entity.ScheduledTime = updateDto.ScheduledTime;
        if (updateDto.ErrorMessage != null) entity.ErrorMessage = updateDto.ErrorMessage;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
