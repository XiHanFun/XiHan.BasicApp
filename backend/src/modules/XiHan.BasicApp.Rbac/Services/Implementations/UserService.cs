#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserService
// Guid:3c2b3c4d-5e6f-7890-abcd-ef12345678b8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Dtos.Users;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.BasicApp.Rbac.Services.Abstractions;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Implementations;

/// <summary>
/// 用户服务实现
/// </summary>
public class UserService : ApplicationServiceBase, IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly UserManager _userManager;
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IDepartmentRepository departmentRepository,
        UserManager userManager,
        ISqlSugarDbContext dbContext)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _departmentRepository = departmentRepository;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public async Task<UserDto?> GetByIdAsync(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user?.ToDto();
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    public async Task<UserDetailDto?> GetDetailAsync(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        var roleIds = await _userRepository.GetUserRoleIdsAsync(id);
        var roles = await _roleRepository.QueryListAsync(r => roleIds.Contains(r.BasicId));

        var departmentIds = await _userRepository.GetUserDepartmentIdsAsync(id);
        var departments = await _departmentRepository.QueryListAsync(d => departmentIds.Contains(d.BasicId));

        var permissions = await _userRepository.GetUserPermissionsAsync(id);

        return new UserDetailDto
        {
            BasicId = user.BasicId,
            TenantId = user.TenantId,
            UserName = user.UserName,
            RealName = user.RealName,
            NickName = user.NickName,
            Avatar = user.Avatar,
            Email = user.Email,
            Phone = user.Phone,
            Gender = user.Gender,
            Birthday = user.Birthday,
            Status = user.Status,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp,
            TimeZone = user.TimeZone,
            Language = user.Language,
            Country = user.Country,
            Remark = user.Remark,
            CreatedBy = user.CreatedBy,
            CreatedTime = user.CreatedTime,
            ModifiedBy = user.ModifiedBy,
            ModifiedTime = user.ModifiedTime,
            IsDeleted = user.IsDeleted,
            DeletedBy = user.DeletedBy,
            DeletedTime = user.DeletedTime,
            RoleIds = roleIds,
            RoleNames = roles.Select(r => r.RoleName).ToList(),
            DepartmentIds = departmentIds,
            DepartmentNames = departments.Select(d => d.DepartmentName).ToList(),
            Permissions = permissions
        };
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    public async Task<UserDto?> GetByUserNameAsync(string userName)
    {
        var user = await _userRepository.GetByUserNameAsync(userName);
        return user?.ToDto();
    }

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return user?.ToDto();
    }

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    public async Task<UserDto?> GetByPhoneAsync(string phone)
    {
        var user = await _userRepository.GetByPhoneAsync(phone);
        return user?.ToDto();
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    public async Task<UserDto> CreateAsync(CreateUserDto input)
    {
        // 验证用户名唯一性
        if (!await _userManager.IsUserNameUniqueAsync(input.UserName))
        {
            throw new InvalidOperationException(ErrorMessageConstants.UserNameExists);
        }

        // 验证邮箱唯一性
        if (!string.IsNullOrEmpty(input.Email) && !await _userManager.IsEmailUniqueAsync(input.Email))
        {
            throw new InvalidOperationException(ErrorMessageConstants.EmailExists);
        }

        // 验证手机号唯一性
        if (!string.IsNullOrEmpty(input.Phone) && !await _userManager.IsPhoneUniqueAsync(input.Phone))
        {
            throw new InvalidOperationException(ErrorMessageConstants.PhoneExists);
        }

        var user = new SysUser
        {
            TenantId = input.TenantId,
            UserName = input.UserName,
            Password = _userManager.HashPassword(input.Password),
            RealName = input.RealName,
            NickName = input.NickName,
            Avatar = input.Avatar,
            Email = input.Email,
            Phone = input.Phone,
            Gender = input.Gender,
            Birthday = input.Birthday,
            TimeZone = input.TimeZone,
            Language = input.Language,
            Country = input.Country,
            Remark = input.Remark
        };

        await _userRepository.InsertAsync(user);

        // 分配角色
        if (input.RoleIds.Any())
        {
            await AssignRolesAsync(new AssignUserRolesDto
            {
                UserId = user.BasicId,
                RoleIds = input.RoleIds
            });
        }

        // 分配部门
        if (input.DepartmentIds.Any())
        {
            await AssignDepartmentsAsync(new AssignUserDepartmentsDto
            {
                UserId = user.BasicId,
                DepartmentIds = input.DepartmentIds
            });
        }

        return user.ToDto();
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    public async Task<UserDto> UpdateAsync(UpdateUserDto input)
    {
        var user = await _userRepository.GetByIdAsync(input.BasicId);
        if (user == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.UserNotFound);
        }

        // 验证邮箱唯一性
        if (!string.IsNullOrEmpty(input.Email) && !await _userManager.IsEmailUniqueAsync(input.Email, user.BasicId))
        {
            throw new InvalidOperationException(ErrorMessageConstants.EmailExists);
        }

        // 验证手机号唯一性
        if (!string.IsNullOrEmpty(input.Phone) && !await _userManager.IsPhoneUniqueAsync(input.Phone, user.BasicId))
        {
            throw new InvalidOperationException(ErrorMessageConstants.PhoneExists);
        }

        // 更新用户信息
        if (input.RealName != null)
        {
            user.RealName = input.RealName;
        }

        if (input.NickName != null)
        {
            user.NickName = input.NickName;
        }

        if (input.Avatar != null)
        {
            user.Avatar = input.Avatar;
        }

        if (input.Email != null)
        {
            user.Email = input.Email;
        }

        if (input.Phone != null)
        {
            user.Phone = input.Phone;
        }

        if (input.Gender.HasValue)
        {
            user.Gender = input.Gender.Value;
        }

        if (input.Birthday.HasValue)
        {
            user.Birthday = input.Birthday;
        }

        if (input.Status.HasValue)
        {
            user.Status = input.Status.Value;
        }

        if (input.TimeZone != null)
        {
            user.TimeZone = input.TimeZone;
        }

        if (input.Language != null)
        {
            user.Language = input.Language;
        }

        if (input.Country != null)
        {
            user.Country = input.Country;
        }

        if (input.Remark != null)
        {
            user.Remark = input.Remark;
        }

        await _userRepository.UpdateAsync(user);

        return user.ToDto();
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.UserNotFound);
        }

        return await _userRepository.DeleteAsync(user);
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public async Task<bool> ChangePasswordAsync(ChangePasswordDto input)
    {
        var user = await _userRepository.GetByIdAsync(input.UserId);
        if (user == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.UserNotFound);
        }

        // 验证原密码
        if (!_userManager.VerifyPassword(user, input.OldPassword))
        {
            throw new InvalidOperationException(ErrorMessageConstants.OldPasswordError);
        }

        // 验证两次密码是否一致
        if (input.NewPassword != input.ConfirmPassword)
        {
            throw new InvalidOperationException(ErrorMessageConstants.PasswordNotMatch);
        }

        user.Password = _userManager.HashPassword(input.NewPassword);
        return await _userRepository.UpdateAsync(user);
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    public async Task<bool> ResetPasswordAsync(ResetPasswordDto input)
    {
        var user = await _userRepository.GetByIdAsync(input.UserId);
        if (user == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.UserNotFound);
        }

        user.Password = _userManager.HashPassword(input.NewPassword);
        return await _userRepository.UpdateAsync(user);
    }

    /// <summary>
    /// 分配角色
    /// </summary>
    public async Task<bool> AssignRolesAsync(AssignUserRolesDto input)
    {
        var client = _dbContext.GetClient();

        // 删除原有角色
        await client.Deleteable<SysUserRole>()
            .Where(ur => ur.UserId == input.UserId)
            .ExecuteCommandAsync();

        // 添加新角色
        if (input.RoleIds.Any())
        {
            var userRoles = input.RoleIds.Select(roleId => new SysUserRole
            {
                UserId = input.UserId,
                RoleId = roleId
            }).ToList();

            await client.Insertable(userRoles).ExecuteCommandAsync();
        }

        return true;
    }

    /// <summary>
    /// 分配部门
    /// </summary>
    public async Task<bool> AssignDepartmentsAsync(AssignUserDepartmentsDto input)
    {
        var client = _dbContext.GetClient();

        // 删除原有部门
        await client.Deleteable<SysUserDepartment>()
            .Where(ud => ud.UserId == input.UserId)
            .ExecuteCommandAsync();

        // 添加新部门
        if (input.DepartmentIds.Any())
        {
            var userDepartments = input.DepartmentIds.Select(departmentId => new SysUserDepartment
            {
                UserId = input.UserId,
                DepartmentId = departmentId
            }).ToList();

            await client.Insertable(userDepartments).ExecuteCommandAsync();
        }

        return true;
    }

    /// <summary>
    /// 获取用户权限
    /// </summary>
    public async Task<List<string>> GetUserPermissionsAsync(long userId)
    {
        return await _userRepository.GetUserPermissionsAsync(userId);
    }

    /// <summary>
    /// 分页查询用户
    /// </summary>
    public async Task<PageResponse<UserDto>> GetPagedListAsync(PageQuery query)
    {
        var queryable = _userRepository.Queryable();

        // 应用筛选条件
        if (query.Conditions != null && query.Conditions.Any())
        {
            foreach (var condition in query.Conditions)
            {
                // 这里可以根据条件动态构建查询
                // 示例：根据用户名搜索
                if (condition.Field == "UserName" && !string.IsNullOrEmpty(condition.Value?.ToString()))
                {
                    queryable = queryable.Where(u => u.UserName.Contains(condition.Value.ToString()!));
                }
            }
        }

        // 应用排序
        if (query.Sorts != null && query.Sorts.Any())
        {
            foreach (var sort in query.Sorts)
            {
                queryable = sort.Direction == Paging.Enums.SortDirection.Ascending
                    ? queryable.OrderBy($"{sort.Field} ASC")
                    : queryable.OrderBy($"{sort.Field} DESC");
            }
        }
        else
        {
            queryable = queryable.OrderBy(u => u.CreatedTime, OrderByType.Desc);
        }

        // 分页
        var total = await queryable.CountAsync();
        var items = await queryable
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PageResponse<UserDto>
        {
            Items = items.ToDto(),
            PageData = new PageData(query.PageIndex, query.PageSize, total)
        };
    }
}
