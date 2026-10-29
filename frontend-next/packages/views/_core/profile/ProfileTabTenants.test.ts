/**
 * 个人中心「我的租户」（ProfileTabTenants.vue，4.x 原样搬入）：
 * 注册地（没有成员类型）、所有者、成员各打身份标签，说明行带加入时间（注册地是账号建立的时间）；
 * 当前租户打「当前」、不给切换，其余切换交给 auth store（与控制中心同一个入口），被拒即提示后端文案；没有租户显示空态。
 */
import type { AppContextApis, AppTenantSwitcherItem } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { toast } from '~/composables'
import { i18n } from '~/locales'
import { ApiError } from '~/request'
import { registerAppContext, useAuthStore } from '~/stores'
import { formatDate } from '~/utils'
import ProfileTabTenants from './ProfileTabTenants.vue'

const apis = {
  tenantApi: { myAvailableTenants: vi.fn(), switchTenant: vi.fn() },
}

const tenants: AppTenantSwitcherItem[] = [
  { tenantId: 'acme', tenantName: 'Acme', isHome: true, memberType: null, isCurrent: true, joinedTime: '2026-01-02T12:00:00Z' },
  { tenantId: 'demo', tenantName: 'Demo Tenant', isHome: false, memberType: 'Owner', isCurrent: false, joinedTime: '2026-03-04T12:00:00Z' },
  { tenantId: 'beta', tenantName: 'Beta', isHome: false, memberType: 'Member', isCurrent: false, joinedTime: '2026-05-06T12:00:00Z' },
]

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
  apis.tenantApi.myAvailableTenants.mockReset()
  apis.tenantApi.switchTenant.mockReset()
  registerAppContext({ apis: apis as unknown as AppContextApis })
  apis.tenantApi.myAvailableTenants.mockResolvedValue(tenants)
})

async function render() {
  const wrapper = mount(ProfileTabTenants, { global: { plugins: [i18n] } })
  await flushPromises()
  return wrapper
}

function text(value: string) {
  return value.replace(/\s+/g, ' ').trim()
}

function joined(tenant: AppTenantSwitcherItem) {
  return `加入于 ${formatDate(tenant.joinedTime, 'YYYY-MM-DD')}`
}

describe('我的租户页签', () => {
  it('注册地、所有者、成员各打身份标签并带加入时间；当前租户打「当前」、不给切换', async () => {
    const wrapper = await render()
    const rows = wrapper.findAll('.pf-list-item').map(item => ({
      title: text(item.find('.pf-list-title').text()),
      desc: text(item.find('.pf-list-desc').text()),
      tag: item.find('.pf-tenant-actions [data-part="label"]').text(),
      action: item.find('.pf-tenant-actions button').exists() ? item.find('.pf-tenant-actions button').text() : null,
    }))
    expect(rows).toEqual([
      { title: 'Acme 当前', desc: `注册地 · ${joined(tenants[0]!)}`, tag: '注册地', action: null },
      { title: 'Demo Tenant', desc: `租户所有者 · ${joined(tenants[1]!)}`, tag: '租户所有者', action: '切换' },
      { title: 'Beta', desc: `普通成员 · ${joined(tenants[2]!)}`, tag: '普通成员', action: '切换' },
    ])
    expect(wrapper.findAll('.pf-list-item')[0]!.classes()).toContain('pf-list-item--active')
  })

  it('切换按租户标识交给 auth store；被拒即提示后端文案', async () => {
    const switchTenant = vi.spyOn(useAuthStore(), 'switchTenant').mockResolvedValue()
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    const wrapper = await render()
    await wrapper.findAll('.pf-list-item')[1]!.find('.pf-tenant-actions button').trigger('click')
    expect(switchTenant).toHaveBeenCalledWith('demo')
    expect(danger).not.toHaveBeenCalled()

    // 成功时整页重建、不复位切换中；这里换一个挂载再看失败
    const again = await render()
    switchTenant.mockRejectedValue(new ApiError(403, { code: 'TENANCY_DENIED_001', detail: '你不是该租户的成员。' }))
    await again.findAll('.pf-list-item')[2]!.find('.pf-tenant-actions button').trigger('click')
    await flushPromises()
    expect(switchTenant).toHaveBeenLastCalledWith('beta')
    expect(danger).toHaveBeenCalledWith('你不是该租户的成员。')
  })

  it('没有可访问的租户时显示空态', async () => {
    apis.tenantApi.myAvailableTenants.mockResolvedValue([])
    const wrapper = await render()
    expect(wrapper.text()).toContain(i18n.global.t('component.profile.tenants.empty'))
  })
})
