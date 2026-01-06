# RBAC 种子数据系统

> **完整的高可用种子数据**  
> **版本**：v2.0  
> **最后更新**：2026-01-07

---

## 📋 种子数据概述

本系统提供了一套完整的 RBAC 种子数据，初始化后即可获得一个高可用的权限管理系统，无需额外配置即可开始使用。

---

## 🎯 设计目标

1. **即用性**：初始化后立即可用，无需手动配置
2. **完整性**：涵盖所有核心功能模块
3. **标准化**：遵循 RBAC 最佳实践
4. **可扩展**：易于在此基础上扩展业务需求

---

## 📊 种子数据清单

### 执行顺序

种子数据按照依赖关系自动排序执行（Order 字段）：

| Order | Seeder                    | 说明                                     | 数量    |
| ----- | ------------------------- | ---------------------------------------- | ------- |
| 1     | `SysOperationSeeder`      | 系统操作（CRUD + 业务 + 系统 + 管理）    | 14 个   |
| 2     | `SysResourceSeeder`       | 系统资源（菜单、API、日志等）            | 18 个   |
| 3     | `SysPermissionSeeder`     | 系统权限（Resource + Operation 组合）    | ~100 个 |
| 4     | `SysDepartmentSeeder`     | 系统部门（公司、技术、产品、运营、行政） | 5 个    |
| 10    | `SysRoleSeeder`           | 系统角色（6 个预置角色）                 | 6 个    |
| 15    | `SysRolePermissionSeeder` | 角色权限映射                             | ~400 个 |
| 20    | `SysUserSeeder`           | 系统用户（4 个测试用户）                 | 4 个    |
| 25    | `SysMenuSeeder`           | 系统菜单（完整菜单树）                   | 15 个   |
| 30    | `SysUserRoleSeeder`       | 用户角色映射                             | 4 个    |

---

## 👥 预置用户

### 1. admin（超级管理员）

- **用户名**：`admin`
- **密码**：`Admin@123`（SHA256 加密）
- **角色**：超级管理员
- **权限**：所有权限
- **邮箱**：admin@xihanfun.com
- **电话**：13800138000

### 2. system（系统管理员）

- **用户名**：`system`
- **密码**：`System@123`
- **角色**：系统管理员
- **权限**：管理权限（排除删除、撤销、租户管理）
- **邮箱**：system@xihanfun.com
- **电话**：13800138001

### 3. test（测试用户）

- **用户名**：`test`
- **密码**：`Test@123`
- **角色**：普通员工
- **权限**：查看和管理自己的数据
- **邮箱**：test@xihanfun.com
- **电话**：13800138002

### 4. demo（演示用户）

- **用户名**：`demo`
- **密码**：`Demo@123`
- **角色**：访客
- **权限**：基础查看权限
- **邮箱**：demo@xihanfun.com
- **电话**：13800138003

---

## 🎭 预置角色

### 1. super_admin（超级管理员）

- **权限范围**：所有权限
- **数据范围**：全部数据
- **使用场景**：系统维护、紧急处理

### 2. admin（系统管理员）

- **权限范围**：管理权限（排除敏感操作）
- **数据范围**：全部数据
- **使用场景**：日常系统管理

### 3. dept_admin（部门管理员）

- **权限范围**：用户、角色、部门管理
- **数据范围**：本部门及子部门
- **使用场景**：部门级管理

### 4. dept_manager（部门经理）

- **权限范围**：查看和基本管理
- **数据范围**：本部门数据
- **使用场景**：部门日常管理

### 5. employee（普通员工）

- **权限范围**：查看权限
- **数据范围**：仅本人数据
- **使用场景**：普通员工日常使用

### 6. guest（访客）

- **权限范围**：极少数查看权限
- **数据范围**：仅本人数据
- **使用场景**：临时访问、演示账号

---

## 🔧 标准操作（14 个）

### CRUD 操作

1. `create` - 创建
2. `read` - 查看
3. `update` - 更新
4. `delete` - 删除
5. `view` - 查看详情

### 业务操作

6. `approve` - 审批
7. `execute` - 执行

### 系统操作

8. `import` - 导入
9. `export` - 导出
10. `download` - 下载

### 管理操作

11. `grant` - 授权
12. `revoke` - 撤销
13. `enable` - 启用
14. `disable` - 禁用

---

## 🏢 系统资源（18 个）

### 系统管理模块

1. `system` - 系统管理（根节点）
2. `user` - 用户管理
3. `user_api` - 用户 API
4. `role` - 角色管理
5. `role_api` - 角色 API
6. `permission` - 权限管理
7. `permission_api` - 权限 API
8. `menu` - 菜单管理
9. `menu_api` - 菜单 API
10. `department` - 部门管理
11. `department_api` - 部门 API
12. `tenant` - 租户管理

### 日志监控模块

13. `log` - 日志管理
14. `login_log` - 登录日志
15. `operation_log` - 操作日志
16. `monitor` - 系统监控

### 配置管理模块

17. `config` - 配置管理
18. `dict` - 字典管理

---

## 🎯 权限分配策略

### 超级管理员（super_admin）

```
✅ 所有权限（100%）
```

### 系统管理员（admin）

```
✅ 所有查看、创建、更新权限
✅ 导入、导出、启用、禁用权限
❌ 删除权限（delete）
❌ 撤销权限（revoke）
❌ 租户管理（tenant:*）
```

### 部门管理员（dept_admin）

```
✅ 用户管理（user:*）
✅ 部门管理（department:*）
✅ 角色管理（role:* 除 delete）
❌ 权限管理
❌ 系统配置
```

### 部门经理（dept_manager）

```
✅ 所有查看权限（*:read, *:view）
✅ 用户基本管理（user:create, user:update）
❌ 删除权限
❌ 系统管理
```

### 普通员工（employee）

```
✅ 查看权限（*:read, *:view）
❌ 任何修改操作
```

### 访客（guest）

```
✅ 用户查看（user:read）
✅ 部门查看（department:read）
❌ 其他所有权限
```

---

## 📁 部门结构

```
曦寒科技（ROOT）
├── 技术部（TECH）
├── 产品部（PRODUCT）
├── 运营部（OPERATION）
└── 行政部（ADMIN）
```

---

## 🎨 菜单结构

```
首页（Dashboard）
└── 固定标签页

系统管理（System）
├── 用户管理（User）
├── 角色管理（Role）
├── 权限管理（Permission）
├── 菜单管理（Menu）
├── 部门管理（Department）
└── 租户管理（Tenant）

日志管理（Log）
├── 登录日志（LoginLog）
└── 操作日志（OperationLog）

系统监控（Monitor）

配置管理（Config）

字典管理（Dict）
```

---

## 🚀 使用方法

### 1. 自动初始化

系统启动时会自动执行种子数据初始化：

```csharp
// 在 Program.cs 或 Startup.cs 中
await app.Services.SeedDataAsync();
```

### 2. 手动初始化

```csharp
// 获取种子数据服务
var seederService = app.Services.GetRequiredService<IDataSeederService>();

// 执行所有种子数据
await seederService.SeedAsync();

// 或执行特定种子数据
await seederService.SeedAsync<SysUserSeeder>();
```

### 3. 重置种子数据

```bash
# 清空数据库
dotnet ef database drop

# 重新创建数据库
dotnet ef database update

# 重新执行种子数据
dotnet run -- seed
```

---

## ⚠️ 安全注意事项

### 生产环境必做

1. **修改默认密码**

   ```sql
   -- 立即修改所有默认用户密码
   UPDATE Sys_User SET Password = '新加密密码' WHERE UserName IN ('admin', 'system', 'test', 'demo');
   ```

2. **禁用测试账号**

   ```sql
   -- 禁用测试账号和演示账号
   UPDATE Sys_User SET Status = 0 WHERE UserName IN ('test', 'demo');
   ```

3. **删除测试数据**

   ```sql
   -- 删除测试用户（可选）
   DELETE FROM Sys_User_Role WHERE UserId IN (SELECT BasicId FROM Sys_User WHERE UserName IN ('test', 'demo'));
   DELETE FROM Sys_User WHERE UserName IN ('test', 'demo');
   ```

4. **修改管理员信息**
   - 修改管理员邮箱和电话为实际联系方式
   - 启用双因子认证（2FA）
   - 配置登录 IP 白名单

---

## 📝 扩展建议

### 添加新资源

1. 在 `SysResourceSeeder` 中添加资源
2. 在 `SysPermissionSeeder` 中会自动生成权限
3. 在 `SysRolePermissionSeeder` 中分配权限给角色
4. 在 `SysMenuSeeder` 中添加对应菜单

### 添加新角色

1. 在 `SysRoleSeeder` 中添加角色
2. 在 `SysRolePermissionSeeder` 中为角色分配权限
3. 根据需要调整数据权限范围

### 添加新用户

1. 在 `SysUserSeeder` 中添加用户
2. 在 `SysUserRoleSeeder` 中分配角色

---

## 🔍 调试技巧

### 查看种子数据执行日志

```csharp
// 在 appsettings.json 中设置日志级别
{
  "Logging": {
    "LogLevel": {
      "XiHan.BasicApp.Rbac.Seeders": "Debug"
    }
  }
}
```

### 检查权限分配

```sql
-- 查看用户拥有的权限
SELECT u.UserName, r.RoleName, p.PermissionCode, p.PermissionName
FROM Sys_User u
JOIN Sys_User_Role ur ON u.BasicId = ur.UserId
JOIN Sys_Role r ON ur.RoleId = r.BasicId
JOIN Sys_Role_Permission rp ON r.BasicId = rp.RoleId
JOIN Sys_Permission p ON rp.PermissionId = p.BasicId
WHERE u.UserName = 'admin';
```

### 统计权限数量

```sql
-- 统计各角色的权限数量
SELECT r.RoleName, COUNT(rp.PermissionId) AS PermissionCount
FROM Sys_Role r
LEFT JOIN Sys_Role_Permission rp ON r.BasicId = rp.RoleId
GROUP BY r.RoleName;
```

---

## 📚 相关文档

- [RBAC 完整指南](../RBAC_COMPLETE_GUIDE.md)
- [种子数据示例](../SEED_DATA_EXAMPLE.md)
- [架构设计](../ARCHITECTURE.md)

---

**最后更新**：2026-01-07  
**维护者**：XiHan Development Team
