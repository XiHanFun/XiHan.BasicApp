/**
 * 列表页的排序从哪来（SchemaPage）
 *
 * 表格设置里每列的升 / 降箭头是这页的默认排序，个人视图存的是保存时正在用的排序：
 * 首次加载时有默认视图用视图的，没有用默认排序；开着同步时默认排序以账号那份为准（换了设备、本地没有缓存也一样）；
 * 搜索区「重置」清空筛选后回到默认排序，不是变成不排序
 */
import type { PageSchema, SchemaQueryParams } from '~/components/schema/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import SchemaPage from '~/components/schema/SchemaPage.vue'
import { i18n } from '~/locales'
import { storage } from '~/utils'

/** 账号上的那份设置：按分区给，首次加载前由 replyFromServer 一次性送到 */
const server = vi.hoisted(() => ({
  sections: {} as Record<string, unknown>,
  pending: [] as Array<() => void>,
}))

vi.mock('~/components/schema/useUserSettingSync', async () => {
  const { ref: vueRef } = await import('vue')
  return {
    useUserSettingSync: () => ({
      hydrate: async () => undefined,
      save: vi.fn(),
      subscribeRemote: () => () => {},
      bindSection: (section: string, target: { accept: (value: unknown) => boolean, apply: (value: unknown) => void, writeLocal: (value: unknown) => void }) => {
        const ready = vueRef(false)
        let settle!: () => void
        const settled = new Promise<void>((resolve) => {
          settle = resolve
        })
        server.pending.push(() => {
          const value = server.sections[section]
          if (value !== undefined && target.accept(value)) {
            target.apply(value)
            target.writeLocal(value)
          }
          ready.value = true
          settle()
        })
        return {
          ready,
          settled,
          dispose: () => {},
        }
      },
    }),
  }
})

async function replyFromServer() {
  for (const reply of server.pending.splice(0)) {
    reply()
  }
  await flushPromises()
}

const PAGE_CODE = 'sorts-test'

function schema(page: (params: SchemaQueryParams) => Promise<{ items: unknown[], total: number }>): PageSchema {
  return {
    pageCode: PAGE_CODE,
    pageName: 'Sorts',
    resource: { page },
    fields: [
      {
        key: 'name',
        title: 'Name',
        dataType: 'string',
        sortable: true,
        searchable: true,
      },
      {
        key: 'createdAt',
        title: 'Created',
        dataType: 'datetime',
        sortable: true,
      },
    ],
  } as unknown as PageSchema
}

async function mountPage() {
  const page = vi.fn(async (_params: SchemaQueryParams) => ({ items: [], total: 0 }))
  const wrapper = mount(SchemaPage, {
    props: {
      schema: schema(page),
    },
    global: {
      plugins: [i18n],
    },
    attachTo: document.body,
  })
  await flushPromises()
  return { wrapper, page }
}

const createdDesc = {
  columns: [
    { key: 'name', visible: true },
    { key: 'createdAt', visible: true, sort: 'desc' },
  ],
}

beforeEach(() => {
  setActivePinia(createPinia())
  server.sections = {}
  server.pending = []
})

afterEach(() => {
  localStorage.clear()
  document.body.innerHTML = ''
})

describe('schemaPage 的排序来源', () => {
  it('换了设备、本地没有缓存：首次加载用账号上的默认排序', async () => {
    server.sections.table = createdDesc
    const { page } = await mountPage()
    await replyFromServer()

    expect(page).toHaveBeenCalledTimes(1)
    expect(page.mock.calls[0]![0].sorts).toEqual([
      { field: 'createdAt', order: 'desc' },
    ])
  })

  it('有默认视图时首次加载用视图里的排序，视图没存排序就不排序', async () => {
    server.sections.table = createdDesc
    server.sections.views = [
      {
        code: 'view-1',
        name: 'All',
        isDefault: true,
        snapshot: { filters: {}, sorts: [] },
      },
    ]
    const { page } = await mountPage()
    await replyFromServer()

    expect(page).toHaveBeenCalledTimes(1)
    expect(page.mock.calls[0]![0].sorts).toEqual([])
  })

  it('搜索区重置：清空筛选后回到默认排序', async () => {
    storage.set(`xh:table-settings:${PAGE_CODE}`, createdDesc)
    const { wrapper, page } = await mountPage()
    await replyFromServer()
    expect(page.mock.calls.at(-1)![0].sorts).toEqual([
      { field: 'createdAt', order: 'desc' },
    ])

    await wrapper.find(`button[aria-label="${i18n.global.t('component.search_panel.reset')}"]`).trigger('click')
    await flushPromises()

    expect(page.mock.calls.at(-1)![0]).toMatchObject({
      page: 1,
      sorts: [
        { field: 'createdAt', order: 'desc' },
      ],
    })
  })
})
