#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMessageTemplateAppService
// Guid:7d8cf411-58ad-4eff-ec61-d4fcb7c88efc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 消息模板命令应用服务接口
/// </summary>
public interface IMessageTemplateAppService : IApplicationService
{
    /// <summary>
    /// 创建消息模板
    /// </summary>
    Task<MessageTemplateDetailDto> CreateMessageTemplateAsync(MessageTemplateCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新消息模板
    /// </summary>
    Task<MessageTemplateDetailDto> UpdateMessageTemplateAsync(MessageTemplateUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新消息模板状态
    /// </summary>
    Task<MessageTemplateDetailDto> UpdateMessageTemplateStatusAsync(MessageTemplateStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除消息模板
    /// </summary>
    Task DeleteMessageTemplateAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 消息模板查询应用服务接口
/// </summary>
public interface IMessageTemplateQueryService : IApplicationService
{
    /// <summary>
    /// 获取消息模板分页列表
    /// </summary>
    Task<XiHan.Framework.Domain.Shared.Paging.Dtos.PageResultDtoBase<MessageTemplateListItemDto>> GetMessageTemplatePageAsync(MessageTemplatePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取消息模板详情
    /// </summary>
    Task<MessageTemplateDetailDto?> GetMessageTemplateDetailAsync(long id, CancellationToken cancellationToken = default);
}
