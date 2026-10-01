/**
 * 日期选择封装与组件库 3.0.0 的接法。
 *
 * 1. showTime：输入行多出时、分两段，分隔按段类型出；选出的时刻随时间戳上抛，不再归零到当日零点。
 * 2. 字段接线落在 role=group 的段位组上；输入行同时有清空钮与日历钮。
 * 3. 区间受控值填到一半时交给本地草稿：组件库收到的仍是那一半，上游只收两端齐备或 null。
 */
import { mount } from '@vue/test-utils'
import { XhDatePickerRoot, XhDateRangePickerRoot, XhFieldControl, XhFieldLabel, XhFieldRoot } from '@xihan-ui/vue'
import { describe, expect, it } from 'vitest'
import { h } from 'vue'
import { i18n } from '~/locales'
import XDatePicker from './XDatePicker.vue'
import XDateRangePicker from './XDateRangePicker.vue'

const withI18n = { plugins: [i18n] }

function literals(group: ReturnType<ReturnType<typeof mount>['find']>): string[] {
  return group.findAll('span').map(span => span.text() || span.element.textContent || '')
}

describe('xDatePicker', () => {
  it('showTime 时输入行铺年月日时分，分隔按段类型出', () => {
    const wrapper = mount(XDatePicker, {
      props: { value: new Date(2026, 9, 1, 9, 30).getTime(), showTime: true },
      global: withI18n,
    })
    const group = wrapper.find('[data-scope="date-picker"][data-part="segment-group"]')
    expect(group.findAll('[data-part="segment"]')).toHaveLength(5)
    expect(literals(group)).toEqual(['-', '-', ' ', ':'])
    expect(wrapper.findComponent(XhDatePickerRoot).props('value')).toEqual(['2026-10-01T09:30'])
  })

  it('不开 showTime 时只铺日期段，值取日历日', () => {
    const wrapper = mount(XDatePicker, { props: { value: new Date(2026, 9, 1, 9, 30).getTime() }, global: withI18n })
    const group = wrapper.find('[data-scope="date-picker"][data-part="segment-group"]')
    expect(group.findAll('[data-part="segment"]')).toHaveLength(3)
    expect(wrapper.findComponent(XhDatePickerRoot).props('value')).toEqual(['2026-10-01'])
  })

  it('选出的时刻随时间戳上抛，清空上抛 null', async () => {
    const wrapper = mount(XDatePicker, { props: { showTime: true }, global: withI18n })
    const root = wrapper.findComponent(XhDatePickerRoot)
    root.vm.$emit('update:value', ['2026-10-01T18:05'])
    root.vm.$emit('update:value', [])
    expect(wrapper.emitted('update:value')).toEqual([[new Date(2026, 9, 1, 18, 5).getTime()], [null]])
  })

  it('min / max 按时间戳换算成组件库的 ISO 串', () => {
    const wrapper = mount(XDatePicker, {
      props: { max: new Date(2026, 9, 1, 15, 0).getTime(), min: new Date(1900, 0, 1).getTime() },
      global: withI18n,
    })
    const root = wrapper.findComponent(XhDatePickerRoot)
    expect(root.props('max')).toBe('2026-10-01')
    expect(root.props('min')).toBe('1900-01-01')
  })

  it('字段的 id 与名字落在段位组上，输入行有清空钮与日历钮', () => {
    const wrapper = mount({
      render: () => h(XhFieldRoot, null, () => [
        h(XhFieldLabel, null, () => '生日'),
        h(XhFieldControl, null, () => h(XDatePicker, { value: null })),
      ]),
    }, { global: withI18n })
    const label = wrapper.find('label')
    const group = wrapper.find('[data-scope="date-picker"][data-part="segment-group"]')
    expect(group.attributes('role')).toBe('group')
    expect(group.attributes('id')).toBe(label.attributes('for'))
    expect(group.attributes('aria-labelledby')).toBe(label.attributes('id'))
    expect(wrapper.find('[data-scope="date-picker"][data-part="control"]').attributes('aria-label')).toBeUndefined()
    expect(wrapper.find('[data-scope="date-picker"][data-part="clear-trigger"]').exists()).toBe(true)
    expect(wrapper.find('[data-scope="date-picker"][data-part="trigger"]').exists()).toBe(true)
  })
})

describe('xDateRangePicker', () => {
  const start = new Date(2026, 8, 1).getTime()
  const end = new Date(2026, 8, 30).getTime()

  it('只填一端时不上抛，那一半仍交回组件库', async () => {
    const wrapper = mount(XDateRangePicker, { props: { value: null }, global: withI18n })
    const root = wrapper.findComponent(XhDateRangePickerRoot)

    root.vm.$emit('update:value', ['2026-09-01', ''])
    await wrapper.vm.$nextTick()
    expect(wrapper.emitted('update:value')).toBeUndefined()
    expect(root.props('value')).toEqual(['2026-09-01', ''])

    root.vm.$emit('update:value', ['2026-09-01', '2026-09-30'])
    await wrapper.vm.$nextTick()
    expect(wrapper.emitted('update:value')).toEqual([[[start, end]]])
  })

  it('只填了终点时同样留在草稿里', async () => {
    const wrapper = mount(XDateRangePicker, { props: { value: null }, global: withI18n })
    const root = wrapper.findComponent(XhDateRangePickerRoot)
    root.vm.$emit('update:value', ['', '2026-09-30'])
    await wrapper.vm.$nextTick()
    expect(wrapper.emitted('update:value')).toBeUndefined()
    expect(root.props('value')).toEqual(['', '2026-09-30'])
  })

  it('清空上抛 null；上游改值时草稿跟着上游走', async () => {
    const wrapper = mount(XDateRangePicker, { props: { value: [start, end] as [number, number] }, global: withI18n })
    const root = wrapper.findComponent(XhDateRangePickerRoot)
    expect(root.props('value')).toEqual(['2026-09-01', '2026-09-30'])

    root.vm.$emit('update:value', [])
    expect(wrapper.emitted('update:value')).toEqual([[null]])

    root.vm.$emit('update:value', ['2026-09-01', ''])
    await wrapper.setProps({ value: null })
    expect(root.props('value')).toEqual([])
  })

  it('两组段位与日历钮都在输入行里', () => {
    const wrapper = mount(XDateRangePicker, { props: { value: null }, global: withI18n })
    expect(wrapper.findAll('[data-scope="date-range-picker"][data-part="segment-group"]')).toHaveLength(2)
    expect(wrapper.find('[data-scope="date-range-picker"][data-part="trigger"]').exists()).toBe(true)
  })
})
