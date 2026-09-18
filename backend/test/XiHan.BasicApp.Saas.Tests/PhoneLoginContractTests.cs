// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Contracts;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 手机验证码登录契约测试。
/// </summary>
/// <remarks>
/// 前端 code-login 页面调用 /Auth/PhoneLoginCode 与 /Auth/PhoneLogin；
/// 这两个方法曾经存在又被整体重建删掉，页面却留着，结果是点了没反应、后端 401。
/// 这里锁住方法与匿名可访问性，避免再次静默消失。
/// </remarks>
public sealed class PhoneLoginContractTests
{
    [Theory]
    [InlineData("PhoneLoginCodeAsync")]
    [InlineData("PhoneLoginAsync")]
    public void AuthAppService_ShouldExposePhoneLoginEndpoints(string methodName)
    {
        var method = typeof(IAuthAppService).GetMethod(methodName);
        Assert.NotNull(method);
    }

    [Theory]
    [InlineData("PhoneLoginCodeAsync")]
    [InlineData("PhoneLoginAsync")]
    public void PhoneLoginEndpoints_ShouldAllowAnonymous(string methodName)
    {
        var implementation = typeof(AuthAppService).GetMethod(methodName);
        Assert.NotNull(implementation);
        Assert.NotNull(implementation!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>());
    }
}
