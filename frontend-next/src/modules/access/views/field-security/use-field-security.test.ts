import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fieldSecurity } from '~/contracts'
import { QueryOperator } from '~/types/contracts'
import { fieldSecurityResource, flattenDepartments, toDefinition, useFieldRuleEditor } from './use-field-security'

const users = {
  code: 'identity.users',
  titleKey: 'identity.users.title',
  fields: [
    { name: 'displayName', titleKey: 'identity.users.fields.display_name', isText: true, isNullable: false },
    { name: 'email', titleKey: 'identity.users.fields.email', isText: true, isNullable: true },
    { name: 'lastLoginAt', titleKey: 'identity.users.fields.last_login_at', isText: false, isNullable: true },
  ],
}

function form(overrides: Partial<Parameters<typeof toDefinition>[0]> = {}): Parameters<typeof toDefinition>[0] {
  return { targetKind: 'Role', targetId: 'r-1', entity: 'identity.users', field: 'email', strategy: 'Hidden', keepHead: 3, keepTail: 4, replacement: ' [secret] ', isEditable: true, status: 'Active', remark: '  ', ...overrides }
}

describe('field security definition', () => {
  it('only sends the parameters the strategy uses, and a plain rule is always read-only', () => {
    expect(toDefinition(form())).toMatchObject({ keepHead: null, keepTail: null, replacement: null, isEditable: true, remark: null })
    expect(toDefinition(form({ strategy: 'PartialMask' }))).toMatchObject({ keepHead: 3, keepTail: 4, replacement: null })
    expect(toDefinition(form({ strategy: 'Redact' }))).toMatchObject({ keepHead: null, replacement: '[secret]' })
    expect(toDefinition(form({ strategy: 'None' })).isEditable).toBe(false)
  })

  it('lists active departments with their path, keeping the children of a disabled one', () => {
    const node = (id: string, name: string, status: 'Active' | 'Disabled', children: unknown[] = []) => ({ department: { id, name, status }, memberCount: 0, children })
    const options = flattenDepartments([node('hq', 'HQ', 'Active', [node('old', 'Old', 'Disabled', [node('rd', 'R&D', 'Active')])])] as never)
    expect(options).toEqual([
      { value: 'hq', label: 'HQ', description: 'HQ' },
      { value: 'rd', label: 'R&D', description: 'HQ / Old / R&D' },
    ])
  })
})

describe('field rule editor', () => {
  it('offers only the strategies the field supports and falls back when the field changes', () => {
    const editor = useFieldRuleEditor(code => (code === users.code ? users : undefined))
    editor.changeEntity('identity.users')
    editor.changeField('displayName')
    expect(editor.strategyAllowed('Hidden')).toBe(false)
    expect(editor.form.strategy).toBe('None')
    expect(editor.form.isEditable).toBe(false)
    editor.changeStrategy('Hash')
    editor.changeField('lastLoginAt')
    expect(editor.form.strategy).toBe('Hidden')
    expect(editor.strategyAllowed('FullMask')).toBe(false)
    editor.changeEntity('identity.users')
    expect(editor.form.field).toBe('lastLoginAt')
  })
})

describe('field security resource', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('sends multi-select searches as in filters and toggles status by a whole update', async () => {
    const list = vi.spyOn(fieldSecurity, 'list').mockResolvedValue({ items: [], total: 0, page: { index: 1, size: 20 } })
    await fieldSecurityResource().page!({ page: 1, pageSize: 20, filters: { keyword: 'mail' }, conditionFilters: [{ field: 'strategy', operator: QueryOperator.In, values: ['Hidden', 'Hash'] }] })
    expect(list.mock.calls[0]![0]).toMatchObject({ keyword: 'mail', filters: [{ field: 'strategy', operator: 'in', value: 'Hidden,Hash' }] })

    const row = { id: 'f-1', targetKind: 'User', targetId: 'u-1', targetName: 'Finn', entity: 'identity.users', field: 'email', strategy: 'FullMask', keepHead: null, keepTail: null, replacement: null, isEditable: false, status: 'Active', remark: 'r', createdAt: '2026-10-29T00:00:00Z' } as const
    vi.spyOn(fieldSecurity, 'get').mockResolvedValue(row)
    const update = vi.spyOn(fieldSecurity, 'update').mockResolvedValue({ ...row, status: 'Disabled' })
    await fieldSecurityResource().updateStatus!('f-1', false)
    expect(update.mock.calls[0]![0]).toEqual({ id: 'f-1', targetKind: 'User', targetId: 'u-1', entity: 'identity.users', field: 'email', strategy: 'FullMask', keepHead: null, keepTail: null, replacement: null, isEditable: false, status: 'Disabled', remark: 'r' })
  })
})
