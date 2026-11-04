// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 按权限码的批量结果（授予 / 撤销）
/// </summary>
/// <param name="Succeeded">成功的权限码</param>
/// <param name="Failed">失败的权限码与错误码</param>
public sealed record PermissionBatchResult(IReadOnlyList<string> Succeeded, IReadOnlyList<PermissionFailure> Failed);

/// <summary>
/// 一个失败项
/// </summary>
/// <param name="Permission">权限码</param>
/// <param name="Code">错误码</param>
/// <param name="Message">失败原因，已按这次调用的区域本地化，可直接显示</param>
public sealed record PermissionFailure(string Permission, string Code, string Message)
{
    /// <summary>
    /// 由失败结果得出错误码与文案
    /// </summary>
    public static PermissionFailure From(string permission, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new PermissionFailure(permission, error.Code, error.LocalizedMessage());
    }
}
