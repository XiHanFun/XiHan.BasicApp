import type { PluginView } from '@/modules/plugins/runtime'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { findPluginPage, loadedPlugins, nestTexts, pluginPermission, pluginRoute, pluginViewSource } from '@/modules/plugins/runtime'
import { i18n } from '~/locales'

const ops = vi.hoisted(() => ({
  list: vi.fn(),
}))

vi.mock('~/contracts', async importOriginal => ({
  ...await importOriginal<typeof import('~/contracts')>(),
  pluginViews: ops,
}))

afterEach(() => {
  vi.clearAllMocks()
})

// 示例插件的权限码：插件加载后才登记，不在导出的契约里
const READ = 'crm.crm-customers.read'
const DELETE = 'crm.crm-customers.delete'

function crm(): PluginView {
  return {
    name: 'crm',
    manifestId: 'xihan.samples.crm',
    version: '1.0.0',
    pages: [
      {
        code: 'crm.customers',
        route: '/plugins/crm/customers',
        component: 'plugins/crm/customers/index',
        titleKey: 'crm.customers.title',
        order: 10,
        permission: READ,
        viewKind: 'schema',
        viewJson: '{"list":"crm-customers.list","columns":[{"key":"name","titleKey":"crm.customers.name"}]}',
      },
    ],
    operations: [
      {
        id: 'crm-customers.delete',
        method: 'DELETE',
        path: '/api/crm-customers/{id:guid}',
        permission: DELETE,
      },
      {
        id: 'crm-customers.list',
        method: 'GET',
        path: '/api/crm-customers',
        permission: READ,
      },
      {
        id: 'crm-customers.ping',
        method: 'post',
        path: '/api/crm-customers:ping',
        permission: null,
      },
    ],
    texts: {
      'crm.customers.title': '客户',
      'crm.customers.name': '名称',
      'crm.customers.email_help': '写成 ada@contoso.test 这样',
    },
  }
}

describe('插件文案', () => {
  it('点号键还原成嵌套对象，@ 按字面量写出', () => {
    expect(nestTexts(crm().texts)).toEqual({
      crm: {
        customers: {
          title: '客户',
          name: '名称',
          email_help: '写成 ada{\'@\'}contoso.test 这样',
        },
      },
    })
  })
})

describe('插件操作的路由', () => {
  it('路由值取自模板，约束去掉，方法一律大写', () => {
    expect(pluginRoute(crm(), 'crm-customers.delete')).toEqual({
      method: 'DELETE',
      template: '/api/crm-customers/{id}',
      path: ['id'],
      query: [],
      success: 200,
    })
    expect(pluginRoute(crm(), 'crm-customers.ping').method).toBe('POST')
  })

  it('没发布到 HTTP 的操作直接报错', () => {
    expect(() => pluginRoute(crm(), 'crm-customers.export')).toThrow(/not published over HTTP/)
  })

  it('按钮的权限取操作要的权限码，没有要求为 undefined', () => {
    expect(pluginPermission(crm(), 'crm-customers.delete')).toBe(DELETE)
    expect(pluginPermission(crm(), 'crm-customers.ping')).toBeUndefined()
    expect(pluginPermission(crm(), null)).toBeUndefined()
  })
})

describe('插件页面的视图来源', () => {
  it('菜单里没有插件页面就不取描述', async () => {
    await pluginViewSource.prepare(['identity/users/nope/index'])
    expect(ops.list).not.toHaveBeenCalled()
    expect(loadedPlugins()).toEqual([])
  })

  it('取到描述后按组件路径给出加载器，文案并进当前区域', async () => {
    ops.list.mockResolvedValue({
      plugins: [
        crm(),
      ],
    })
    await pluginViewSource.prepare(['plugins/crm/customers/index'])

    expect(ops.list).toHaveBeenCalledOnce()
    expect(findPluginPage('plugins/crm/customers/index')?.page.code).toBe('crm.customers')
    expect(typeof pluginViewSource.resolve('plugins/crm/customers/index')).toBe('function')
    expect(pluginViewSource.resolve('plugins/crm/orders/index')).toBeUndefined()
    expect(i18n.global.te('crm.customers.title')).toBe(true)
  })

  it('取描述失败时插件页面解析不到，记下原因', async () => {
    const logged = vi.spyOn(console, 'error').mockImplementation(() => {})
    ops.list.mockRejectedValue(new Error('offline'))
    await pluginViewSource.prepare(['plugins/crm/customers/index'])

    expect(loadedPlugins()).toEqual([])
    expect(pluginViewSource.resolve('plugins/crm/customers/index')).toBeUndefined()
    expect(logged).toHaveBeenCalledOnce()
    logged.mockRestore()
  })
})
