// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Operations.ConditionHits;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Queries;

/// <summary>
/// 条件命中查询。
/// </summary>
/// <remarks>
/// 关键字匹配策略编码、策略名称与路径；另有筛选 <c>source</c>、<c>action</c>（eq / in）、<c>phase</c>、<c>isBlocked</c>、<c>policyId</c>、<c>policyCode</c>（eq / contains）、
/// <c>path</c>（contains）；另有排序 <c>hitCount</c>、<c>policyCode</c>。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class ConditionHitQueries(SqlSugarUnitOfWork unitOfWork) : AuditLogQueries<SysConditionHit, ConditionHitResponse, ConditionHitResponse>(unitOfWork)
{
    /// <inheritdoc />
    protected override ISugarQueryable<SysConditionHit> ApplyKeyword(ISugarQueryable<SysConditionHit> query, string keyword)
        => query.Where(e => e.PolicyCode.Contains(keyword) || (e.PolicyName != null && e.PolicyName.Contains(keyword)) || (e.Path != null && e.Path.Contains(keyword)));

    /// <inheritdoc />
    protected override Result<ISugarQueryable<SysConditionHit>> ApplyFilter(ISugarQueryable<SysConditionHit> query, string field, string op, string value)
        => (field, op) switch
        {
            ("source", "eq") => TryParseEnum<ConditionHitSource>(value, out var source) ? Result.Success(query.Where(e => e.Source == source)) : InvalidValue(field, value),
            ("action", "eq") => TryParseEnum<ConditionHitAction>(value, out var action) ? Result.Success(query.Where(e => e.Action == action)) : InvalidValue(field, value),
            ("action", "in") => ParseEnums<ConditionHitAction>(value) is { } actions ? Result.Success(query.Where(e => actions.Contains(e.Action))) : InvalidValue(field, value),
            ("phase", "eq") => TryParseEnum<ConditionHitPhase>(value, out var phase) ? Result.Success(query.Where(e => e.Phase == phase)) : InvalidValue(field, value),
            ("isBlocked", "eq") => bool.TryParse(value, out var blocked) ? Result.Success(query.Where(e => e.IsBlocked == blocked)) : InvalidValue(field, value),
            ("policyId", "eq") => Guid.TryParse(value, out var policyId) ? Result.Success(query.Where(e => e.PolicyId == policyId)) : InvalidValue(field, value),
            ("policyCode", "eq") => Result.Success(query.Where(e => e.PolicyCode == value)),
            ("policyCode", "contains") => Result.Success(query.Where(e => e.PolicyCode.Contains(value))),
            ("path", "contains") => Result.Success(query.Where(e => e.Path!.Contains(value))),
            _ => Unsupported(),
        };

    /// <inheritdoc />
    protected override ISugarQueryable<SysConditionHit>? ApplySort(ISugarQueryable<SysConditionHit> query, string field, OrderByType direction)
        => field switch
        {
            "hitCount" => query.OrderBy(e => e.HitCount, direction),
            "policyCode" => query.OrderBy(e => e.PolicyCode, direction),
            _ => null,
        };

    /// <inheritdoc />
    protected override ConditionHitResponse MapItem(SysConditionHit entity) => ConditionHitResponse.From(entity);

    /// <inheritdoc />
    protected override ConditionHitResponse MapDetail(SysConditionHit entity) => ConditionHitResponse.From(entity);
}
