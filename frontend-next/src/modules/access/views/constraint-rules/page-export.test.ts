import type { ConstraintRuleRow } from './constraint-rule'
import type { PageSchema } from '~/components'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { toExportFields } from '~/components/schema/selectors'
import { toCsv } from '~/components/schema/useSchemaExport'
import { i18n } from '~/locales'
import { toListRow } from './constraint-rule'
import ConstraintRulesPage from './index.vue'

function pageSchema(): PageSchema {
  const wrapper = mount(ConstraintRulesPage, { global: { plugins: [i18n], stubs: { SchemaPage: true } } })
  return wrapper.findComponent({ name: 'SchemaPage' }).props('schema') as PageSchema
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
})

describe('约束规则的导出', () => {
  it('按读权限出导出按钮，是本地 CSV（4.x 同），不提交到导出中心', () => {
    const schema = pageSchema()
    expect(schema.exportPermission).toBe('access.constraint-rules.read')
    expect(schema.resource.export).toBeUndefined()
  })

  it('规则项数从规则项算出，枚举写文案', () => {
    const row = {
      code: 'ssd-1',
      name: '出纳与会计',
      type: 'Ssd',
      targetType: 'Role',
      violationAction: 'Deny',
      items: [{ targetId: 'r1' }, { targetId: 'r2' }],
      priority: 5,
      status: 'Active',
      isEffective: true,
      description: null,
      createdAt: null,
    } as unknown as ConstraintRuleRow
    const [, line] = toCsv(toExportFields(pageSchema(), () => true), [toListRow(row)]).replace('﻿', '').split('\r\n')
    const cells = line!.split(',')
    expect(cells[0]).toBe('ssd-1')
    expect(cells[5]).toBe('2')
    expect(cells[6]).toBe('5')
    expect(cells).not.toContain('Deny')
    expect(cells).not.toContain('Active')
  })
})
