#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFieldSecurityService
// Guid:6c9e3ec6-2852-45c5-9d7b-33bb557ece9b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 22:13:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.InternalServices;

public interface IFieldSecurityService
{
    Task<FieldSecurityDecisionDto> GetCurrentUserFieldSecurityAsync(
        string resourceCode,
        IReadOnlyCollection<string>? fieldNames = null,
        CancellationToken cancellationToken = default);
}
