// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 引用一批权限码的数据
/// </summary>
/// <remarks>
/// 存着权限码的各包各实现一份：Access（角色与用户授予、授权规则、权限条件、委托、互斥约束、运营节点的门控权限码、待审的授权申请）、Tenancy（套餐白名单）
/// 插件卸载前列出来给平台确认，卸载时在同一个工作单元里清掉；操作与审计日志里的权限码是留档，不算引用
/// </remarks>
public interface IPermissionReferences
{
    /// <summary>
    /// 列出引用
    /// </summary>
    /// <param name="codes">权限码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>引用；某一侧的库读不出即失败，不当作没有</returns>
    Task<Result<IReadOnlyList<PermissionReference>>> FindAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken);

    /// <summary>
    /// 清掉平台库里的引用
    /// </summary>
    /// <remarks>
    /// 在调用方的读写工作单元里；缓存在提交之后失效。授权申请是留档、不在这里改
    /// </remarks>
    /// <param name="codes">权限码</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<Result> RemoveAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken);
}

/// <summary>
/// 一条对权限码的引用
/// </summary>
/// <param name="Kind">引用的种类</param>
/// <param name="Side">所属一侧：<c>platform</c>、租户 ID，或 <c>*</c>（对每个租户都生效的授予）</param>
/// <param name="Subject">引用方：角色编码、用户 ID、套餐编码、菜单编码、规则编码等</param>
/// <param name="Permission">被引用的权限码</param>
public sealed record PermissionReference(PermissionReferenceKind Kind, string Side, string Subject, string Permission);

/// <summary>
/// 引用权限码的种类
/// </summary>
public enum PermissionReferenceKind
{
    /// <summary>
    /// 角色的授予
    /// </summary>
    RoleGrant = 0,

    /// <summary>
    /// 用户直授
    /// </summary>
    UserGrant = 1,

    /// <summary>
    /// 授予的生效时段或拒绝规则
    /// </summary>
    PermissionRule = 2,

    /// <summary>
    /// 授予上的条件
    /// </summary>
    PermissionCondition = 3,

    /// <summary>
    /// 按权限码的委托
    /// </summary>
    Delegation = 4,

    /// <summary>
    /// 互斥约束规则的一项
    /// </summary>
    ConstraintRule = 5,

    /// <summary>
    /// 运营建的目录、外链或内嵌节点的门控权限码
    /// </summary>
    Menu = 6,

    /// <summary>
    /// 套餐白名单
    /// </summary>
    Edition = 7,

    /// <summary>
    /// 待审的授权申请（留档不清，批准时会因权限码不存在被拒）
    /// </summary>
    PermissionRequest = 8,
}
