import type { PermissionConditionAttributeDto, PermissionConditionInventoryItemDto, PermissionConditionListItemDto } from '@/api'
import { describe, expect, it } from 'vitest'
import {
  ConditionOperator,
  ConditionValueKind,
  ConfigDataType,
  PermissionAction,
  ValidityStatus,
} from '@/api'
import {
  composeAttributeName,
  defaultValueType,
  groupConditions,
  inventoryToCsv,
  nextGroupNumber,
  parseKeyValueLines,
  requiresValue,
  resolveAttribute,
  semanticsKey,
  toGrantConditionTargets,
  valueHintKey,
  valueTypeOptionsFor,
} from './permission-condition'

function attribute(name: string, kind: ConditionValueKind, extra: Partial<PermissionConditionAttributeDto> = {}): PermissionConditionAttributeDto {
  return { name, kind, aliases: [], isFamily: name.endsWith('.*'), isLegacy: false, isMultiValued: false, isRowAttribute: false, operators: [], ...extra }
}

function condition(group: number, id: string): PermissionConditionListItemDto {
  return { basicId: id, conditionGroup: group } as PermissionConditionListItemDto
}

const catalog = [
  attribute('subject.roleCodes', ConditionValueKind.String, { aliases: ['subject.role'], isMultiValued: true }),
  attribute('environment.ip', ConditionValueKind.IpAddress),
  attribute('resource.query.*', ConditionValueKind.String),
]

describe('permission-condition', () => {
  it('只把有效授权列进条件抽屉，按权限码排序', () => {
    const targets = toGrantConditionTargets('role', [
      { basicId: '2', permissionId: '20', permissionCode: 'demo:user:read', permissionName: '用户查看', permissionAction: PermissionAction.Grant, conditionCount: 1, status: ValidityStatus.Valid },
      { basicId: '1', permissionId: '10', permissionCode: 'demo:role:read', permissionName: null, permissionAction: PermissionAction.Deny, conditionCount: 0, status: ValidityStatus.Valid },
      { basicId: '3', permissionId: '30', permissionCode: 'demo:dept:read', permissionName: '部门', permissionAction: PermissionAction.Grant, conditionCount: 0, status: ValidityStatus.Invalid },
    ])

    expect(targets.map(target => target.key)).toEqual(['role:1', 'role:2'])
    expect(targets[0]).toMatchObject({ bindingId: '1', permissionName: 'demo:role:read', action: PermissionAction.Deny })
  })

  it('按组号排好，组内保持原顺序', () => {
    const groups = groupConditions([condition(1, 'a'), condition(0, 'b'), condition(1, 'c')])

    expect(groups.map(group => group.group)).toEqual([0, 1])
    expect(groups[1]?.items.map(item => item.basicId)).toEqual(['a', 'c'])
    expect(nextGroupNumber([])).toBe(0)
    expect(nextGroupNumber([condition(0, 'a'), condition(3, 'b')])).toBe(4)
  })

  it('属性按精确名、旧名、一族解析，大小写不敏感', () => {
    expect(resolveAttribute(catalog, 'Environment.IP').attribute?.name).toBe('environment.ip')
    expect(resolveAttribute(catalog, 'subject.role')).toMatchObject({ isAlias: true, attribute: { name: 'subject.roleCodes' } })
    expect(resolveAttribute(catalog, 'resource.query.status')).toMatchObject({ familySuffix: 'status', attribute: { name: 'resource.query.*' } })
    expect(resolveAttribute(catalog, 'resource.query.').attribute).toBeNull()
    expect(resolveAttribute(catalog, 'resource.status').attribute).toBeNull()
    expect(composeAttributeName(catalog[2]!, ' id ')).toBe('resource.query.id')
    expect(composeAttributeName(catalog[1]!, 'ignored')).toBe('environment.ip')
  })

  it('值类型：布尔、数字只配同种类的属性', () => {
    expect(defaultValueType(ConditionValueKind.Boolean)).toBe(ConfigDataType.Boolean)
    expect(defaultValueType(ConditionValueKind.IpAddress)).toBe(ConfigDataType.String)
    expect(valueTypeOptionsFor(ConditionValueKind.IpAddress)).toEqual([ConfigDataType.String, ConfigDataType.Json, ConfigDataType.Array])
    expect(valueTypeOptionsFor(ConditionValueKind.Number)).toContain(ConfigDataType.Number)
  })

  it('条件值提示随运算符与值种类变化', () => {
    expect(requiresValue(ConditionOperator.IsNull)).toBe(false)
    expect(valueHintKey(ConditionValueKind.String, ConditionOperator.IsNotNull)).toBe('hint_no_value')
    expect(valueHintKey(ConditionValueKind.TimeOfDay, ConditionOperator.Between)).toBe('hint_time_range')
    expect(valueHintKey(ConditionValueKind.IpAddress, ConditionOperator.In)).toBe('hint_ip_list')
    expect(valueHintKey(ConditionValueKind.Number, ConditionOperator.GreaterThan)).toBe('hint_number')
    expect(semanticsKey(PermissionAction.Deny)).toBe('semantics_deny')
    expect(semanticsKey(PermissionAction.Grant)).toBe('semantics_grant')
  })

  it('「名=值」逐行解析，忽略空行与没有等号的行', () => {
    expect(parseKeyValueLines('status=draft\n\nbad line\n id = 7 \r\n=x')).toEqual({ status: 'draft', id: '7' })
    expect(parseKeyValueLines(null)).toEqual({})
  })

  it('盘点导出 CSV：带 BOM，含逗号、引号、换行的单元格加引号', () => {
    const csv = inventoryToCsv(
      [{ conditionId: '1' } as PermissionConditionInventoryItemDto],
      ['条件', '值'],
      () => ['1', 'a,"b"\nc'],
    )

    expect(csv).toBe('﻿条件,值\r\n1,"a,""b""\nc"')
  })
})
