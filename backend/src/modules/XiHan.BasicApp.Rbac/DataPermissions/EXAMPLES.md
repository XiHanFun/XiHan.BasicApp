#数据权限过滤使用示例

## 场景 1: 文档管理系统

### 实体定义

```csharp
using XiHan.BasicApp.Rbac.DataPermissions.Attributes;
using XiHan.BasicApp.Rbac.DataPermissions.Enums;

/// <summary>
/// 文档实体 - 应用部门级数据权限
/// </summary>
[DataPermission(DataPermissionScope.DepartmentOnly)]
public class Document : RbacFullAuditedEntity<long>
{
    public string Title { get; set; }
    public string Content { get; set; }
    public long? DepartmentId { get; set; }
    public DocumentStatus Status { get; set; }
}
```

### 服务实现

```csharp
public class DocumentService : ApplicationServiceBase
{
    private readonly IRepository<Document> _documentRepository;
    private readonly DataPermissionHandler _dataPermissionHandler;

    public DocumentService(
        IRepository<Document> documentRepository,
        DataPermissionHandler dataPermissionHandler)
    {
        _documentRepository = documentRepository;
        _dataPermissionHandler = dataPermissionHandler;
    }

    /// <summary>
    /// 获取用户可见的文档列表（自动应用数据权限）
    /// </summary>
    public async Task<List<DocumentDto>> GetMyDocumentsAsync(long userId)
    {
        var query = _documentRepository.Queryable();

        // 自动应用数据权限过滤
        query = await _dataPermissionHandler.ApplyDataPermissionAsync(query, userId);

        // 添加其他业务条件
        query = query.Where(d => d.Status == DocumentStatus.Published);
        query = query.OrderByDescending(d => d.CreatedTime);

        var documents = await query.ToListAsync();
        return documents.ToDto();
    }

    /// <summary>
    /// 检查用户是否可以访问特定文档
    /// </summary>
    public async Task<bool> CanAccessDocumentAsync(long userId, long documentId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null) return false;

        return await _dataPermissionHandler.CheckDataAccessPermissionAsync(
            userId,
            document.CreatedBy,
            document.DepartmentId);
    }
}
```

## 场景 2: 人力资源管理系统

### 实体定义

```csharp
/// <summary>
/// 员工实体 - 应用本部门及子部门数据权限
/// </summary>
[DataPermission(DataPermissionScope.DepartmentAndChildren)]
public class Employee : RbacFullAuditedEntity<long>
{
    public string Name { get; set; }
    public string EmployeeNo { get; set; }
    public long? DepartmentId { get; set; }
    public decimal Salary { get; set; }
}

/// <summary>
/// 薪资记录 - 仅本人可见
/// </summary>
[DataPermission(DataPermissionScope.SelfOnly)]
public class SalaryRecord : RbacFullAuditedEntity<long>
{
    public long EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset PaymentDate { get; set; }
}
```

### 服务实现

```csharp
public class EmployeeService : ApplicationServiceBase
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<SalaryRecord> _salaryRepository;
    private readonly DataPermissionHandler _dataPermissionHandler;

    /// <summary>
    /// 获取部门经理可见的员工列表（包含子部门）
    /// </summary>
    public async Task<List<EmployeeDto>> GetDepartmentEmployeesAsync(long managerId)
    {
        var query = _employeeRepository.Queryable();

        // 自动过滤：只返回本部门及子部门的员工
        query = await _dataPermissionHandler.ApplyDataPermissionAsync(query, managerId);

        var employees = await query.ToListAsync();
        return employees.ToDto();
    }

    /// <summary>
    /// 获取薪资记录（仅本人可见）
    /// </summary>
    public async Task<List<SalaryRecordDto>> GetMySalaryRecordsAsync(long userId)
    {
        var query = _salaryRepository.Queryable();

        // 自动过滤：只返回用户自己的薪资记录
        query = await _dataPermissionHandler.ApplyDataPermissionAsync(query, userId);

        var records = await query.ToListAsync();
        return records.ToDto();
    }
}
```

## 场景 3: 项目管理系统

### 实体定义

```csharp
/// <summary>
/// 项目实体 - 使用自定义字段名
/// </summary>
[DataPermission(
    DataPermissionScope.DepartmentOnly,
    DepartmentField = "OwnerDepartmentId",
    CreatorField = "ProjectManagerId"
)]
public class Project : RbacFullAuditedEntity<long>
{
    public string ProjectName { get; set; }
    public string ProjectCode { get; set; }
    public long? OwnerDepartmentId { get; set; }
    public long? ProjectManagerId { get; set; }
    public ProjectStatus Status { get; set; }
}

/// <summary>
/// 项目任务 - 继承项目的数据权限
/// </summary>
[DataPermission(DataPermissionScope.DepartmentOnly)]
public class ProjectTask : RbacFullAuditedEntity<long>
{
    public long ProjectId { get; set; }
    public string TaskName { get; set; }
    public long? AssignedUserId { get; set; }
    public long? DepartmentId { get; set; }
}
```

### 服务实现

```csharp
public class ProjectService : ApplicationServiceBase
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<ProjectTask> _taskRepository;
    private readonly DataPermissionHandler _dataPermissionHandler;
    private readonly IDataPermissionFilter _dataPermissionFilter;

    /// <summary>
    /// 获取用户的项目列表
    /// </summary>
    public async Task<PageResponse<ProjectDto>> GetProjectsAsync(long userId, PageQuery query)
    {
        var queryable = _projectRepository.Queryable();

        // 应用数据权限过滤
        queryable = await _dataPermissionHandler.ApplyDataPermissionAsync(queryable, userId);

        // 应用搜索条件
        if (query.Conditions != null && query.Conditions.Any())
        {
            foreach (var condition in query.Conditions)
            {
                if (condition.Field == "ProjectName")
                {
                    queryable = queryable.Where(p => p.ProjectName.Contains(condition.Value.ToString()));
                }
            }
        }

        // 分页
        var total = await queryable.CountAsync();
        var items = await queryable
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PageResponse<ProjectDto>
        {
            Items = items.ToDto(),
            PageInfo = new PageInfo
            {
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                Total = total
            }
        };
    }

    /// <summary>
    /// 使用过滤器直接构建复杂查询
    /// </summary>
    public async Task<List<ProjectDto>> GetCustomFilteredProjectsAsync(long userId)
    {
        // 获取数据权限表达式
        var permissionExpression = await _dataPermissionFilter.BuildFilterExpressionAsync<Project>(
            userId,
            DataPermissionScope.DepartmentAndChildren,
            "OwnerDepartmentId",
            "ProjectManagerId");

        var query = _projectRepository.Queryable();

        if (permissionExpression != null)
        {
            query = query.Where(permissionExpression);
        }

        // 添加业务逻辑过滤
        query = query.Where(p => p.Status == ProjectStatus.Active);
        query = query.Where(p => p.CreatedTime >= DateTime.Now.AddMonths(-6));

        var projects = await query.ToListAsync();
        return projects.ToDto();
    }
}
```

## 场景 4: 销售订单系统

### 实体定义

```csharp
/// <summary>
/// 销售订单 - 销售人员只能看到自己的订单
/// </summary>
[DataPermission(DataPermissionScope.SelfOnly)]
public class SalesOrder : RbacFullAuditedEntity<long>
{
    public string OrderNo { get; set; }
    public long CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public long? SalesPersonId { get; set; }  // 映射到 CreatedBy
}

/// <summary>
/// 客户 - 按部门权限管理
/// </summary>
[DataPermission(DataPermissionScope.DepartmentAndChildren)]
public class Customer : RbacFullAuditedEntity<long>
{
    public string CustomerName { get; set; }
    public string ContactPhone { get; set; }
    public long? DepartmentId { get; set; }
}
```

### 服务实现

```csharp
public class SalesService : ApplicationServiceBase
{
    private readonly IRepository<SalesOrder> _orderRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly DataPermissionHandler _dataPermissionHandler;

    /// <summary>
    /// 获取销售人员的订单列表
    /// </summary>
    public async Task<List<SalesOrderDto>> GetMyOrdersAsync(long salesPersonId)
    {
        var query = _orderRepository.Queryable();

        // 自动过滤：只返回销售人员自己的订单
        query = await _dataPermissionHandler.ApplyDataPermissionAsync(query, salesPersonId);

        // 按时间倒序
        query = query.OrderByDescending(o => o.CreatedTime);

        var orders = await query.Take(100).ToListAsync();
        return orders.ToDto();
    }

    /// <summary>
    /// 获取部门的客户列表（包含子部门）
    /// </summary>
    public async Task<List<CustomerDto>> GetDepartmentCustomersAsync(long managerId)
    {
        var query = _customerRepository.Queryable();

        // 自动过滤：返回本部门及子部门的客户
        query = await _dataPermissionHandler.ApplyDataPermissionAsync(query, managerId);

        var customers = await query.ToListAsync();
        return customers.ToDto();
    }

    /// <summary>
    /// 销售统计报表（带权限控制）
    /// </summary>
    public async Task<SalesStatisticsDto> GetSalesStatisticsAsync(long userId, DateTime startDate, DateTime endDate)
    {
        var query = _orderRepository.Queryable();

        // 应用数据权限
        query = await _dataPermissionHandler.ApplyDataPermissionAsync(query, userId);

        // 时间范围过滤
        query = query.Where(o => o.CreatedTime >= startDate && o.CreatedTime <= endDate);

        // 统计
        var statistics = new SalesStatisticsDto
        {
            TotalOrders = await query.CountAsync(),
            TotalAmount = await query.SumAsync(o => o.TotalAmount),
            AverageAmount = await query.AverageAsync(o => o.TotalAmount)
        };

        return statistics;
    }
}
```

## 场景 5: 多租户 SaaS 系统

### 实体定义

```csharp
/// <summary>
/// 租户数据 - 同时支持租户隔离和数据权限
/// </summary>
[DataPermission(
    DataPermissionScope.DepartmentOnly,
    EnableTenantFilter = true,  // 启用租户过滤
    TenantField = "TenantId"
)]
public class TenantData : RbacFullAuditedEntity<long>
{
    public long? TenantId { get; set; }
    public long? DepartmentId { get; set; }
    public string DataContent { get; set; }
}
```

### 服务实现

```csharp
public class TenantDataService : ApplicationServiceBase
{
    private readonly IRepository<TenantData> _dataRepository;
    private readonly DataPermissionHandler _dataPermissionHandler;

    /// <summary>
    /// 获取数据（自动应用租户隔离和数据权限）
    /// </summary>
    public async Task<List<TenantDataDto>> GetDataAsync(long userId, long tenantId)
    {
        var query = _dataRepository.Queryable();

        // 租户隔离
        query = query.Where(d => d.TenantId == tenantId);

        // 数据权限过滤
        query = await _dataPermissionHandler.ApplyDataPermissionAsync(query, userId);

        var data = await query.ToListAsync();
        return data.ToDto();
    }
}
```

## 场景 6: 条件性应用数据权限

```csharp
public class ReportService : ApplicationServiceBase
{
    private readonly IRepository<Report> _reportRepository;
    private readonly DataPermissionHandler _dataPermissionHandler;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 根据用户角色决定是否应用数据权限
    /// </summary>
    public async Task<List<ReportDto>> GetReportsAsync(long userId)
    {
        var query = _reportRepository.Queryable();

        // 获取用户的数据权限范围
        var scope = await _dataPermissionHandler.GetUserDataPermissionScopeAsync(userId);

        // 只对非管理员应用数据权限
        if (scope != DataPermissionScope.All)
        {
            query = await _dataPermissionHandler.ApplyDataPermissionAsync(query, userId);
        }

        var reports = await query.ToListAsync();
        return reports.ToDto();
    }

    /// <summary>
    /// 根据业务逻辑动态选择权限范围
    /// </summary>
    public async Task<List<ReportDto>> GetDynamicReportsAsync(long userId, bool includeSubDepartments)
    {
        var query = _reportRepository.Queryable();

        // 根据参数动态选择权限范围
        var scope = includeSubDepartments
            ? DataPermissionScope.DepartmentAndChildren
            : DataPermissionScope.DepartmentOnly;

        var filter = await _dataPermissionFilter.BuildFilterExpressionAsync<Report>(
            userId, scope, "DepartmentId", "CreatedBy");

        if (filter != null)
        {
            query = query.Where(filter);
        }

        var reports = await query.ToListAsync();
        return reports.ToDto();
    }
}
```

## 性能优化示例

### 使用缓存优化部门树查询

```csharp
public class OptimizedDataPermissionFilter : DataPermissionFilter
{
    private readonly ICache _cache;
    private const string CacheKeyPrefix = "dept_tree:";

    public OptimizedDataPermissionFilter(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        ICache cache)
        : base(userRepository, departmentRepository)
    {
        _cache = cache;
    }

    protected override async Task<List<long>> GetUserAllDepartmentIdsAsync(long userId)
    {
        var cacheKey = $"{CacheKeyPrefix}{userId}";

        // 尝试从缓存获取
        var cachedIds = await _cache.GetAsync<List<long>>(cacheKey);
        if (cachedIds != null)
        {
            return cachedIds;
        }

        // 查询并缓存
        var departmentIds = await base.GetUserAllDepartmentIdsAsync(userId);
        await _cache.SetAsync(cacheKey, departmentIds, TimeSpan.FromHours(1));

        return departmentIds;
    }
}
```

## 单元测试示例

```csharp
public class DataPermissionFilterTests
{
    private readonly IDataPermissionFilter _filter;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;

    [Fact]
    public async Task Should_Filter_By_SelfOnly()
    {
        // Arrange
        var userId = 1L;
        var documents = new List<Document>
        {
            new() { BasicId = 1, CreatedBy = 1 },
            new() { BasicId = 2, CreatedBy = 2 },
            new() { BasicId = 3, CreatedBy = 1 }
        }.AsQueryable();

        // Act
        var expression = await _filter.BuildFilterExpressionAsync<Document>(
            userId, DataPermissionScope.SelfOnly, "DepartmentId", "CreatedBy");

        var result = documents.Where(expression.Compile()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, doc => Assert.Equal(userId, doc.CreatedBy));
    }

    [Fact]
    public async Task Should_Filter_By_Department()
    {
        // Arrange
        var userId = 1L;
        var userDepartmentIds = new List<long> { 10, 20 };
        _userRepositoryMock
            .Setup(x => x.GetUserDepartmentIdsAsync(userId))
            .ReturnsAsync(userDepartmentIds);

        var documents = new List<Document>
        {
            new() { BasicId = 1, DepartmentId = 10 },
            new() { BasicId = 2, DepartmentId = 30 },
            new() { BasicId = 3, DepartmentId = 20 }
        }.AsQueryable();

        // Act
        var expression = await _filter.BuildFilterExpressionAsync<Document>(
            userId, DataPermissionScope.DepartmentOnly, "DepartmentId", "CreatedBy");

        var result = documents.Where(expression.Compile()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, doc =>
            Assert.Contains(doc.DepartmentId.Value, userDepartmentIds));
    }
}
```

## 总结

这些示例展示了数据权限过滤在各种业务场景中的应用。关键要点：

1. **简单易用**: 通过特性标记实体，自动应用过滤
2. **灵活配置**: 支持自定义字段名和权限范围
3. **性能优化**: 使用缓存和索引提升性能
4. **业务集成**: 与现有业务逻辑无缝集成
5. **测试友好**: 易于编写单元测试和集成测试
