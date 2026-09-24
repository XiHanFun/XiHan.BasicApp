// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Permissions;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天权限码清单完整性测试。
/// </summary>
public sealed class ChatPermissionTests
{
    /// <summary>
    /// 权限清单必须完整覆盖四码（作用侧两侧，由权限种子声明）。
    /// </summary>
    [Fact]
    public void Permissions_ShouldRegisterAllFourCodes()
    {
        string[] codes =
        [
            ChatPermissionCodes.Read,
            ChatPermissionCodes.Send,
            ChatPermissionCodes.Manage,
            ChatPermissionCodes.Audit
        ];

        Assert.All(codes, code => Assert.Contains(code, ChatPermissionCodes.All));
        Assert.Equal(codes.Length, ChatPermissionCodes.All.Count);
        Assert.All(codes, code => Assert.StartsWith($"{ChatPermissionCodes.Module}:", code, StringComparison.Ordinal));
    }
}
