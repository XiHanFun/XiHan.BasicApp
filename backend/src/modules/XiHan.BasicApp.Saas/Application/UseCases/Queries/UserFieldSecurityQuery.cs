#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserFieldSecurityQuery
// Guid:6aa83e4c-d1f3-4fc1-89c1-eaf0f7f177ea
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 22:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.UseCases.Queries;

public class UserFieldSecurityQuery
{
    public string ResourceCode { get; set; } = string.Empty;

    public string[] FieldNames { get; set; } = [];
}
