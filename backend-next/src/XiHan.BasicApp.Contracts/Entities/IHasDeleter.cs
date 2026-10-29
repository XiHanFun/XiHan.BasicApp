// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 记录软删除操作人的实体。
/// </summary>
/// <remarks>
/// 与框架 <c>ISoftDeletable</c> 搭配：删除标记与时间由框架维护，操作人由 <c>Runtime</c> 在写库时填充。
/// </remarks>
public interface IHasDeleter
{
    /// <summary>
    /// 删除人（主体 ID）。
    /// </summary>
    /// <remarks>
    /// 未删除时为 <see langword="null"/>。
    /// </remarks>
    string? DeletedBy { get; set; }
}
