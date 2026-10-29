/**
 * 个人中心「通知」（ProfileTabNotifications.vue，4.x 原样搬入）：
 * 接收渠道只有站内信、邮件、短信，通知类型只有公告、待办、安全（4.x 的推送、机器人、审批、营销 6.0 没有效果，不显示）；
 * 开关照后端的偏好打开；保存把六项整体交给壳层、换成保存后的偏好并提示，重置即重新取；加载与保存被拒即提示后端文案。
 */
import type { AppContextApis, NotificationPreference } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { toast } from '~/composables'
import { i18n } from '~/locales'
import { ApiError } from '~/request'
import { registerAppContext } from '~/stores'
import ProfileTabNotifications from './ProfileTabNotifications.vue'

const apis = {
  getNotificationPreferenceApi: vi.fn(),
  updateNotificationPreferenceApi: vi.fn(),
}

const saved: NotificationPreference = { channelInApp: true, channelEmail: false, channelSms: true, typeAnnouncement: false, typeTask: true, typeSecurity: true }

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
  for (const fn of Object.values(apis))
    fn.mockReset()
  registerAppContext({ apis: apis as unknown as AppContextApis })
  apis.getNotificationPreferenceApi.mockResolvedValue({ ...saved })
})

async function render() {
  const wrapper = mount(ProfileTabNotifications, { global: { plugins: [i18n] } })
  await flushPromises()
  return wrapper
}

function rows(wrapper: Awaited<ReturnType<typeof render>>) {
  return wrapper.findAll('.pf-section').map(section => section.findAll('.pf-list-item').map(item => ({
    title: item.find('.pf-list-title').text(),
    checked: item.find('[role="switch"]').attributes('aria-checked'),
  })))
}

function button(wrapper: Awaited<ReturnType<typeof render>>, label: string) {
  return wrapper.findAll('.pf-section__actions button').find(item => item.text() === label)!
}

describe('通知页签', () => {
  it('渠道只列站内信、邮件、短信，类型只列公告、待办、安全，开关照后端的偏好', async () => {
    const wrapper = await render()
    expect(apis.getNotificationPreferenceApi).toHaveBeenCalledTimes(1)
    expect(rows(wrapper)).toEqual([
      [
        { title: '站内信', checked: 'true' },
        { title: '邮箱通知', checked: 'false' },
        { title: '短信通知', checked: 'true' },
      ],
      [
        { title: '系统公告', checked: 'false' },
        { title: '任务提醒', checked: 'true' },
        { title: '安全告警', checked: 'true' },
      ],
    ])
    expect(wrapper.text()).not.toContain('component.profile')
  })

  it('改了开关再保存：六项整体交给壳层，换成保存后的偏好并提示；重置即重新取', async () => {
    const success = vi.spyOn(toast, 'success').mockImplementation(() => '')
    apis.updateNotificationPreferenceApi.mockResolvedValue({ ...saved, channelEmail: true, typeTask: false })
    const wrapper = await render()

    await wrapper.findAll('.pf-list-item')[1]!.find('[role="switch"]').trigger('click')
    expect(rows(wrapper)[0]![1]).toEqual({ title: '邮箱通知', checked: 'true' })
    await button(wrapper, '保存偏好').trigger('click')
    await flushPromises()
    expect(apis.updateNotificationPreferenceApi).toHaveBeenCalledWith({ ...saved, channelEmail: true })
    expect(success).toHaveBeenCalledWith('通知偏好已保存')
    expect(rows(wrapper)[1]![1]).toEqual({ title: '任务提醒', checked: 'false' })

    await button(wrapper, '重置').trigger('click')
    await flushPromises()
    expect(apis.getNotificationPreferenceApi).toHaveBeenCalledTimes(2)
    expect(rows(wrapper)[1]![1]).toEqual({ title: '任务提醒', checked: 'true' })
  })

  it('加载与保存被拒即提示后端文案', async () => {
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    apis.getNotificationPreferenceApi.mockRejectedValue(new ApiError(404, { code: 'IDENTITY_NOTFOUND_001', detail: '用户不存在。' }))
    const wrapper = await render()
    expect(danger).toHaveBeenCalledWith('用户不存在。')

    apis.updateNotificationPreferenceApi.mockRejectedValue(new ApiError(400, { code: 'KERNEL_VALIDATION_000', detail: '请求无效。' }))
    await button(wrapper, '保存偏好').trigger('click')
    await flushPromises()
    expect(danger).toHaveBeenLastCalledWith('请求无效。')
  })
})
