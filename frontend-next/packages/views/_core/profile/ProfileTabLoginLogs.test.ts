/**
 * 个人中心「登录日志」（ProfileTabLoginLogs.vue，4.x 原样搬入）：
 * 结果标签用本地文案（6.0 没有枚举元数据，4.x 取后端 LoginResult），认证失败类用危险配色；
 * 说明行拼 IP（没有即「未知 IP」）、浏览器与系统；超过一页才出分页，翻页按页码重取；加载失败提示后端文案并显示空态。
 */
import type { AppContextApis, LoginAuditResult, LoginLogItem } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { SchemaPagination } from '~/components'
import { toast } from '~/composables'
import { i18n } from '~/locales'
import { ApiError } from '~/request'
import { registerAppContext } from '~/stores'
import ProfileTabLoginLogs from './ProfileTabLoginLogs.vue'

const apis = {
  getLoginLogsApi: vi.fn(),
}

const RESULTS: LoginAuditResult[] = ['AccountDisabled', 'AccountLocked', 'Failed', 'ImpersonationEnded', 'ImpersonationStarted', 'InvalidCredentials', 'Logout', 'MfaBound', 'MfaUnbound', 'PasswordChanged', 'PasswordReset', 'RequiresTwoFactor', 'RolesActivated', 'SessionRevoked', 'Success', 'TenantSwitched', 'TokenRefreshed', 'TwoFactorFailed']

function page(items: LoginLogItem[], total = items.length) {
  return { items, total, page: 1, pageSize: 10 }
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
  for (const fn of Object.values(apis))
    fn.mockReset()
  registerAppContext({ apis: apis as unknown as AppContextApis })
  apis.getLoginLogsApi.mockResolvedValue(page([
    { loginTime: '2026-10-30T01:00:00Z', loginIp: '10.0.0.1', browser: 'Chrome', os: 'Windows', loginResult: 'Success' },
    { loginTime: '2026-10-30T00:00:00Z', loginResult: 'InvalidCredentials', message: '用户名或密码错误。' },
    { loginTime: '2026-10-29T00:00:00Z', loginIp: '10.0.0.2', loginResult: 'RolesActivated' },
  ]))
})

async function render() {
  const wrapper = mount(ProfileTabLoginLogs, { global: { plugins: [i18n] } })
  await flushPromises()
  return wrapper
}

function text(value: string) {
  return value.replace(/\s+/g, ' ').trim()
}

describe('登录日志页签', () => {
  it('按结果显示本地文案与失败原因，认证失败用危险配色；没有 IP 显示「未知 IP」', async () => {
    const wrapper = await render()
    expect(apis.getLoginLogsApi).toHaveBeenCalledWith(1, 10)
    const rows = wrapper.findAll('.pf-log-row').map(row => ({
      result: row.find('.pf-list-title [data-part="label"]').text(),
      message: row.find('.pf-log-message').exists() ? row.find('.pf-log-message').text() : null,
      desc: text(row.find('.pf-list-desc').text()),
      danger: row.find('.pf-list-icon--danger').exists(),
    }))
    expect(rows).toEqual([
      { result: '成功', message: null, desc: '10.0.0.1 · Chrome · Windows', danger: false },
      { result: '密码错误', message: '用户名或密码错误。', desc: '未知 IP', danger: true },
      { result: '切换角色', message: null, desc: '10.0.0.2', danger: false },
    ])
    expect(wrapper.findComponent(SchemaPagination).exists()).toBe(false)
  })

  it('每种结果都有自己的文案（含 6.0 新增的切换角色）', async () => {
    apis.getLoginLogsApi.mockResolvedValue(page(RESULTS.map(loginResult => ({ loginTime: '2026-10-30T00:00:00Z', loginResult }))))
    const wrapper = await render()
    const labels = wrapper.findAll('.pf-log-row .pf-list-title').map(title => title.text())
    expect(labels).toHaveLength(RESULTS.length)
    expect(new Set(labels).size).toBe(RESULTS.length)
    expect(labels.some(label => label.includes('component.profile'))).toBe(false)
  })

  it('超过一页才出分页，翻页按页码重取；加载失败提示后端文案并显示空态', async () => {
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    apis.getLoginLogsApi.mockResolvedValue(page([{ loginTime: '2026-10-30T00:00:00Z', loginResult: 'Logout' }], 12))
    const wrapper = await render()
    const pagination = wrapper.findComponent(SchemaPagination)
    expect(pagination.exists()).toBe(true)

    apis.getLoginLogsApi.mockRejectedValue(new ApiError(400, { code: 'KERNEL_VALIDATION_000', detail: '页码超出范围。' }))
    pagination.vm.$emit('update:page', 2)
    await flushPromises()
    expect(apis.getLoginLogsApi).toHaveBeenLastCalledWith(2, 10)
    expect(danger).toHaveBeenCalledWith('页码超出范围。')
    expect(wrapper.find('.pf-empty').text()).toContain('暂无登录记录')
  })
})
