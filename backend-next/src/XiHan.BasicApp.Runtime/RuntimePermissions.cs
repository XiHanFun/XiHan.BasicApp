// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Runtime;

/// <summary>
/// 运行时的权限码。
/// </summary>
/// <remarks>
/// 宿主普通端点用到、不归哪个能力包的权限码。
/// </remarks>
public static class RuntimePermissions
{
    /// <summary>
    /// 向 CloudEvents 入站端点投递事件。
    /// </summary>
    /// <remarks>
    /// 入站事件直接进各包的处理器（如断开实时连接、部门归属变更），只授给平台侧的服务账号或服务角色，不给普通用户。
    /// </remarks>
    public const string CloudEventsDeliver = "runtime.cloud-events.deliver";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(CloudEventsDeliver, "Deliver CloudEvents", PermissionScope.Platform, audited: true, description: "Deliver events to the CloudEvents inbound endpoint; grant it only to platform service accounts or service roles."),
    ];
}
