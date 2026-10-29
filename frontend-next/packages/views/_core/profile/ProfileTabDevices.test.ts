/**
 * 个人中心「登录设备」（ProfileTabDevices.vue，4.x 原样搬入）：
 * 当前会话打「当前」标签、其余给「踢下线」；标题取浏览器（6.0 不给设备名），说明行拼 IP、地点、系统与最近活动；
 * 踢下线确认后按会话主键下线并重拉列表，被拒即提示后端文案；登出其他设备先确认，没有其他设备只提示。
 */
import type { AppContextApis, UserSessionItem } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { dialog, toast } from '~/composables'
import { i18n } from '~/locales'
import { ApiError } from '~/request'
import { registerAppContext } from '~/stores'
import { formatDate } from '~/utils'
import ProfileTabDevices from './ProfileTabDevices.vue'

const apis = {
  getSessionsApi: vi.fn(),
  revokeSessionApi: vi.fn(),
  revokeOtherSessionsApi: vi.fn(),
}

const current: UserSessionItem = { sessionId: 's-1', deviceType: 3, browser: 'Chrome', operatingSystem: 'Windows', ipAddress: '10.0.0.1', loginTime: '2026-10-30T00:00:00Z', lastActivityTime: '2026-10-30T02:00:00Z', isCurrent: true }
const phone: UserSessionItem = { sessionId: 's-2', deviceType: 2, operatingSystem: 'iOS', ipAddress: '10.0.0.2', location: '上海', loginTime: '2026-10-29T00:00:00Z', lastActivityTime: '2026-10-29T03:04:00Z', isCurrent: false }

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
  for (const fn of Object.values(apis))
    fn.mockReset()
  registerAppContext({ apis: apis as unknown as AppContextApis })
  apis.getSessionsApi.mockResolvedValue([current, phone])
})

const mounted: Array<{ unmount: () => void }> = []

afterEach(() => {
  for (const wrapper of mounted.splice(0))
    wrapper.unmount()
})

/** 踢下线的确认浮层传送到 body 下，挂到文档上才点得到 */
async function render() {
  const wrapper = mount(ProfileTabDevices, { global: { plugins: [i18n] }, attachTo: document.body })
  mounted.push(wrapper)
  await flushPromises()
  return wrapper
}

function text(value: string) {
  return value.replace(/\s+/g, ' ').trim()
}

function rows(wrapper: Awaited<ReturnType<typeof render>>) {
  return wrapper.findAll('.pf-list-item').map(item => ({
    title: text(item.find('.pf-list-title').text()),
    desc: text(item.find('.pf-list-desc').text()),
    tag: item.find('.pf-list-body + *').text(),
  }))
}

describe('登录设备页签', () => {
  it('当前会话打「当前」、显示在线；其余标题没有浏览器即「未知设备」，说明行带地点、系统与最近活动，可以踢下线', async () => {
    const wrapper = await render()
    expect(rows(wrapper)).toEqual([
      { title: 'Chrome', desc: '10.0.0.1 · Windows · 在线', tag: '当前' },
      { title: '未知设备', desc: `10.0.0.2 · 上海 · iOS · ${formatDate(phone.lastActivityTime, 'MM-DD HH:mm')}`, tag: '踢下线' },
    ])
    expect(wrapper.findAll('.pf-list-item')[0]!.classes()).toContain('pf-list-item--active')
  })

  it('踢下线确认后按会话主键下线并重拉列表；被拒即提示后端文案', async () => {
    const success = vi.spyOn(toast, 'success').mockImplementation(() => '')
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    apis.revokeSessionApi.mockResolvedValue({ items: [current] })
    const wrapper = await render()

    async function kick() {
      await wrapper.findAll('.pf-list-item')[1]!.find('button').trigger('click')
      await flushPromises()
      const confirm = document.body.querySelector<HTMLButtonElement>('[data-scope="popconfirm"][data-part="content"][data-state="open"] [data-part="confirm-trigger"]')
      expect(confirm?.textContent).toBe('确认')
      confirm!.click()
      await flushPromises()
    }

    await kick()
    expect(apis.revokeSessionApi).toHaveBeenCalledWith('s-2')
    expect(success).toHaveBeenCalledWith('设备已登出')
    expect(apis.getSessionsApi).toHaveBeenCalledTimes(2)

    apis.revokeSessionApi.mockRejectedValue(new ApiError(404, { code: 'IDENTITY_NOTFOUND_002', detail: '会话不存在。' }))
    await kick()
    expect(danger).toHaveBeenCalledWith('会话不存在。')
    expect(apis.getSessionsApi).toHaveBeenCalledTimes(2)
  })

  it('登出其他设备先确认（带着其他设备的数量）再下线；只有当前设备时只提示', async () => {
    const success = vi.spyOn(toast, 'success').mockImplementation(() => '')
    const info = vi.spyOn(toast, 'info').mockImplementation(() => '')
    const confirm = vi.spyOn(dialog, 'confirm').mockImplementation(async (options) => {
      await options.onOk?.()
      return true
    })
    apis.revokeOtherSessionsApi.mockResolvedValue({ items: [current] })
    const wrapper = await render()
    const button = () => wrapper.findAll('.pf-section__extra button').find(item => item.text() === '登出其他设备')!

    await button().trigger('click')
    await flushPromises()
    expect(confirm).toHaveBeenCalledWith(expect.objectContaining({ title: '登出所有设备', content: '将下线除当前设备外的 1 个设备，是否继续？' }))
    expect(apis.revokeOtherSessionsApi).toHaveBeenCalledTimes(1)
    expect(success).toHaveBeenCalledWith('已登出所有其他设备')

    apis.getSessionsApi.mockResolvedValue([current])
    await wrapper.find('.pf-section__extra button').trigger('click')
    await flushPromises()
    await button().trigger('click')
    expect(info).toHaveBeenCalledWith('没有其他在线设备')
    expect(confirm).toHaveBeenCalledTimes(1)
  })
})
