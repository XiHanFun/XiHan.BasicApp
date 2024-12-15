#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2024 ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppRbacDomainModule
// Guid:857d9979-5e6c-4e60-9b5b-4b9d4f5cc910
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/7 6:25:39
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Domain;

namespace XiHan.BasicApp.Rbac.Domain;

/// <summary>
/// XiHanBasicAppRbacDomainModule
/// </summary>
[DependsOn(
    typeof(XiHanDddDomainModule)
)]
public class XiHanBasicAppRbacDomainModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
