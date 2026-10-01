/**
 * 可搜索下拉 XCombobox 的取值与输入框文字。
 *
 * 组件库的 combobox 只管显示：输入框里写什么、候选怎么筛都归调用方。
 * 输入框受控后若不主动回填，选中项在收起时就是一个空输入框——时区选择器曾经就这样，
 * 要点进去再点出来才显示；远程检索也曾靠 Select 上不存在的 filterable / remote 透传，根本发不出去。
 */
import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { i18n } from '~/locales'
import XCombobox from './XCombobox.vue'

const options = [
  { label: '北京', value: 1 },
  { label: '上海', value: 2 },
  { label: '广州', value: 3, description: 'Guangzhou' },
]

type Props = InstanceType<typeof XCombobox>['$props']

function mountCombobox(props: Props = {}, attrs: Record<string, unknown> = {}) {
  return mount(XCombobox, {
    props: { options, ...props },
    attrs,
    global: { plugins: [i18n] },
    attachTo: document.body,
  })
}

/** 浮层经 Portal 挂在 body 上，条目与空态都到 document 里找 */
function visibleItems(): string[] {
  return [...document.querySelectorAll('[data-scope="combobox"][data-part="item"]')]
    .map(el => el.querySelector('[data-part="item-text"]')?.textContent?.trim() ?? '')
}

function part(name: string): HTMLElement | null {
  return document.querySelector<HTMLElement>(`[data-scope="combobox"][data-part="${name}"]`)
}

async function open(wrapper: ReturnType<typeof mountCombobox>) {
  await wrapper.find('input').trigger('click')
  await flushPromises()
}

async function type(wrapper: ReturnType<typeof mountCombobox>, text: string) {
  await wrapper.find('input').setValue(text)
  await flushPromises()
}

afterEach(() => {
  document.body.innerHTML = ''
  vi.useRealTimers()
})

describe('xCombobox 输入框文字', () => {
  it('收起时输入框显示选中项的文字，不必先点进去', async () => {
    const wrapper = mountCombobox({ value: 2 })
    await flushPromises()

    expect((wrapper.find('input').element as HTMLInputElement).value).toBe('上海')
  })

  it('选项晚到时跟着补上文字', async () => {
    const wrapper = mountCombobox({ value: 2, options: [] })
    await flushPromises()
    await wrapper.setProps({ options })
    await flushPromises()

    expect((wrapper.find('input').element as HTMLInputElement).value).toBe('上海')
  })

  it('打字时显示打的字，失焦后复位成选中项文字', async () => {
    const wrapper = mountCombobox({ value: 2 })
    await open(wrapper)
    await type(wrapper, '广')

    expect((wrapper.find('input').element as HTMLInputElement).value).toBe('广')

    await wrapper.find('input').trigger('blur')
    await flushPromises()

    expect((wrapper.find('input').element as HTMLInputElement).value).toBe('上海')
  })
})

describe('xCombobox 取值', () => {
  it('选中后按原类型收上来：数字选项不会变成字符串', async () => {
    const wrapper = mountCombobox({ value: null })
    await open(wrapper)

    document.querySelectorAll<HTMLElement>('[data-scope="combobox"][data-part="item"]')[2]!.click()
    await flushPromises()

    expect(wrapper.emitted('update:value')?.at(-1)).toEqual([3])
  })

  it('打字筛出后选中：输入框换成选中项文字，再展开是完整清单而不是按这段文字筛过的', async () => {
    const wrapper = mountCombobox({
      'value': null,
      'onUpdate:value': (value: unknown) => wrapper.setProps({ value: value as number | null }),
    })
    await open(wrapper)
    await type(wrapper, '广')
    expect(visibleItems()).toEqual(['广州'])

    document.querySelector<HTMLElement>('[data-scope="combobox"][data-part="item"]')!.click()
    await flushPromises()

    expect((wrapper.find('input').element as HTMLInputElement).value).toBe('广州')

    await open(wrapper)
    expect(visibleItems()).toEqual(['北京', '上海', '广州'])
  })

  it('多选的已选项排成标签，超出 maxTagCount 合成 +N', async () => {
    const wrapper = mountCombobox({ value: [1, 2, 3], multiple: true, maxTagCount: 2 })
    await flushPromises()

    const tags = wrapper.findAll('[data-scope="tag"][data-value]').map(tag => tag.text())
    expect(tags).toEqual(['北京', '上海'])
    expect(wrapper.find('[data-scope="tag"][data-count]').text()).toBe('+1')
  })
})

describe('xCombobox 筛选与检索', () => {
  it('缺省在本地按文字与副文本筛', async () => {
    const wrapper = mountCombobox()
    await open(wrapper)
    expect(visibleItems()).toEqual(['北京', '上海', '广州'])

    await type(wrapper, 'guang')
    expect(visibleItems()).toEqual(['广州'])
  })

  it('筛不出时空态给出「没有匹配的结果」，不让浮层悄悄消失', async () => {
    const wrapper = mountCombobox()
    await open(wrapper)
    await type(wrapper, '纽约')

    expect(visibleItems()).toEqual([])
    expect(part('empty')?.hidden).toBe(false)
    expect(part('empty')?.textContent?.trim()).toBe(i18n.global.t('common.no_result'))
  })

  it('remote 时不在本地筛，停顿后发出一次 search，同一串不重复发', async () => {
    vi.useFakeTimers()
    const wrapper = mountCombobox({ remote: true })
    await open(wrapper)
    await type(wrapper, ' 广 ')

    expect(visibleItems()).toEqual(['北京', '上海', '广州'])
    expect(wrapper.emitted('search')).toBeUndefined()

    vi.advanceTimersByTime(300)
    expect(wrapper.emitted('search')).toEqual([['广']])

    await type(wrapper, '广')
    vi.advanceTimersByTime(300)
    expect(wrapper.emitted('search')).toEqual([['广']])
  })

  it('remote 换掉候选后，已选项的标签仍念得出名字，删掉另一枚时它照样按原类型收上来', async () => {
    const wrapper = mountCombobox({ remote: true, multiple: true, value: [1, 2] })
    await flushPromises()
    await wrapper.setProps({ options: [{ label: '深圳', value: 4 }] })
    await flushPromises()

    expect(wrapper.findAll('[data-scope="tag"][data-value]').map(tag => tag.text())).toEqual(['北京', '上海'])

    await wrapper.find('[data-scope="tag"][data-value="2"] button').trigger('click')
    await flushPromises()

    expect(wrapper.emitted('update:value')?.at(-1)).toEqual([[1]])
  })

  it('加载中且没有候选时露出在途占位、列表报 aria-busy', async () => {
    const wrapper = mountCombobox({ options: [], loading: true })
    await open(wrapper)

    expect(part('loading')?.hidden).toBe(false)
    expect(part('empty')?.hidden).toBe(true)
    expect(part('content')?.getAttribute('aria-busy')).toBe('true')
  })
})

describe('xCombobox 读屏名', () => {
  it('aria-label 放进视觉隐藏的 label 部件，输入框经 aria-labelledby 念得出来', async () => {
    const wrapper = mountCombobox({}, { 'aria-label': '所属城市' })
    await nextTick()

    const label = wrapper.find('[data-scope="combobox"][data-part="label"]')
    const input = wrapper.find('input')
    expect(label.text()).toBe('所属城市')
    expect(input.attributes('aria-labelledby')).toBe(label.attributes('id'))
    expect(input.attributes('aria-label')).toBeUndefined()
  })
})
