#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMessageRecordQueryService
// Guid:327fe84f-8a27-4ae6-9378-b109d42b33b7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
