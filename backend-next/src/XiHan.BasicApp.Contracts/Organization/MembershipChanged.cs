// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Contracts.Organization;

/// <summary>
/// 用户的部门归属变了。
/// </summary>
/// <remarks>
/// 领域事件，主题 <see cref="Topics.OrganizationMembershipChanged"/>，信封带发生时的租户。
/// 订阅方以消息里的用户与部门为线索、按处理时的归属核对（同一用户进出部门的消息到达顺序不保证与发生顺序一致）。
/// </remarks>
/// <param name="UserId">用户。</param>
/// <param name="DepartmentId">部门。</param>
/// <param name="Joined">加入为真，移出为假。</param>
/// <param name="OccurredAt">发生时间（UTC）。</param>
public sealed record MembershipChanged(Guid UserId, Guid DepartmentId, bool Joined, DateTimeOffset OccurredAt) : IDomainEvent;
