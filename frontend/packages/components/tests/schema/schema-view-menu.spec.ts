/**
 * 条件预设浮层：点名称套用、悬停看它存的条件、星标切换默认、删除；
 * 底部列出当前的筛选、排序与每页条数（保存的就是这些），起名保存（同名的按钮写「覆盖」）；同步标记跟「条件预设」开关
 */
import type { ListFieldSchema } from '~/components/schema/types'
import type { PersonalView, ViewSnapshot } from '~/components/schema/useViewManager'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import SchemaViewMenu from '~/components/schema/SchemaViewMenu.vue'
import { i18n } from '~/locales'
import { useAppStore } from '~/stores'

const t = i18n.global.t

const fields: ListFieldSchema[] = [
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: [
      { label: '失败', value: 'Failed' },
      { label: '成功', value: 'Succeeded' },
    ],
  },
  {
    key: 'createdAt',
    title: '创建时间',
    dataType: 'datetime',
    searchable: true,
    searchRange: true,
    sortable: true,
  },
  {
    key: 'size',
    title: '大小',
    dataType: 'number',
    sortable: true,
  },
]

const views: PersonalView[] = [
  {
    code: 'view-1',
    name: '本周失败的',
    snapshot: {
      filters: {
        status: ['Failed'],
      },
      sorts: [
        { field: 'createdAt', order: 'desc' },
      ],
    },
    isDefault: true,
  },
  {
    code: 'view-2',
    name: '大文件',
    snapshot: { filters: {} },
  },
]

const empty: ViewSnapshot = { filters: {}, sorts: [], pageSize: 20 }

function button(name: string): HTMLButtonElement {
  const found = [
    ...document.body.querySelectorAll('button'),
  ].find(element => element.getAttribute('aria-label') === name || element.textContent?.trim() === name)
  if (!found)
    throw new Error(`no button ${name}`)
  return found
}

async function open(activeCode?: string, current: ViewSnapshot = empty) {
  const wrapper = mount(SchemaViewMenu, {
    props: { views, activeCode, fields, current },
    global: {
      plugins: [i18n],
    },
    attachTo: document.body,
  })
  button(t('component.schema_views.title')).click()
  await flushPromises()
  return wrapper
}

/** 当前条件一栏：名称 → 各行标签的文字（没有标签时取「无」） */
function currentConditions(): Record<string, string[]> {
  const result: Record<string, string[]> = {}
  for (const dt of document.body.querySelectorAll('.schema-views__conditions dt')) {
    const dd = dt.nextElementSibling!
    const lines = [
      ...dd.querySelectorAll('.schema-views__tag'),
    ].map(item => item.textContent!.trim())
    result[dt.textContent!.trim()] = lines.length
      ? lines
      : [
          dd.textContent!.trim(),
        ]
  }
  return result
}

beforeEach(() => {
  setActivePinia(createPinia())
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('schemaViewMenu', () => {
  it('点名称套用，当前预设标为 aria-current；悬停看它存的筛选与排序', async () => {
    const wrapper = await open('view-2')

    expect(button('大文件').getAttribute('aria-current')).toBe('true')
    expect(button('本周失败的').hasAttribute('aria-current')).toBe(false)
    expect(button('本周失败的').title).toBe(t('component.schema_views.summary', { filters: '状态：失败', sorts: '创建时间：降序' }))
    expect(button('大文件').title).toBe(t('component.schema_views.summary', {
      filters: t('component.schema_views.none'),
      sorts: t('component.schema_views.none'),
    }))
    button('本周失败的').click()
    expect(wrapper.emitted('apply')).toEqual([
      ['view-1'],
    ])
    wrapper.unmount()
  })

  it('列出当前的筛选、排序与每页条数：字段名与选项名、日期区间、多选、升降序，空的筛选不列', async () => {
    const wrapper = await open(undefined, {
      filters: {
        status: ['Failed', 'Succeeded'],
        createdAt: [
          Date.UTC(2026, 10, 1),
          Date.UTC(2026, 10, 7),
        ],
        keyword: '',
        size: null,
      },
      sorts: [
        { field: 'size', order: 'asc' },
        { field: 'createdAt', order: 'desc' },
      ],
      pageSize: 50,
    })

    expect(currentConditions()).toEqual({
      [t('component.schema_views.filters')]: ['状态：失败或成功', '创建时间：2026-11-01 ~ 2026-11-07'],
      [t('component.schema_views.sorts')]: ['大小：升序', '创建时间：降序'],
      [t('component.schema_views.page_size')]: [
        t('component.schema_views.page_size_value', { n: 50 }),
      ],
    })
    wrapper.unmount()
  })

  it('没有筛选也没有排序时写「无」', async () => {
    const wrapper = await open()

    expect(currentConditions()).toMatchObject({
      [t('component.schema_views.filters')]: [
        t('component.schema_views.none'),
      ],
      [t('component.schema_views.sorts')]: [
        t('component.schema_views.none'),
      ],
    })
    wrapper.unmount()
  })

  it('同步标记跟「条件预设」开关，不跟表格设置', async () => {
    const store = useAppStore()
    store.tableSyncEnabled = true
    store.presetSyncEnabled = false
    const wrapper = await open()

    const head = document.body.querySelector('.schema-views__body')!.firstElementChild!
    expect(head.textContent).toContain(t('preference.sync_status.local'))
    wrapper.unmount()
  })

  it('星标：默认的再点即取消，别的设为默认；删除按名称', async () => {
    const wrapper = await open()

    button(t('component.schema_views.unset_default', { name: '本周失败的' })).click()
    button(t('component.schema_views.set_default', { name: '大文件' })).click()
    button(t('component.schema_views.remove', { name: '大文件' })).click()
    expect(wrapper.emitted('setDefault')).toEqual([
      [null],
      ['view-2'],
    ])
    expect(wrapper.emitted('remove')).toEqual([
      ['view-2'],
    ])
    wrapper.unmount()
  })

  it('起名保存，名称去空白；同名时按钮写覆盖；没填名称不能保存', async () => {
    const wrapper = await open()
    const input = document.body.querySelector<HTMLInputElement>('input[name="viewName"]')!

    expect(button(t('component.schema_views.save')).disabled).toBe(true)
    input.value = ' 大文件 '
    input.dispatchEvent(new Event('input'))
    await flushPromises()
    expect(button(t('component.schema_views.overwrite')).disabled).toBe(false)

    input.value = ' 新的 '
    input.dispatchEvent(new Event('input'))
    await flushPromises()
    button(t('component.schema_views.save')).click()
    await flushPromises()
    expect(wrapper.emitted('save')).toEqual([
      ['新的'],
    ])
    wrapper.unmount()
  })
})
