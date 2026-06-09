#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDictDomainService
// Guid:d767e26a-2e90-481c-b484-b94d687af8e2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 字典领域服务
/// </summary>
public interface IDictDomainService
{
    /// <summary>
    /// 创建字典
    /// </summary>
    Task<DictCommandResult> CreateDictAsync(DictCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建字典项
    /// </summary>
    Task<DictItemCommandResult> CreateDictItemAsync(DictItemCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字典
    /// </summary>
    Task DeleteDictAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字典项
    /// </summary>
    Task DeleteDictItemAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字典
    /// </summary>
    Task<DictCommandResult> UpdateDictAsync(DictUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字典项
    /// </summary>
    Task<DictItemCommandResult> UpdateDictItemAsync(DictItemUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字典项状态
    /// </summary>
    Task<DictItemCommandResult> UpdateDictItemStatusAsync(DictItemStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字典状态
    /// </summary>
    Task<DictCommandResult> UpdateDictStatusAsync(DictStatusChangeCommand command, CancellationToken cancellationToken = default);
}
