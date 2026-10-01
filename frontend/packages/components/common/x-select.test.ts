/**
 * 下拉选择 XSelect 按组件库的部件结构摆：多选标签、在途与空态、读屏名。
 *
 * 手写触发器曾经漏掉标签行，多选只显示一串截断的「a, b, c」，maxTagCount 不起作用；
 * 加载中与无选项时又拿自绘的转圈 / 空状态顶掉了 list，触发器的 aria-controls 指向一个不存在的节点。
 */
import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it } from 'vitest'
import { i18n } from '~/locales'
import XSelect from './XSelect.vue'

const options = [
  { label: '启用', value: 1 },
  { label: '停用', value: 2 },
  { label: '锁定', value: 3 },
]

type Props = InstanceType<typeof XSelect>['$props']

function mountSelect(props: Props = {}, attrs: Record<string, unknown> = {}) {
  return mount(XSelect, {
    props: { options, ...props },
    attrs,
    global: { plugins: [i18n] },
    attachTo: document.body,
  })
}

function part(name: string): HTMLElement | null {
  return document.querySelector<HTMLElement>(`[data-scope="select"][data-part="${name}"]`)
}

async function open(wrapper: ReturnType<typeof mountSelect>) {
  await wrapper.find('[data-part="trigger"]').trigger('click')
  await flushPromises()
}

afterEach(() => {
  document.body.innerHTML = ''
})

describe('xSelect 多选标签', () => {
  it('已选项排成标签，超出 maxTagCount 合成 +N', async () => {
    const wrapper = mountSelect({ value: [1, 2, 3], multiple: true, maxTagCount: 2 })
    await flushPromises()

    const tags = wrapper.findAll('[data-scope="tag"][data-value]').map(tag => tag.text())
    expect(tags).toEqual(['启用', '停用'])
    expect(wrapper.find('[data-scope="tag"][data-count]').text()).toBe('+1')
  })

  it('单选不出标签行', async () => {
    const wrapper = mountSelect({ value: 1 })
    await flushPromises()

    expect(wrapper.find('[data-part="tag-list"]').exists()).toBe(false)
    expect(wrapper.find('[data-part="value-text"]').text()).toBe('启用')
  })
})

describe('xSelect 在途与空态', () => {
  it('加载中列表照样在，报 aria-busy，选项不消失', async () => {
    const wrapper = mountSelect({ value: 1, loading: true })
    await open(wrapper)

    const list = part('list')
    expect(list).not.toBeNull()
    expect(wrapper.find('[data-part="trigger"]').attributes('aria-controls')).toBe(list?.id)
    expect(list?.getAttribute('aria-busy')).toBe('true')
    expect(list?.querySelectorAll('[data-part="item"]').length).toBe(3)
    // 已有选项时后台刷新保留上一帧，在途占位让位
    expect(part('loading')?.hidden).toBe(true)
  })

  it('首次加载、还没有选项时露出在途占位', async () => {
    const wrapper = mountSelect({ options: [], loading: true })
    await open(wrapper)

    expect(part('loading')?.hidden).toBe(false)
    expect(part('empty')?.hidden).toBe(true)
  })

  it('没有选项时露出空态，列表仍在', async () => {
    const wrapper = mountSelect({ options: [] })
    await open(wrapper)

    expect(part('list')).not.toBeNull()
    expect(part('empty')?.hidden).toBe(false)
    expect(part('empty')?.textContent?.trim()).toBe(i18n.global.t('common.no_data'))
  })
})

describe('xSelect 取值与三态', () => {
  it('选中后按原类型收上来：数字选项不会变成字符串', async () => {
    const wrapper = mountSelect({ value: null })
    await open(wrapper)

    document.querySelectorAll<HTMLElement>('[data-scope="select"][data-part="item"]')[1]!.click()
    await flushPromises()

    expect(wrapper.emitted('update:value')?.at(-1)).toEqual([2])
  })

  it('readOnly 与 invalid 落到触发器上', async () => {
    const wrapper = mountSelect({ readOnly: true, invalid: true })
    await flushPromises()

    const trigger = wrapper.find('[data-part="trigger"]')
    expect(trigger.attributes('aria-readonly')).toBe('true')
    expect(trigger.attributes('aria-invalid')).toBe('true')
  })
})

describe('xSelect 读屏名', () => {
  it('aria-label 放进视觉隐藏的 label 部件，触发器的 aria-labelledby 指得到它', async () => {
    const wrapper = mountSelect({ value: 1 }, { 'aria-label': '模拟范围' })
    await flushPromises()

    const label = wrapper.find('[data-scope="select"][data-part="label"]')
    const trigger = wrapper.find('[data-part="trigger"]')
    expect(label.text()).toBe('模拟范围')
    expect(trigger.attributes('aria-labelledby')?.split(' ')).toContain(label.attributes('id'))
    expect(trigger.attributes('aria-label')).toBeUndefined()
  })
})
