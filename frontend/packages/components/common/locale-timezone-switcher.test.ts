/**
 * 语言 / 时区切换的行内下拉形态（偏好设置、个人中心）。
 *
 * 语言下拉自绘了选中态却没放值文本与 label 部件，触发器的 aria-labelledby 指向两个不存在的节点，读屏念不出名字；
 * 时区下拉的输入框受控却从空串起步，选中的时区在收起时显示成一个空输入框，要点进去再点出来才出字。
 */
import type { AppContextApis } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { i18n } from '~/locales'
import { registerAppContext } from '~/stores/app-context'
import LocaleSwitcher from './LocaleSwitcher.vue'
import TimezoneSwitcher from './TimezoneSwitcher.vue'

beforeEach(() => {
  setActivePinia(createPinia())
  document.body.innerHTML = ''
})

/** aria-labelledby 里每个 id 都要在文档里找得到，再拼出念给读屏的名字 */
function nameFromLabelledBy(el: Element): string {
  const ids = el.getAttribute('aria-labelledby')?.split(' ').filter(Boolean) ?? []
  return ids.map((id) => {
    const target = document.getElementById(id)
    expect(target, `aria-labelledby 指向的 #${id} 不存在`).not.toBeNull()
    return target!.textContent!.trim()
  }).join(' ')
}

describe('localeSwitcher 行内下拉的读屏名', () => {
  it('触发器的名字由隐藏 label 与值文本拼成，两个 id 都落得到节点', async () => {
    const wrapper = mount(LocaleSwitcher, {
      props: { variant: 'select', value: 'en-US' },
      global: { plugins: [i18n] },
      attachTo: document.body,
    })
    await flushPromises()

    const trigger = wrapper.find('[data-scope="select"][data-part="trigger"]').element
    expect(nameFromLabelledBy(trigger)).toBe(`${i18n.global.t('preference.general.language')} ${i18n.global.t('header.locale.en_us')}`)
    wrapper.unmount()
  })
})

describe('timezoneSwitcher 行内下拉', () => {
  it('收起时输入框直接显示选中的时区，目录到了换成带偏移的全称', async () => {
    let resolve!: (list: { id: string, baseUtcOffsetMinutes: number }[]) => void
    registerAppContext({
      apis: {
        timeZoneApi: { options: () => new Promise((r) => { resolve = r }) },
      } as unknown as AppContextApis,
    })

    const wrapper = mount(TimezoneSwitcher, {
      props: { variant: 'select', value: 'Asia/Shanghai' },
      global: { plugins: [i18n] },
      attachTo: document.body,
    })
    await flushPromises()

    const input = wrapper.find('input').element as HTMLInputElement
    // 目录未到时 withCurrent 先补上当前值，输入框不空着
    expect(input.value).toBe('Asia/Shanghai')

    resolve([{ id: 'UTC', baseUtcOffsetMinutes: 0 }, { id: 'Asia/Shanghai', baseUtcOffsetMinutes: 480 }])
    await flushPromises()

    expect(input.value).toBe('Asia/Shanghai (UTC+08:00)')
    expect(nameFromLabelledBy(input)).toBe(i18n.global.t('preference.general.timezone'))
    expect(input.placeholder).toBe(i18n.global.t('header.timezone.search'))
    wrapper.unmount()
  })
})
