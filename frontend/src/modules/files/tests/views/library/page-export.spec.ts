import type { PageSchema } from '~/components'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import LibraryPage from '@/modules/files/views/library/index.vue'
import { toExportFields } from '~/components/schema/selectors'
import { i18n } from '~/locales'

function pageSchema(): PageSchema {
  const wrapper = mount(LibraryPage, {
    global: {
      plugins: [i18n],
      stubs: { SchemaPage: true },
    },
  })
  return wrapper.findComponent({ name: 'SchemaPage' }).props('schema') as PageSchema
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
})

describe('文件库的导出', () => {
  it('按读权限出导出按钮，是本地 CSV，不提交到导出中心', () => {
    const schema = pageSchema()
    expect(schema.exportPermission).toBe('files.files.read')
    expect(schema.resource.export).toBeUndefined()
  })

  it('字段对齐列表的列；图标列没有值，不导出', () => {
    expect(toExportFields(pageSchema(), () => true).map(field => field.key)).toEqual([
      'originalName',
      'name',
      'kind',
      'size',
      'status',
      'accessLevel',
      'isTemporary',
      'extension',
      'contentType',
      'downloadCount',
      'createdAt',
    ])
  })
})
