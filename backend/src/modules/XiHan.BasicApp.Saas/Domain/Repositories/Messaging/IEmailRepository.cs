// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 邮件仓储接口
/// </summary>
public interface IEmailRepository : ISaasRepository<SysEmail>
{
    /// <summary>
    /// 获取待发送邮件
    /// </summary>
    Task<IReadOnlyList<SysEmail>> GetPendingEmailsAsync(int maxCount, CancellationToken cancellationToken = default);
}
