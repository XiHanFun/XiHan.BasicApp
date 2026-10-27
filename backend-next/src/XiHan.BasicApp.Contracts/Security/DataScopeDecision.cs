// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 当前主体的数据范围判定（方案 5.6）：要么不受限，要么只看得到 <see cref="UserIds"/> 里的用户（含自己）与 <see cref="DepartmentIds"/> 里的部门。
/// 列表按「归属人列」或「部门列」套用（<c>Runtime.DataScopeQuery.ApplyDataScope</c>）；按 ID 取单条时用 <see cref="Allows"/>。
/// </summary>
/// <param name="Unrestricted">不受限（平台侧、角色数据范围为全部、或没有应用角色）。</param>
/// <param name="UserIds">可见的用户主键（受限时至少含自己）。</param>
/// <param name="DepartmentIds">可见的部门主键（受限时可能为空）。</param>
public sealed record DataScopeDecision(bool Unrestricted, IReadOnlySet<Guid> UserIds, IReadOnlySet<Guid> DepartmentIds)
{
    private static readonly HashSet<Guid> _none = [];

    /// <summary>
    /// 不受限。
    /// </summary>
    public static DataScopeDecision Everything { get; } = new(true, _none, _none);

    /// <summary>
    /// 某个用户是否在范围内。
    /// </summary>
    public bool Allows(Guid userId) => Unrestricted || UserIds.Contains(userId);
}
