#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IReviewLogRepository
// Guid:1040b109-8749-4355-a3c9-3fba6858c654
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 审批日志仓储接口
/// </summary>
public interface IReviewLogRepository : ISaasSplitRepository<SysReviewLog>
{
}
