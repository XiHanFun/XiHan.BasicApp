// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Identity.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 分页列出用户
/// </summary>
/// <remarks>
/// 请求带 <c>PageRequest</c>（嵌套对象）因而落到 <c>POST /api/users:list</c>
/// 租户主体看本租户注册的用户、外部成员（别处注册、生效中的普通成员，<c>isExternalMember</c> 为真）与运维支持成员（<c>isSupportMember</c> 也为真，账号只读、只能配角色，别的选人入口用 <c>isSupportMember eq false</c> 筛掉）
/// 只读目录类操作，同时作为 MCP / AI 工具暴露；与 <c>users.get</c> 一起是内部服务查用户目录的 gRPC 面
/// </remarks>
/// <param name="queries">用户查询（直接用 SqlSugar 客户端，经 <c>TenantScopedQuery</c> 起手）</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Grpc | Expose.Mcp | Expose.Ai, Permission = IdentityPermissions.UsersRead)]
public sealed class ListUsers(UserQueries queries) : IOperation<PageRequest, PageResult<UserListItemResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "users.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<UserListItemResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
