#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TestService
// Guid:t1e2s3t4-s5e6-4r7v-8i9c-0e1a2b3c4d5e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/26 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Tests;

/// <summary>
/// 测试服务
/// </summary>
[DynamicApi(GroupName = "测试模块", Description = "测试服务 - 用于演示动态 API 功能")]
public class TestService : ApplicationServiceBase
{
    /// <summary>
    /// 获取测试消息1
    /// </summary>
    [DynamicApi(
        RouteTemplate = "test/get1",
        Name = "GetTest1",
        Description = "获取第一个测试消息，使用自定义路由",
        GroupName = "测试-基础接口")]
    public string GetTest1()
    {
        return $"This is test message 1 from TestService.";
    }

    /// <summary>
    /// 获取测试消息2
    /// </summary>
    [DynamicApi(
        Name = "GetTest2",
        Description = "获取第二个测试消息，使用默认路由规则")]
    public string GetTest2()
    {
        return $"This is test message 2 from TestService.";
    }

    /// <summary>
    /// 根据 ID 获取测试数据
    /// </summary>
    [DynamicApi(
        Description = "根据 ID 获取指定的测试数据",
        GroupName = "测试-查询接口")]
    public Task<string> GetByIdAsync(long id)
    {
        return Task.FromResult($"Test data with ID: {id}");
    }

    /// <summary>
    /// 创建测试数据
    /// </summary>
    [DynamicApi(
        Description = "创建新的测试数据，支持传入自定义内容",
        GroupName = "测试-写入接口")]
    public Task<string> CreateAsync(string content)
    {
        return Task.FromResult($"Created: {content} at {DateTimeOffset.Now}");
    }

    /// <summary>
    /// 自动从 XML 注释获取描述 - 这是一个演示方法，展示如何自动使用 XML 注释的 summary 作为 API 描述
    /// </summary>
    /// <remarks>
    /// 这个方法没有在 DynamicApi 特性中指定 Description，
    /// 所以会自动使用上面 summary 标签中的内容作为 Swagger 描述。
    /// </remarks>
    [DynamicApi(GroupName = "测试-自动描述")]
    public Task<string> GetAutoDescriptionAsync()
    {
        return Task.FromResult("This API uses XML comment summary as description automatically");
    }

    /// <summary>
    /// 内部方法（不暴露为 API）
    /// </summary>
    [DynamicApi(IsEnabled = false)]
    public string InternalMethod()
    {
        return "This method is not exposed as API";
    }

    /// <summary>
    /// 隐藏的 API（不在 Swagger 中显示）
    /// </summary>
    [DynamicApi(
        Description = "这个 API 存在但不会在 Swagger 文档中显示",
        VisibleInApiExplorer = false)]
    public string HiddenApi()
    {
        return "This API is hidden from Swagger";
    }
}
