// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包的错误码：<c>IDENTITY_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </summary>
public static class IdentityErrors
{
    /// <summary>用户不存在（或不在当前租户）。</summary>
    public const string UserNotFound = "IDENTITY_NOTFOUND_001";

    /// <summary>用户名在该租户内已被占用。</summary>
    public const string UserNameTaken = "IDENTITY_CONFLICT_001";

    /// <summary>创建用户需要当前租户。</summary>
    public const string TenantRequired = "IDENTITY_VALIDATION_001";

    /// <summary>列表的筛选或排序字段不受支持。</summary>
    public const string QueryUnsupported = "IDENTITY_VALIDATION_002";
}
