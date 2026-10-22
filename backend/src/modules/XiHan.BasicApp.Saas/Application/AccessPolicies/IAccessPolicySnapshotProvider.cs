// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 访问策略快照提供器
/// </summary>
public interface IAccessPolicySnapshotProvider
{
    /// <summary>
    /// 取当前快照；版本号变了就重建
    /// </summary>
    ValueTask<AccessPolicySnapshot> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 策略或条件写入后调用：在当前工作单元提交后换一个新版本号，本节点下次取用时重建，其他节点在下次检查版本时重建
    /// </summary>
    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
