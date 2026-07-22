// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 系统消息查询服务
/// </summary>
public interface IMessageRecordQueryService
{
    /// <summary>
    /// 获取邮件
    /// </summary>
    Task<SysEmail> GetEmailOrThrowAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取短信
    /// </summary>
    Task<SysSms> GetSmsOrThrowAsync(long id, CancellationToken cancellationToken = default);
}
