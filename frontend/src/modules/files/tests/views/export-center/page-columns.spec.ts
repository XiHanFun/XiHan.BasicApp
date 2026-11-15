import type { Component } from 'vue'
import type { PageSchema } from '~/components'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import process from 'node:process'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { KNOWN_BUSINESS_TYPES } from '@/modules/files/views/export-center/export-center'
import { toExportFields } from '~/components/schema/selectors'
import { i18n } from '~/locales'

interface SchemaNode {
  $ref?: string
  type?: string | string[]
  items?: SchemaNode
  properties?: Record<string, SchemaNode>
  oneOf?: SchemaNode[]
  anyOf?: SchemaNode[]
  allOf?: SchemaNode[]
}

const document = JSON.parse(readFileSync(resolve(process.cwd(), 'contracts/openapi.json'), 'utf8')) as {
  paths: Record<string, Record<string, { operationId?: string, responses?: Record<string, { content?: Record<string, { schema?: SchemaNode }> }> }>>
  components: { schemas: Record<string, SchemaNode> }
}

function deref(node: SchemaNode | undefined): SchemaNode | undefined {
  if (!node)
    return undefined
  if (node.$ref)
    return deref(document.components.schemas[node.$ref.replace('#/components/schemas/', '')])
  const variant = [
    ...(node.oneOf ?? []),
    ...(node.anyOf ?? []),
    ...(node.allOf ?? []),
  ].map(deref).find(item => item?.properties || item?.items)
  return variant ?? node
}

/** 列表操作响应里一行的模式 */
function rowSchemaOf(operationId: string): SchemaNode | undefined {
  for (const methods of Object.values(document.paths)) {
    for (const operation of Object.values(methods)) {
      if (operation.operationId !== operationId)
        continue
      const success = Object.entries(operation.responses ?? {}).find(([code]) => /^2\d\d$/.test(code))?.[1]
      return deref(deref(deref(success?.content?.['application/json']?.schema)?.properties?.items)?.items)
    }
  }
  return undefined
}

/** 路径能不能在一行的模式里走通：深一层用点号、数组用下标，`|` 分隔的备选有一条走通即可 */
function resolves(row: SchemaNode, key: string): boolean {
  return key.split('|').some((path) => {
    let node: SchemaNode | undefined = row
    for (const segment of path.split('.')) {
      node = /^\d+$/.test(segment)
        ? deref(node?.items)
        : deref(node?.properties?.[segment])
      if (!node)
        return false
    }
    return true
  })
}

/** 声明了导出的页面视图与页签组件（一页里可以有几张 SchemaPage） */
const sources = import.meta.glob<string>(['@/modules/*/views/**/index.vue', '@/modules/*/views/**/components/*Tab.vue'], { eager: true, query: '?raw', import: 'default' })
const views = import.meta.glob<{ default: Component }>(['@/modules/*/views/**/index.vue', '@/modules/*/views/**/components/*Tab.vue'], { eager: true })

function exportCenterSchemas(): PageSchema[] {
  const schemas: PageSchema[] = []
  for (const [path, module] of Object.entries(views)) {
    if (!sources[path]?.includes('exportPermission'))
      continue
    const wrapper = mount(module.default, {
      global: {
        plugins: [i18n],
        stubs: { SchemaPage: true },
      },
    })
    for (const page of wrapper.findAllComponents({ name: 'SchemaPage' })) {
      const schema = page.props('schema') as PageSchema
      if (schema?.resource?.export)
        schemas.push(schema)
    }
    wrapper.unmount()
  }
  return schemas
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
})

describe('接了导出中心的列表页', () => {
  it('导出的列都能在列表操作的响应里取到值，业务类型都认得', () => {
    const schemas = exportCenterSchemas()
    const missing: string[] = []
    for (const schema of schemas) {
      const center = schema.resource.export!
      const row = rowSchemaOf(center.businessType)
      expect(row?.properties, center.businessType).toBeDefined()
      for (const field of toExportFields(schema, () => true)) {
        const path = center.columnPaths?.[field.key]
        if (path === null)
          continue
        if (!resolves(row!, path ?? field.key)) {
          missing.push(`${center.businessType}: ${field.key}${path
            ? ` → ${path}`
            : ''}`)
        }
      }
    }
    expect(missing).toEqual([])
    // 字典项要先选中字典、短信要切到页签才渲染，其余都在页面打开时就有
    const mounted = new Set(schemas.map(schema => schema.resource.export!.businessType))
    expect(KNOWN_BUSINESS_TYPES.filter(type => !mounted.has(type))).toEqual(['sms.list', 'dict-items.list'])
    expect([...mounted].filter(type => !(KNOWN_BUSINESS_TYPES as readonly string[]).includes(type))).toEqual([])
  })
})
