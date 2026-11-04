// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// 部门树节点
/// </summary>
/// <param name="Department">部门</param>
/// <param name="MemberCount">直接成员数（不含子部门）</param>
/// <param name="Children">子部门，按排序号与名称</param>
public sealed record DepartmentNode(DepartmentResponse Department, long MemberCount, IReadOnlyList<DepartmentNode> Children);

/// <summary>
/// <c>departments.tree</c> 的响应：当前租户的部门树
/// </summary>
/// <param name="Items">顶层节点</param>
/// <param name="Total">树里的部门数（带关键字时是命中的部门数）</param>
public sealed record DepartmentTreeResponse(IReadOnlyList<DepartmentNode> Items, long Total);

/// <summary>
/// <c>departments.tree</c> 的请求
/// </summary>
/// <param name="Keyword">关键字，匹配编码、名称、电话、邮箱、地址与说明；为空即整棵树</param>
public sealed record DepartmentTreeRequest([StringLength(128)] string? Keyword = null);

/// <summary>
/// <c>departments.get</c> 的响应
/// </summary>
/// <param name="Department">部门</param>
/// <param name="Ancestors">祖先链，由近到远</param>
/// <param name="LeaderUserName">负责人登录名；没有负责人为空</param>
/// <param name="ChildCount">直接子部门数</param>
/// <param name="MemberCount">直接成员数</param>
public sealed record DepartmentDetailResponse(DepartmentResponse Department, IReadOnlyList<DepartmentResponse> Ancestors, string? LeaderUserName, long ChildCount, long MemberCount);
