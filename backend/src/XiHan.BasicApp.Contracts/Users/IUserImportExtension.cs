// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Users;

/// <summary>
/// 导入里的一行
/// </summary>
/// <param name="Index">在这次请求里的序号（从 0 开始）</param>
/// <param name="UserId">分配给这一行的用户 ID</param>
/// <param name="UserName">登录名</param>
/// <param name="DepartmentCode">要加入的部门编码，没填为空</param>
/// <param name="RoleCodes">要指派的角色码，没填为空列表</param>
public sealed record UserImportLine(int Index, Guid UserId, string UserName, string? DepartmentCode, IReadOnlyList<string> RoleCodes);

/// <summary>
/// 一行不能导入的原因
/// </summary>
/// <param name="Index">行在请求里的序号</param>
/// <param name="Field">出问题的导入列（请求里的字段名）</param>
/// <param name="Reason">原因</param>
public sealed record UserImportIssue(int Index, string Field, Error Reason);

/// <summary>
/// 导入用户时一并写入的指派（Organization 写部门、Access 写角色）
/// </summary>
/// <remarks>
/// Identity 的 <c>users.import</c> 建用户，归上层包的列由各包实现：先逐行校验（含导入人有没有对应的权限），用户写完后再写入，与建用户同一事务
/// 各实现按登记顺序依次校验，后一个只看前面都通过的行；只有通过了全部校验的行才会写入，所以写入时的失败按整批失败处理（事务回滚）
/// </remarks>
public interface IUserImportExtension
{
    /// <summary>
    /// 逐行校验
    /// </summary>
    /// <param name="lines">已通过 Identity 校验的行（用户还没写入），实现只看自己那一列有值的行</param>
    /// <param name="context">调用上下文</param>
    /// <returns>不能导入的行；导入人缺少权限这类整批都做不了的情况返回失败</returns>
    Task<Result<IReadOnlyList<UserImportIssue>>> ValidateAsync(IReadOnlyList<UserImportLine> lines, InvocationContext context);

    /// <summary>
    /// 写入
    /// </summary>
    /// <param name="lines">全部校验都通过、用户已写入的行</param>
    /// <param name="context">调用上下文</param>
    /// <returns>写入结果；失败即整批回滚</returns>
    Task<Result> ApplyAsync(IReadOnlyList<UserImportLine> lines, InvocationContext context);
}
