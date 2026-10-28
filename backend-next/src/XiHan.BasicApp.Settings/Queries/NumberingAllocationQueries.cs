// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Operations.NumberingRules;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Queries;

/// <summary>
/// 发号记录查询（SqlSugar 直查，平台库）：一条规则的记录按发号时刻倒序；关键字匹配幂等键、业务类型与业务单据。
/// </summary>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class NumberingAllocationQueries(SqlSugarUnitOfWork unitOfWork)
{
    /// <summary>
    /// 分页列出一条规则的发号记录；<paramref name="requester"/> 不为空时只列这一侧发起的。
    /// </summary>
    public async ValueTask<Result<PageResult<NumberingAllocationResponse>>> ListAsync(Guid ruleId, string? requester, string? keyword, PageInfo page, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysNumberingAllocation>(unitOfWork.ConnectionFor(typeof(SysNumberingAllocation)), context).Where(a => a.RuleId == ruleId);
        if (requester is not null)
        {
            query = query.Where(a => a.RequestTenantId == requester);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var text = keyword.Trim();
            query = query.Where(a => a.IdempotencyKey.Contains(text) || (a.BusinessType != null && a.BusinessType.Contains(text)) || (a.BusinessId != null && a.BusinessId.Contains(text)));
        }

        var total = new RefAsync<int>();
        var rows = await query.OrderBy(a => a.GeneratedAt, OrderByType.Desc).ToPageListAsync(page.Index, page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<NumberingAllocationResponse>(rows.Select(NumberingAllocationResponse.From).ToList(), total.Value, page);
    }
}
