/**
 * 灵动岛一条活动的展开视图：整条点击、操作与移除各管各的，图标栏与内容块按调用方给的画
 */
import type { IslandTask } from '~/composables/useDynamicIsland'
import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import DynamicIslandActivity from '~/components/common/DynamicIslandActivity.vue'
import { i18n } from '~/locales'

function task(patch: Partial<IslandTask> = {}): IslandTask {
  return {
    id: 't',
    label: '车辆状态',
    state: 'info',
    order: 1,
    kind: 'event',
    source: 'local',
    startedAt: 0,
    ...patch,
  }
}

function render(props: { task: IslandTask, variant?: 'card' | 'row', clickable?: boolean }) {
  return mount(DynamicIslandActivity, {
    props: {
      variant: 'row',
      clickable: false,
      now: 0,
      ...props,
    },
    global: {
      plugins: [i18n],
    },
  })
}

describe('灵动岛活动视图的点击', () => {
  it('可点击时整条由一枚带名字的按钮承接', async () => {
    const wrapper = render({
      task: task(),
      clickable: true,
    })

    const hit = wrapper.get('.dia__hit')
    expect(hit.attributes('aria-label')).toBe('车辆状态')
    await hit.trigger('click')
    expect(wrapper.emitted('open')).toHaveLength(1)
  })

  it('不可点击时没有整条按钮', () => {
    expect(render({
      task: task(),
    }).find('.dia__hit').exists()).toBe(false)
  })

  it('点操作只发操作，不当成整条点击', async () => {
    const handler = vi.fn()
    const action = {
      key: 'lock',
      label: '上锁',
      icon: 'lucide:lock',
      handler,
    }
    const wrapper = render({
      task: task({
        actions: [action],
      }),
      clickable: true,
    })

    await wrapper.get('.dia__actions button').trigger('click')

    expect(wrapper.emitted('action')?.[0]).toEqual([action])
    expect(wrapper.emitted('open')).toBeUndefined()
  })

  it('图标栏的按钮只画图标，名字留给读屏；没给图标的退回文字', () => {
    const wrapper = render({
      task: task({
        actionLayout: 'bar',
        actions: [
          {
            key: 'lock',
            label: '上锁',
            icon: 'lucide:lock',
            handler: () => {},
          },
          {
            key: 'trunk',
            label: '开后备箱',
            handler: () => {},
          },
        ],
      }),
    })

    const buttons = wrapper.findAll('.dia__actions.is-bar button')
    expect(buttons.map(button => button.attributes('aria-label'))).toEqual(['上锁', '开后备箱'])
    expect(buttons[0]?.text()).toBe('')
    expect(buttons[1]?.text()).toBe('开后备箱')
  })

  it('面板行里非常驻的可移除，常驻的与卡片里的不给移除钮', async () => {
    const row = render({
      task: task(),
    })
    await row.get('.dia__dismiss button').trigger('click')
    expect(row.emitted('dismiss')).toHaveLength(1)

    expect(render({
      task: task({ persistent: true }),
    }).find('.dia__dismiss').exists()).toBe(false)
    expect(render({
      task: task(),
      variant: 'card',
    }).find('.dia__dismiss').exists()).toBe(false)
    expect(render({
      task: task({ persistent: true, dismissible: true }),
    }).find('.dia__dismiss').exists()).toBe(true)
  })
})

describe('灵动岛活动视图的内容', () => {
  it('内容块按顺序画数值、进度、列表、网格与自定义组件', () => {
    const Widget = defineComponent({
      render: () => h('span', { class: 'widget' }, '自定义'),
    })
    const wrapper = render({
      task: task({
        blocks: [
          {
            type: 'metric',
            value: 323,
            unit: 'km',
            caption: '续航',
          },
          { type: 'progress', value: 60, label: '电量' },
          {
            type: 'list',
            items: [
              {
                key: 'dy',
                label: '抖音',
                value: '31分钟',
                progress: 90,
              },
            ],
          },
          {
            type: 'grid',
            items: [
              { key: 'mon', label: '周一', value: '24°/19°' },
              { key: 'tue', label: '周二', value: '24°/20°' },
            ],
          },
          { type: 'custom', component: Widget },
        ],
      }),
    })

    expect(wrapper.get('.dib-metric__value').text()).toBe('323km')
    expect(wrapper.get('.dib-metric__caption').text()).toBe('续航')
    expect(wrapper.get('.dib-progress__value').text()).toBe('60%')
    expect(wrapper.get('.dib-list__label').text()).toBe('抖音')
    expect(wrapper.get('.dib-list__value').text()).toBe('31分钟')
    expect(wrapper.findAll('.dib-grid__cell')).toHaveLength(2)
    expect(wrapper.get('.dib-grid').attributes('style')).toContain('--dib-grid-columns: 2')
    expect(wrapper.get('.widget').text()).toBe('自定义')
  })

  it('右侧小件与已用时按调用方给的画，elapsed: false 时不显示已用时', () => {
    const shown = mount(DynamicIslandActivity, {
      props: {
        task: task({
          state: 'loading',
          trailing: { type: 'elapsed' },
        }),
        variant: 'row',
        clickable: false,
        now: 65_000,
      },
      global: {
        plugins: [i18n],
      },
    })
    expect(shown.get('.dia__head .di-acc__text').text()).toBe('1:05')
    expect(shown.find('.dia__meta').exists()).toBe(true)

    const hidden = render({
      task: task({ state: 'loading', elapsed: false }),
    })
    expect(hidden.find('.dia__meta').exists()).toBe(false)
  })
})
