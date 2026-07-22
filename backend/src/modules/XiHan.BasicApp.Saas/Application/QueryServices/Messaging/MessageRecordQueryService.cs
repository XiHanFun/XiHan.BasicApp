// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 系统消息查询服务实现
/// </summary>
public sealed class MessageRecordQueryService
    : IMessageRecordQueryService
{
    private readonly IEmailRepository _emailRepository;

    private readonly ISmsRepository _smsRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageRecordQueryService(IEmailRepository emailRepository, ISmsRepository smsRepository)
    {
        _emailRepository = emailRepository;
        _smsRepository = smsRepository;
    }

    /// <inheritdoc />
    public async Task<SysEmail> GetEmailOrThrowAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统邮件主键必须大于 0。");
        }

        return await _emailRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统邮件记录不存在。");
    }

    /// <inheritdoc />
    public async Task<SysSms> GetSmsOrThrowAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统短信主键必须大于 0。");
        }

        return await _smsRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统短信记录不存在。");
    }
}
