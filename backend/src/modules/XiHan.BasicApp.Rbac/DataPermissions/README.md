# 数据权限过滤功能

## 概述

数据权限过滤是一个企业级的数据访问控制功能，可以根据用户的权限范围自动过滤查询结果，确保用户只能访问其权限范围内的数据。

## 功能特点

### 支持的数据权限范围

1. **全部数据权限 (All)**

   - 用户可以查看所有数据
   - 通常分配给超级管理员

2. **本部门及子部门数据权限 (DepartmentAndChildren)**

   - 用户可以查看本部门及其所有子部门的数据
   - 适用于部门经理等管理人员

3. **仅本部门数据权限 (DepartmentOnly)**

   - 用户只能查看本部门的数据
   - 适用于普通部门成员

4. **仅本人数据权限 (SelfOnly)**

   - 用户只能查看自己创建的数据
   - 最严格的权限控制

5. **自定义数据权限 (Custom)**
   - 支持自定义过滤规则
   - 可以实现复杂的业务逻辑

## 核心组件

### 1. DataPermissionScope (枚举)

定义数据权限范围类型。

```csharp
public enum DataPermissionScope
{
    All = 0,                    // 全部数据
    DepartmentAndChildren = 1,  // 本部门及子部门
    DepartmentOnly = 2,         // 仅本部门
    SelfOnly = 3,              // 仅本人
    Custom = 99                 // 自定义
}
```

### 2. DataPermissionAttribute (特性)

用于标记需要应用数据权限过滤的实体。

```csharp
[DataPermission(DataPermissionScope.DepartmentOnly)]
public class MyEntity
{
    public RbacIdType BasicId { get; set; }
    public long? DepartmentId { get; set; }
    public string? CreatedBy { get; set; }
}
```

### 3. IDataPermissionFilter (接口)

数据权限过滤器接口，定义过滤逻辑。

### 4. DataPermissionFilter (实现)

数据权限过滤器的具体实现，包含：

- 表达式树构建
- 部门树递归查询
- 权限范围判断

### 5. DataPermissionHandler (处理器)

数据权限处理器，负责：

- 检查实体是否需要数据权限过滤
- 获取用户的权限范围
- 应用过滤条件

## 使用方法

### 1. 标记实体

在需要数据权限控制的实体上添加 `DataPermissionAttribute` 特性：

```csharp
using XiHan.BasicApp.Rbac.DataPermissions.Attributes;
using XiHan.BasicApp.Rbac.DataPermissions.Enums;

[DataPermission(DataPermissionScope.DepartmentOnly)]
public class Document
{
    public RbacIdType BasicId { get; set; }
    public string Title { get; set; }
    public long? DepartmentId { get; set; }  // 必须有部门字段
    public string? CreatedBy { get; set; }      // 必须有创建者字段
}
```

### 2. 自定义字段名称

如果实体使用不同的字段名称，可以在特性中指定：

```csharp
[DataPermission(
    DataPermissionScope.DepartmentOnly,
    DepartmentField = "OrgId",
    CreatorField = "CreateUserId"
)]
public class CustomEntity
{
    public RbacIdType BasicId { get; set; }
    public long? OrgId { get; set; }
    public long? CreateUserId { get; set; }
}
```

### 3. 在服务中使用

#### 方法 1: 使用扩展方法

```csharp
public class DocumentService
{
    private readonly IRepository<Document> _repository;
    private readonly DataPermissionHandler _dataPermissionHandler;

    public async Task<List<Document>> GetDocumentsAsync(long userId)
    {
        var query = _repository.Queryable();

        // 应用数据权限过滤
        query = await query.WithDataPermissionAsync(_dataPermissionHandler, userId);

        return await query.ToListAsync();
    }
}
```

#### 方法 2: 使用处理器

```csharp
public class DocumentService
{
    private readonly IRepository<Document> _repository;
    private readonly DataPermissionHandler _dataPermissionHandler;

    public async Task<List<Document>> GetDocumentsAsync(long userId)
    {
        var query = _repository.Queryable();

        // 应用数据权限过滤
        query = await _dataPermissionHandler.ApplyDataPermissionAsync(query, userId);

        return await query.ToListAsync();
    }
}
```

### 4. 检查单条数据的访问权限

```csharp
public class DocumentService
{
    private readonly DataPermissionHandler _dataPermissionHandler;

    public async Task<bool> CanAccessDocument(long userId, long documentCreatorId, long? documentDepartmentId)
    {
        return await _dataPermissionHandler.CheckDataAccessPermissionAsync(
            userId,
            documentCreatorId,
            documentDepartmentId);
    }
}
```

### 5. 使用过滤器直接构建表达式

```csharp
public class DocumentService
{
    private readonly IDataPermissionFilter _dataPermissionFilter;

    public async Task<List<Document>> GetFilteredDocumentsAsync(long userId)
    {
        var expression = await _dataPermissionFilter.BuildFilterExpressionAsync<Document>(
            userId,
            DataPermissionScope.DepartmentAndChildren,
            "DepartmentId",
            "CreatedBy");

        var query = _repository.Queryable();
        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync();
    }
}
```

## 实现原理

### 1. 表达式树构建

数据权限过滤通过构建 LINQ 表达式树来实现动态过滤：

```csharp
// 仅本人数据权限的表达式
x => x.CreatedBy == userId

// 本部门数据权限的表达式
x => departmentIds.Contains(x.DepartmentId)

// 组合表达式
x => x.CreatedBy == userId || departmentIds.Contains(x.DepartmentId)
```

### 2. 部门树递归查询

对于 `DepartmentAndChildren` 范围，系统会递归查询所有子部门：

```
用户部门: [1]
子部门查询:
  - 部门1的子部门: [2, 3]
  - 部门2的子部门: [4, 5]
  - 部门3的子部门: [6]
结果: [1, 2, 3, 4, 5, 6]
```

### 3. 权限范围判断

系统会根据用户的角色自动判断权限范围：

- 超级管理员和管理员: `All`
- 其他角色: 根据配置或默认为 `SelfOnly`

## 配置示例

### 1. 在模块中启用

数据权限功能已在 `XiHanBasicAppRbacModule` 中自动启用：

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    var services = context.Services;

    // 添加数据权限支持
    services.AddDataPermission();
}
```

### 2. 禁用多租户过滤

如果不需要多租户隔离，可以在特性中禁用：

```csharp
[DataPermission(
    DataPermissionScope.DepartmentOnly,
    EnableTenantFilter = false
)]
public class MyEntity { }
```

## 高级用法

### 1. 自定义过滤器

实现 `IDataPermissionFilter` 接口来创建自定义过滤逻辑：

```csharp
public class CustomDataPermissionFilter : IDataPermissionFilter
{
    public async Task<Expression<Func<TEntity, bool>>?> BuildFilterExpressionAsync<TEntity>(
        long userId,
        DataPermissionScope scope,
        string departmentField = "DepartmentId",
        string creatorField = "CreatedBy") where TEntity : class
    {
        // 实现自定义逻辑
        return x => /* 自定义条件 */;
    }
}
```

### 2. 组合多个过滤条件

```csharp
var query = _repository.Queryable();

// 应用数据权限过滤
query = await query.WithDataPermissionAsync(_dataPermissionHandler, userId);

// 添加其他业务条件
query = query.Where(x => x.Status == Status.Active);
query = query.Where(x => x.CreateTime >= startDate);

return await query.ToListAsync();
```

### 3. 条件性应用数据权限

```csharp
var query = _repository.Queryable();

// 只对非管理员应用数据权限
if (!isAdmin)
{
    query = await query.WithDataPermissionAsync(_dataPermissionHandler, userId);
}

return await query.ToListAsync();
```

## 性能优化建议

1. **索引优化**

   - 在 `DepartmentId` 和 `CreatedBy` 字段上创建索引
   - 对于大数据量，考虑创建复合索引

2. **缓存部门树**

   - 部门树查询结果可以缓存，减少数据库查询

3. **批量查询优化**

   - 使用 `IN` 查询而不是多次单条查询
   - 合并多个小查询为一个大查询

4. **延迟加载**
   - 只在需要时才应用数据权限过滤
   - 避免不必要的权限检查

## 注意事项

1. **字段要求**

   - 实体必须有对应的部门字段或创建者字段
   - 字段类型必须是 `long` 或 `long?`

2. **性能影响**

   - 数据权限过滤会增加查询的复杂度
   - 对于大数据量，建议使用适当的索引

3. **权限更新**

   - 用户权限变更后，需要清除相关缓存
   - 部门调整后，需要重新计算权限范围

4. **多租户兼容**
   - 数据权限过滤与多租户隔离可以同时使用
   - 系统会自动组合两种过滤条件

## 测试建议

### 单元测试示例

```csharp
[Fact]
public async Task Should_Filter_By_Department()
{
    // Arrange
    var userId = 1;
    var scope = DataPermissionScope.DepartmentOnly;

    // Act
    var expression = await _filter.BuildFilterExpressionAsync<Document>(
        userId, scope, "DepartmentId", "CreatedBy");
    var result = _documents.Where(expression.Compile()).ToList();

    // Assert
    Assert.All(result, doc =>
        Assert.Contains(doc.DepartmentId.Value, userDepartmentIds));
}
```

## 总结

数据权限过滤功能提供了灵活而强大的数据访问控制能力，可以满足企业级应用的复杂权限需求。通过合理使用和配置，可以确保数据安全的同时保持良好的性能。
