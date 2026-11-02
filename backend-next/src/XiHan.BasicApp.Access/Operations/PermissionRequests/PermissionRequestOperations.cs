// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.PermissionRequests;

/// <summary>
/// 授权申请列表。
/// </summary>
/// <remarks>
/// <c>POST /api/permission-requests:list</c>。照 4.x：这一侧全部的申请，关键字匹配申请原因与备注；筛选 <c>requestUserId</c>、<c>roleId</c>、<c>permissionCode</c>（<c>eq</c>）与 <c>status</c>（<c>eq</c> / <c>in</c>），
/// 排序 <c>createdAt</c>、<c>status</c>，缺省按提交时间降序。每条带审批进度；Workflow 关掉时进度为空、<c>approvalEnabled</c> 为假（方案 W1）。
/// </remarks>
/// <param name="service">授权申请。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PermissionRequestsRead)]
public sealed class ListPermissionRequests(PermissionRequestService service) : IOperation<PageRequest, PageResult<PermissionRequestResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-requests.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<PermissionRequestResponse>>> HandleAsync(PageRequest request, InvocationContext context)
        => await service.ListAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 授权申请详情。
/// </summary>
/// <remarks>
/// <c>GET /api/permission-requests/{id}</c>。与列表同一形态。
/// </remarks>
/// <param name="service">授权申请。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PermissionRequestsRead)]
public sealed class GetPermissionRequest(PermissionRequestService service) : IOperation<GetPermissionRequestRequest, PermissionRequestResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-requests.get";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionRequestResponse>> HandleAsync(GetPermissionRequestRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await service.GetAsync(request.Id, context).ConfigureAwait(false);
    }
}

/// <summary>
/// 提交授权申请。
/// </summary>
/// <remarks>
/// <c>POST /api/permission-requests</c>。为自己申请一个角色或一个权限码（也可以都申请），同一事务里发起审批；申请人要是这一侧的在用账号，同一对象只能有一条待审批的申请。
/// Workflow 关掉时 503 <c>ACCESS_UNAVAILABLE_002</c>（审批未启用）。
/// </remarks>
/// <param name="service">授权申请。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PermissionRequestsCreate)]
public sealed class CreatePermissionRequest(PermissionRequestService service) : IOperation<CreatePermissionRequestRequest, PermissionRequestResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-requests.create";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionRequestResponse>> HandleAsync(CreatePermissionRequestRequest request, InvocationContext context)
        => await service.CreateAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 修改并重新提交授权申请。
/// </summary>
/// <remarks>
/// <c>PUT /api/permission-requests/{id}</c>。只能改自己待审批的申请；撤回旧流程、按新内容重新发起，已有的审批意见作废（方案 W2，4.x 后端有、前端没接）。
/// </remarks>
/// <param name="service">授权申请。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PermissionRequestsUpdate)]
public sealed class UpdatePermissionRequest(PermissionRequestService service) : IOperation<UpdatePermissionRequestRequest, PermissionRequestResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-requests.update";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionRequestResponse>> HandleAsync(UpdatePermissionRequestRequest request, InvocationContext context)
        => await service.UpdateAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 撤回授权申请。
/// </summary>
/// <remarks>
/// <c>POST /api/permission-requests:withdraw</c>。批量；申请人撤回自己待审批的申请，同时撤回进行中的审批流程（4.x 的删除）。
/// </remarks>
/// <param name="service">授权申请。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PermissionRequestsWithdraw)]
public sealed class WithdrawPermissionRequests(PermissionRequestService service) : IOperation<PermissionRequestIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-requests.withdraw";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(PermissionRequestIdsRequest request, InvocationContext context)
        => await service.WithdrawAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 手工处理授权申请。
/// </summary>
/// <remarks>
/// <c>POST /api/permission-requests:set-status</c>。批量；管理员把别人待审批的申请置为已驳回或已撤回（不能置为已批准，审批通过必须走流程），同时撤回进行中的审批流程；
/// 与审批人同一口径：不能处理自己的申请，且要能授出申请的对象（方案 W2）。
/// </remarks>
/// <param name="service">授权申请。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PermissionRequestsStatus)]
public sealed class SetPermissionRequestStatus(PermissionRequestService service) : IOperation<SetPermissionRequestStatusRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-requests.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(SetPermissionRequestStatusRequest request, InvocationContext context)
        => await service.SetStatusAsync(request, context).ConfigureAwait(false);
}
