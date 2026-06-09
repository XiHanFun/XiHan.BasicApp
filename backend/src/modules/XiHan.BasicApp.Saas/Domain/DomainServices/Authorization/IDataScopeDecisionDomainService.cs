#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDataScopeDecisionDomainService
// Guid:b49dbe7f-b661-4a38-bc4e-6908977cf165
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 数据范围裁决领域服务
/// </summary>
public interface IDataScopeDecisionDomainService
{
    /// <summary>
    /// 合并数据范围授权快照
    /// </summary>
    /// <param name="grants">数据范围授权快照集合</param>
    /// <param name="userDepartmentIds">当前用户有效部门ID集合</param>
    /// <param name="now">当前时间</param>
    /// <returns>数据范围裁决结果</returns>
    DataScopeDecision Decide(
        IEnumerable<DataScopeGrantSnapshot> grants,
        IEnumerable<long> userDepartmentIds,
        DateTimeOffset now);
}
