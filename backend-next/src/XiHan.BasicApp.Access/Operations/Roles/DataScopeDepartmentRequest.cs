// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 自定义数据范围里的一个部门（请求与响应共用）。
/// </summary>
/// <param name="DepartmentId">部门主键（当前租户）。</param>
/// <param name="IncludeChildren">是否连同子部门。</param>
public sealed record DataScopeDepartmentRequest(Guid DepartmentId, bool IncludeChildren = false);
