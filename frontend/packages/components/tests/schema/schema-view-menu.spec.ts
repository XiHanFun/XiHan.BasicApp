/**
 * 条件预设浮层：点名称套用、悬停看它存的条件、眼睛预览（下面那块换成预设存的条件，再点还原）、星标切换默认、删除；
 * 「当前条件」先起名（第一行，回车即保存）、再列出当前的筛选、排序与每页条数（保存的就是这些），右上角保存（同名的写「覆盖」）；同步标记跟「条件预设」开关
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

async function open(activeCode?: string, current: ViewSnapshot = empty, list: PersonalView[] = views) {
  const wrapper = mount(SchemaViewMenu, {
    props: {
      views: list,
      activeCode,
      fields,
      current,
    },
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
  for (const dt of document.body.querySelectorAll('.schema-views__conditions dt:not(.schema-views__name-label)')) {
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

  it('还没有预设时预设条件一栏只写「无」', async () => {
    const wrapper = await open(undefined, empty, [])

    expect(document.body.querySelector('.schema-views__list')).toBeNull()
    expect(document.body.querySelector('.schema-views__empty')!.textContent!.trim()).toBe(t('component.schema_views.none'))
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

  it('点眼睛预览：下面那块换成预设存的条件，名称行收起、保存置灰；再点一次还原当前条件', async () => {
    const wrapper = await open()
    const sectionTitles = () => [
      ...document.body.querySelectorAll('.schema-views__section'),
    ].map(h => h.textContent!.trim())

    button(t('component.schema_views.preview', { name: '本周失败的' })).click()
    await flushPromises()
    expect(sectionTitles()[1]).toBe(t('component.schema_views.preview_title', { name: '本周失败的' }))
    expect(button(t('component.schema_views.preview_back')).getAttribute('aria-pressed')).toBe('true')
    expect(currentConditions()).toEqual({
      [t('component.schema_views.filters')]: ['状态：失败'],
      [t('component.schema_views.sorts')]: ['创建时间：降序'],
    })
    expect(document.body.querySelector('input[name="viewName"]')).toBeNull()
    expect(button(t('component.schema_views.save')).disabled).toBe(true)

    button(t('component.schema_views.preview_back')).click()
    await flushPromises()
    expect(sectionTitles()[1]).toBe(t('component.schema_views.current'))
    expect(document.body.querySelector('input[name="viewName"]')).not.toBeNull()
    expect(currentConditions()).toEqual({
      [t('component.schema_views.filters')]: [
        t('component.schema_views.none'),
      ],
      [t('component.schema_views.sorts')]: [
        t('component.schema_views.none'),
      ],
      [t('component.schema_views.page_size')]: [
        t('component.schema_views.page_size_value', { n: 20 }),
      ],
    })
    expect(wrapper.emitted('apply')).toBeUndefined()
    wrapper.unmount()
  })

  it('底部有一段说明，与表格设置同款', async () => {
    const wrapper = await open()

    expect(document.body.querySelector('.schema-views__hint')!.textContent!.trim()).toBe(t('component.schema_views.hint'))
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

  it('已存的预设与当前条件各有小标题；名称是当前条件的第一行，在筛选之前；保存钮在标题行', async () => {
    const wrapper = await open()

    expect([
      ...document.body.querySelectorAll('.schema-views__section'),
    ].map(h => h.textContent!.trim())).toEqual([
      t('component.schema_views.saved'),
      t('component.schema_views.current'),
    ])

    const labels = [
      ...document.body.querySelectorAll('.schema-views__conditions dt'),
    ].map(dt => dt.textContent!.trim())
    expect(labels.slice(0, 2)).toEqual([
      t('component.schema_views.name_label'),
      t('component.schema_views.filters'),
    ])
    const head = document.body.querySelector('.schema-views__body')!.firstElementChild!
    expect(head.contains(button(t('component.schema_views.save')))).toBe(true)
    expect(document.body.querySelector('form')).toBeNull()
    wrapper.unmount()
  })

  it('在名称框里回车即保存，没填名称时回车不保存', async () => {
    const wrapper = await open()
    const input = document.body.querySelector<HTMLInputElement>('input[name="viewName"]')!

    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }))
    await flushPromises()
    expect(wrapper.emitted('save')).toBeUndefined()

    input.value = ' 本月 '
    input.dispatchEvent(new Event('input'))
    await flushPromises()
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }))
    await flushPromises()
    expect(wrapper.emitted('save')).toEqual([
      ['本月'],
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
