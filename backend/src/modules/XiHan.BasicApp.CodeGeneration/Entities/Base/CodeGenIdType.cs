#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenIdType
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

// 全局 ID 类型别名
// 这是实现统一 ID 类型的核心：通过 global using 实现全局类型别名
//
// 使用方法：
// 1. 修改下面的 using 语句来切换整个模块的 ID 类型
// 2. 重新编译项目即可应用新的 ID 类型
//
// 常用配置：
// - 使用 CodeGenIdType：  global using CodeGenIdType = System.Int64;
// - 使用 int：   global using CodeGenIdType = System.Int32;
// - 使用 Guid：  global using CodeGenIdType = System.Guid;
// - 使用 string：global using CodeGenIdType = System.String;
global using CodeGenIdType = System.Int64;

