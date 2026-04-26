#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BasicAppChatHub
// Guid:bf2a3b4c-5d6e-4f7a-9c3d-be4f5a6b7c8d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/06 05:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Web.RealTime.Attributes;
using XiHan.Framework.Web.RealTime.Hubs;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Hubs;

/// <summary>
/// 曦寒基础应用聊天 Hub
/// </summary>
[AuthorizeHub]
public class BasicAppChatHub : XiHanHub
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionManager">连接管理器</param>
    public BasicAppChatHub(IConnectionManager connectionManager)
        : base(connectionManager)
    {
    }
}
