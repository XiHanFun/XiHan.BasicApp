#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOperationRepository
// Guid:9e608807-522c-4f52-8f6f-f6952f4ad53d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 操作仓储接口
/// </summary>
public interface IOperationRepository : ISaasAggregateRepository<SysOperation>
{
    /// <summary>
    /// 根据当前租户和操作编码获取操作
    /// </summary>
    Task<SysOperation?> GetByCodeAsync(string operationCode, CancellationToken cancellationToken = default);
}
