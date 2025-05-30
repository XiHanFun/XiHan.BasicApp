﻿#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:Program
// Guid:c9bf360b-8c2f-4e2a-9f36-cc2edadd551e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/10 5:34:12
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.WebHost;
using XiHan.Framework.AspNetCore.Extensions.DependencyInjection;
using XiHan.Framework.Core.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

_ = await builder.Services.AddApplicationAsync<XiHanBasicAppWebHostModule>();

var app = builder.Build();

await app.InitializeApplicationAsync();

await app.RunAsync();
