/**
 * Dynamic API 路由约定的回归锚点。
 *
 * 职责边界：源码注释里写明的「后端动词前缀是否剥离 / 参数绑到 query 还是 body / 控制器名易写错」
 * 这类坑，每条配一个违约即失败的用例。这些约定一旦被改回去就是线上 404 或参数绑不上，
 * 单看类型签名发现不了。
 */
import type { AxiosRequestConfig } from '~/request'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { permissionChangeLogApi } from './modules/audit'
import { cacheApi } from './modules/cache'
import { dictApi } from './modules/configuration'
import { exportTaskApi } from './modules/export'
import { fileApi, storageConfigApi } from './modules/files'
import { userApi, userSecurityApi, userSessionApi } from './modules/identity'
import { logManagementApi } from './modules/log'
import { botConfigApi, notificationApi, userInboxApi } from './modules/messaging'
import { menuApi } from './modules/navigation'
import { oauthAppApi } from './modules/oauth'
import { departmentApi, userDepartmentApi } from './modules/organization'
import { serverApi } from './modules/server'
import { permissionCenterApi, roleManagementApi, userManagementApi } from './modules/system'
import { tenantApi, tenantEditionPermissionApi, tenantMemberApi } from './modules/tenant'
import { workbenchApi } from './modules/workbench'
import { taskApi } from './modules/workflow'

interface RecordedCall {
  method: 'DELETE' | 'GET' | 'POST' | 'PUT'
  url: string
  body?: unknown
  config?: AxiosRequestConfig
}

const hoisted = vi.hoisted(() => ({ calls: [] as RecordedCall[] }))

vi.mock('@/api/request', () => ({
  requestClient: {
    get(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'GET', url, config })
      return Promise.resolve(null)
    },
    post(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'POST', url, body, config })
      return Promise.resolve(null)
    },
    put(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'PUT', url, body, config })
      return Promise.resolve(null)
    },
    delete(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'DELETE', url, config })
      return Promise.resolve(undefined)
    },
  },
}))

const { calls } = hoisted

beforeEach(() => {
  calls.length = 0
})

function only(): RecordedCall {
  expect(calls).toHaveLength(1)
  const call = calls[0]
  if (!call) {
    throw new Error('没有记录到任何请求调用')
  }
  return call
}

describe('文件接口的谓词与参数绑定', () => {
  it('彻底删除走 DELETE /File/File，三个参数全部走查询串——框架的 DELETE 不收请求体', async () => {
    await fileApi.destroy({ basicId: '1975', deletePhysical: true, reason: '违规' })

    expect(only()).toEqual({
      method: 'DELETE',
      url: '/File/File',
      config: { params: { basicId: '1975', deletePhysical: true, reason: '违规' } },
    })
  })

  it('下载走鉴权的 POST /File/DownloadFile，fileId 走 query 且响应类型为 blob', async () => {
    await fileApi.download('2048')

    const call = only()
    expect(call.method).toBe('POST')
    expect(call.url).toBe('/File/DownloadFile')
    expect(call.body).toBeUndefined()
    expect(call.config).toEqual({ params: { fileId: '2048' }, responseType: 'blob' })
  })

  it('签名地址把原始 fileId 交给 axios 编码，自己不做一次 encodeURIComponent（否则双重编码）', async () => {
    await fileApi.generatePresignedUrl('目录/文件 名.png')

    expect(only().config?.params).toEqual({ fileId: '目录/文件 名.png' })
  })

  it('上传把可选字段拍成 FormData，undefined / null / 空串跳过，0 与 false 保留', async () => {
    const file = new File(['x'], 'a.png', { type: 'image/png' })
    await fileApi.upload({
      file,
      directory: 'avatars',
      remark: '',
      bucketName: null,
      overwrite: false,
      width: 0,
    })

    const call = only()
    expect(call.url).toBe('/File/UploadFile')
    const form = call.body as FormData
    expect(form.get('File')).toBe(file)
    expect(form.get('Directory')).toBe('avatars')
    expect(form.get('Overwrite')).toBe('false')
    expect(form.get('Width')).toBe('0')
    expect(form.get('Remark')).toBeNull()
    expect(form.get('BucketName')).toBeNull()
  })

  it('上传进度按 loaded/total 取整；total 缺失时回退 0 而不是 NaN', async () => {
    const percents: number[] = []
    await fileApi.upload(
      { file: new File(['x'], 'a.png') },
      percent => percents.push(percent),
    )

    const onUploadProgress = only().config?.onUploadProgress
    expect(typeof onUploadProgress).toBe('function')
    onUploadProgress?.({ loaded: 1, total: 3, bytes: 1, lengthComputable: true })
    onUploadProgress?.({ loaded: 5, total: 0, bytes: 5, lengthComputable: false })
    onUploadProgress?.({ loaded: 10, total: 10, bytes: 10, lengthComputable: true })

    expect(percents).toEqual([33, 0, 100])
  })

  it('不传进度回调时不注册 onUploadProgress，避免 axios 为空回调多做一次包装', async () => {
    await fileApi.upload({ file: new File(['x'], 'a.png') })

    expect(only().config?.onUploadProgress).toBeUndefined()
  })

  it('存储配置的设为默认保留完整方法名走 POST——Set 前缀不在动词约定表内', async () => {
    await storageConfigApi.setDefault({ basicId: '1' } as never)

    expect(only()).toMatchObject({ method: 'POST', url: '/StorageConfig/SetDefaultStorageConfig' })
  })
})

describe('非 CRUD 前缀的动作保留完整方法名', () => {
  it('导出任务取消走 POST /ExportTask/Cancel，id 走 query 而不是路由段', async () => {
    await exportTaskApi.cancel('7')

    expect(only()).toEqual({
      method: 'POST',
      url: '/ExportTask/Cancel',
      body: undefined,
      config: { params: { id: '7' } },
    })
  })

  it('导出任务删除是 DELETE /ExportTask/Delete——Delete 在这里是动作名不是被剥离的动词', async () => {
    await exportTaskApi.remove('7')

    expect(only()).toEqual({
      method: 'DELETE',
      url: '/ExportTask/Delete',
      config: { params: { id: '7' } },
    })
  })

  it('通知催办走 POST /Notification/Remind，id 走 query（POST 不把 id 拼进路由段）', async () => {
    await notificationApi.remind('88')

    const call = only()
    expect(call.url).toBe('/Notification/Remind')
    expect(call.body).toBeUndefined()
    expect(call.config).toEqual({ params: { id: '88' } })
  })

  it('任务执行走 POST /Task/RunTask，主键以 basicId 放在请求体', async () => {
    await taskApi.run('66')

    expect(only()).toMatchObject({ method: 'POST', url: '/Task/RunTask', body: { basicId: '66' } })
  })

  it('会话撤销走 POST /UserSession/RevokeUserSession——Revoke 不在动词剥离表内', async () => {
    await userSessionApi.revokeSession({ basicId: '5' } as never)

    expect(only()).toMatchObject({ method: 'POST', url: '/UserSession/RevokeUserSession' })
  })

  it('版本权限撤销是 POST 而不是 DELETE，id 走 query', async () => {
    await tenantEditionPermissionApi.revoke('9')

    expect(only()).toEqual({
      method: 'POST',
      url: '/TenantEdition/RevokeTenantEditionPermission',
      body: undefined,
      config: { params: { id: '9' } },
    })
  })

  it('第三方应用重置密钥走 POST，id 走 query', async () => {
    await oauthAppApi.regenerateSecret('3')

    expect(only()).toEqual({
      method: 'POST',
      url: '/OAuthApp/RegenerateOAuthAppSecret',
      body: undefined,
      config: { params: { id: '3' } },
    })
  })

  it('租户初始化数据库走 POST /Tenant/InitializeDatabase，id 走 query', async () => {
    await tenantApi.initializeDatabase('7')

    expect(only()).toEqual({
      method: 'POST',
      url: '/Tenant/InitializeDatabase',
      body: undefined,
      config: { params: { id: '7' } },
    })
  })

  it('机器人配置设为默认保留 SetDefaultBotConfig 全名', async () => {
    await botConfigApi.setDefault({ basicId: '1' } as never)

    expect(only().url).toBe('/BotConfig/SetDefaultBotConfig')
  })
})

describe('易写错的控制器归属', () => {
  // 回归锚点：重置密码曾在 userApi 与 userSecurityApi 上各挂一条（同端点、不同返回 DTO），
  // userApi 那条已删除，入口只剩 userSecurityApi.resetPassword（= userManagementApi.security.resetPassword）。
  it('重置密码打的是 UserSecurity 控制器，不是 User——后端实现在 UserSecurityAppService', async () => {
    await userSecurityApi.resetPassword({ basicId: '1', newPassword: 'x' } as never)

    expect(only()).toMatchObject({ method: 'POST', url: '/UserSecurity/ResetUserPassword' })
  })

  it('userApi 上不再挂重置密码，避免同端点两个入口给出不同返回类型', () => {
    expect('resetPassword' in userApi).toBe(false)
    expect('resetPassword' in userManagementApi).toBe(false)
    expect(typeof userManagementApi.security.resetPassword).toBe('function')
  })

  it('用户部门归属的命令端是 UserDepartment 控制器（曾误写成 User 直接 404）', async () => {
    await userDepartmentApi.assign({ userId: '1', departmentId: '2' } as never)
    await userDepartmentApi.revoke('3')

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'POST /UserDepartment/UserDepartment',
      'DELETE /UserDepartment/UserDepartment',
    ])
  })

  it('切换租户打的是 Auth 控制器，不是 Tenant', async () => {
    await tenantApi.switchTenant({ tenantId: '10' } as never)

    expect(only()).toMatchObject({ method: 'POST', url: '/Auth/SwitchTenant' })
  })

  it('租户成员的写操作挂在 Tenant 控制器下，读操作才走 TenantMemberQuery', async () => {
    await tenantMemberApi.add({ userId: '1' } as never)
    await tenantMemberApi.invite({ email: 'a@b.c' } as never)
    await tenantMemberApi.detail('2')

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'POST /Tenant/TenantMember',
      'POST /Tenant/InviteTenantMember',
      'GET /TenantMemberQuery/TenantMemberDetail',
    ])
  })
})

describe('管理页聚合详情的查询参数名', () => {
  it('四个管理详情各自用后端形参名绑定，发 id 会绑不上退化成 0', async () => {
    await userManagementApi.detailView('1')
    await roleManagementApi.detailView('2')
    await permissionCenterApi.detailView('3')

    expect(calls.map(item => [item.url, item.config?.params])).toEqual([
      ['/UserManagementQuery/UserManagementDetail', { userId: '1' }],
      ['/RoleManagementQuery/RoleManagementDetail', { roleId: '2' }],
      ['/PermissionCenterQuery/PermissionCenterDetail', { permissionId: '3' }],
    ])
  })
})

describe('查询参数的空值裁剪与默认值', () => {
  it('菜单列表不传条件时发出空查询参数表，不拼出 ?Keyword= 这类空条件', async () => {
    await menuApi.list()

    expect(only()).toMatchObject({ method: 'GET', url: '/MenuQuery/MenuList' })
    expect(only().config?.params).toEqual({})
  })

  it('菜单列表把 null / 空串条件裁掉，false 与 0 保留', async () => {
    await menuApi.list({ keyword: '', parentId: null, isVisible: false, isExternal: true })

    expect(only().config?.params).toEqual({ IsVisible: false, IsExternal: true })
  })

  it('部门树的 Limit 无条件下发，OnlyEnabled 只在显式给值时下发', async () => {
    await departmentApi.tree({ limit: 50 })
    await departmentApi.tree({ limit: 50, onlyEnabled: false, keyword: '  ' })

    expect(calls.map(item => item.config?.params)).toEqual([
      { Limit: 50 },
      { Limit: 50, OnlyEnabled: false, Keyword: '  ' },
    ])
  })

  // 回归锚点：itemTree 曾直接拼对象字面量，缺字段时会把 undefined 写进 params；
  // 现改为与部门树/菜单树同一口径——逐字段 append，空值不入查询串，0 与 false 仍是有效取值。
  it('字典项树逐字段裁剪：0 与 false 照发，缺字段不写进查询串', async () => {
    await dictApi.itemTree({ dictId: '1', limit: 0, onlyEnabled: false })
    expect(only().config?.params).toEqual({ DictId: '1', Limit: 0, OnlyEnabled: false })

    calls.length = 0
    // DTO 上三个字段都是必填，但调用方常以类型断言绕过；此处用 toStrictEqual 才能查出 undefined 键
    await dictApi.itemTree({ dictId: '1' } as never)
    expect(only().config?.params).toStrictEqual({ DictId: '1' })
  })

  it('缓存键查询与批量删除的模式默认为 *', async () => {
    await cacheApi.getKeys()
    await cacheApi.removeByPattern()

    expect(calls.map(item => [item.method, item.url, item.config?.params])).toEqual([
      ['GET', '/Cache/Keys', { Pattern: '*' }],
      ['DELETE', '/Cache/ByPattern', { Pattern: '*' }],
    ])
  })

  it('缓存键存在性判断是 POST 但参数走 query，请求体为空', async () => {
    await cacheApi.exists('auth:token:1')

    const call = only()
    expect(call.method).toBe('POST')
    expect(call.body).toBeUndefined()
    expect(call.config?.params).toEqual({ Key: 'auth:token:1' })
  })

  it('服务器信息的两个开关不传时不进查询串，与其它树形/可选参数接口同口径', async () => {
    // 回归锚点：原实现直接拼 { IncludeDisk: undefined, IncludeNetwork: undefined }，
    // 与 dictApi.itemTree / departmentApi.tree / menuApi.tree 的 appendDynamicApiParam 裁剪口径不一致；
    // 开启接口签名后 query 串形态不一致会影响签名。
    // 必须用 toStrictEqual：toEqual 把「值为 undefined 的键」视同「键不存在」，改前改后都会绿。
    await serverApi.getServerInfo()

    expect(only().config?.params).toStrictEqual({})
  })

  it('服务器信息的开关显式传 false 时照常下发（false 是有效取值，不是空值）', async () => {
    await serverApi.getServerInfo({ includeDisk: false, includeNetwork: true })

    expect(only().config?.params).toStrictEqual({ IncludeDisk: false, IncludeNetwork: true })
  })
})

describe('站内信的请求体形状', () => {
  it('确认 / 已读 / 弹窗已展示三个动作把主键包成 { basicId } 上送', async () => {
    await userInboxApi.confirm('11')
    await userInboxApi.markRead('12')
    await userInboxApi.markPopupShown('13')

    expect(calls.map(item => [item.url, item.body])).toEqual([
      ['/UserInbox/Confirm', { basicId: '11' }],
      ['/UserInbox/MarkRead', { basicId: '12' }],
      ['/UserInbox/MarkPopupShown', { basicId: '13' }],
    ])
  })

  it('全部已读不带请求体', async () => {
    await userInboxApi.markAllRead()

    expect(only()).toMatchObject({ method: 'POST', url: '/UserInbox/MarkAllRead', body: undefined })
  })
})

describe('聚合门面复用的是同一批底层门面对象', () => {
  it('日志中心的八个入口就是审计模块的八个门面本身，不是拷贝', () => {
    expect(logManagementApi.permissionChanges).toBe(permissionChangeLogApi)
    expect(Object.keys(logManagementApi).sort()).toEqual(
      ['access', 'api', 'diff', 'exception', 'login', 'operation', 'permissionChanges', 'trace'],
    )
  })

  it('工作台收件箱直接复用站内信门面，不做二次封装', () => {
    expect(workbenchApi.inbox).toBe(userInboxApi)
  })
})
