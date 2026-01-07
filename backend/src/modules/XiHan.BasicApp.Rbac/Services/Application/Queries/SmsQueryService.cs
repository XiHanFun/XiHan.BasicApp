#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsQueryService
// Guid:d0e1f2a3-b4c5-4d6e-7f8a-9b0c1d2e3f4a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 短信查询服务（处理短信的读操作 - CQRS）
/// </summary>
public class SmsQueryService : ApplicationServiceBase
{
    private readonly ISmsRepository _smsRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SmsQueryService(ISmsRepository smsRepository)
    {
        _smsRepository = smsRepository;
    }

    /// <summary>
    /// 根据ID获取短信
    /// </summary>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var sms = await _smsRepository.GetByIdAsync(id);
        return sms?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据手机号获取短信列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByPhoneNumberAsync(string phoneNumber)
    {
        var smsList = await _smsRepository.GetByPhoneNumberAsync(phoneNumber);
        return smsList.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据发送状态获取短信列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetBySendStatusAsync(SmsStatus status)
    {
        var smsList = await _smsRepository.GetBySendStatusAsync(status);
        return smsList.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取待发送的短信列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetPendingSmsAsync()
    {
        var smsList = await _smsRepository.GetPendingSmsAsync();
        return smsList.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取发送失败的短信列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetFailedSmsAsync()
    {
        var smsList = await _smsRepository.GetFailedSmsAsync();
        return smsList.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _smsRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
