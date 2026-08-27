/**
 * 业务下拉选项常量（src/constants）单元测试。
 *
 * 职责边界：这批常量是「前端下拉」与「后端枚举」之间的唯一映射表，写错一个 value
 * 就是筛选筛不出、表单存不进去。这里做三件事：选项自身结构合法、value 不重复、
 * 与后端枚举成员严格对齐（既不能多出后端没有的值，也不能漏掉后端有的值）。
 */
import type { AxiosRequestConfig } from '~/request'
import { describe, expect, it, vi } from 'vitest'
import {
  AuditResult,
  AuditStatus,
  ConditionOperator,
  ConfigDataType,
  ConfigType,
  ConstraintType,
  DataPermissionScope,
  DelegationStatus,
  DepartmentType,
  DeviceType,
  EmailStatus,
  EmailType,
  EnableStatus,
  FieldMaskStrategy,
  FieldSecurityTargetType,
  FileStatus,
  FileType,
  HttpMethodType,
  MenuType,
  NotificationStatus,
  OAuthAppType,
  OperationCategory,
  OperationTypeCode,
  PermissionAction,
  PermissionChangeType,
  PermissionRequestStatus,
  PermissionType,
  ResourceAccessLevel,
  ResourceType,
  RoleType,
  RunTaskStatus,
  SmsStatus,
  SmsType,
  StatisticsPeriod,
  TenantConfigStatus,
  TenantDatabaseType,
  TenantIsolationMode,
  TenantMemberInviteStatus,
  TenantStatus,
  TriggerType,
  TwoFactorMethod,
  UserGender,
  ValidityStatus,
  ViolationAction,
} from '@/api'
import * as constants from './index'

// 常量模块通过 `@/api` 桶文件间接引入 request 客户端，测试里不需要它做任何事
vi.mock('@/api/request', () => ({
  requestClient: {
    get: (_url: string, _config?: AxiosRequestConfig) => Promise.resolve(null),
    post: () => Promise.resolve(null),
    put: () => Promise.resolve(null),
    delete: () => Promise.resolve(undefined),
  },
}))

interface Option {
  label: string
  value: unknown
}

type EnumLike = Record<string, number | string>

/** 取枚举的「线上值」集合：数字枚举要滤掉反向映射键 */
function enumValues(target: EnumLike): Set<number | string> {
  const values = Object.values(target)
  const numeric = values.filter(value => typeof value === 'number')
  return new Set(numeric.length > 0 ? numeric : values)
}

function isOptionArray(value: unknown): value is Option[] {
  return Array.isArray(value)
    && value.every(item => typeof item === 'object' && item !== null && 'label' in item && 'value' in item)
}

/** 自动发现的全部下拉选项常量 */
const optionExports = (Object.entries(constants) as [string, unknown][])
  .filter((entry): entry is [string, Option[]] => entry[0].endsWith('_OPTIONS') && isOptionArray(entry[1]))

/** 选项常量与后端枚举的对齐表：两侧成员必须一一对应 */
const alignments: [string, Option[], EnumLike][] = [
  ['GENDER_OPTIONS', constants.GENDER_OPTIONS, UserGender],
  ['STATUS_OPTIONS', constants.STATUS_OPTIONS, EnableStatus],
  ['VALIDITY_STATUS_OPTIONS', constants.VALIDITY_STATUS_OPTIONS, ValidityStatus],
  ['DEPARTMENT_TYPE_OPTIONS', constants.DEPARTMENT_TYPE_OPTIONS, DepartmentType],
  ['ROLE_TYPE_OPTIONS', constants.ROLE_TYPE_OPTIONS, RoleType],
  ['DATA_SCOPE_OPTIONS', constants.DATA_SCOPE_OPTIONS, DataPermissionScope],
  ['PERMISSION_ACTION_OPTIONS', constants.PERMISSION_ACTION_OPTIONS, PermissionAction],
  ['TENANT_ISOLATION_MODE_OPTIONS', constants.TENANT_ISOLATION_MODE_OPTIONS, TenantIsolationMode],
  ['TENANT_DATABASE_TYPE_OPTIONS', constants.TENANT_DATABASE_TYPE_OPTIONS, TenantDatabaseType],
  ['TENANT_STATUS_OPTIONS', constants.TENANT_STATUS_OPTIONS, TenantStatus],
  ['TENANT_CONFIG_STATUS_OPTIONS', constants.TENANT_CONFIG_STATUS_OPTIONS, TenantConfigStatus],
  ['MEMBER_INVITE_STATUS_OPTIONS', constants.MEMBER_INVITE_STATUS_OPTIONS, TenantMemberInviteStatus],
  ['FILE_TYPE_OPTIONS', constants.FILE_TYPE_OPTIONS, FileType],
  ['FILE_STATUS_OPTIONS', constants.FILE_STATUS_OPTIONS, FileStatus],
  ['EMAIL_TYPE_OPTIONS', constants.EMAIL_TYPE_OPTIONS, EmailType],
  ['EMAIL_STATUS_OPTIONS', constants.EMAIL_STATUS_OPTIONS, EmailStatus],
  ['SMS_TYPE_OPTIONS', constants.SMS_TYPE_OPTIONS, SmsType],
  ['SMS_STATUS_OPTIONS', constants.SMS_STATUS_OPTIONS, SmsStatus],
  ['TRIGGER_TYPE_OPTIONS', constants.TRIGGER_TYPE_OPTIONS, TriggerType],
  ['RUN_TASK_STATUS_OPTIONS', constants.RUN_TASK_STATUS_OPTIONS, RunTaskStatus],
  ['OAUTH_APP_TYPE_OPTIONS', constants.OAUTH_APP_TYPE_OPTIONS, OAuthAppType],
  ['REVIEW_STATUS_OPTIONS', constants.REVIEW_STATUS_OPTIONS, AuditStatus],
  ['REVIEW_RESULT_OPTIONS', constants.REVIEW_RESULT_OPTIONS, AuditResult],
  ['DEVICE_TYPE_OPTIONS', constants.DEVICE_TYPE_OPTIONS, DeviceType],
  ['TWO_FACTOR_METHOD_OPTIONS', constants.TWO_FACTOR_METHOD_OPTIONS, TwoFactorMethod],
  ['STATISTICS_PERIOD_OPTIONS', constants.STATISTICS_PERIOD_OPTIONS, StatisticsPeriod],
  ['CONFIG_TYPE_OPTIONS', constants.CONFIG_TYPE_OPTIONS, ConfigType],
  ['CONFIG_DATA_TYPE_OPTIONS', constants.CONFIG_DATA_TYPE_OPTIONS, ConfigDataType],
  ['PERMISSION_TYPE_OPTIONS', constants.PERMISSION_TYPE_OPTIONS, PermissionType],
  ['RESOURCE_TYPE_OPTIONS', constants.RESOURCE_TYPE_OPTIONS, ResourceType],
  ['RESOURCE_ACCESS_LEVEL_OPTIONS', constants.RESOURCE_ACCESS_LEVEL_OPTIONS, ResourceAccessLevel],
  ['HTTP_METHOD_OPTIONS', constants.HTTP_METHOD_OPTIONS, HttpMethodType],
  ['OPERATION_CATEGORY_OPTIONS', constants.OPERATION_CATEGORY_OPTIONS, OperationCategory],
  ['OPERATION_TYPE_OPTIONS', constants.OPERATION_TYPE_OPTIONS, OperationTypeCode],
  ['CONDITION_OPERATOR_OPTIONS', constants.CONDITION_OPERATOR_OPTIONS, ConditionOperator],
  ['DELEGATION_STATUS_OPTIONS', constants.DELEGATION_STATUS_OPTIONS, DelegationStatus],
  ['PERMISSION_REQUEST_STATUS_OPTIONS', constants.PERMISSION_REQUEST_STATUS_OPTIONS, PermissionRequestStatus],
  ['FIELD_MASK_STRATEGY_OPTIONS', constants.FIELD_MASK_STRATEGY_OPTIONS, FieldMaskStrategy],
  ['FIELD_SECURITY_TARGET_TYPE_OPTIONS', constants.FIELD_SECURITY_TARGET_TYPE_OPTIONS, FieldSecurityTargetType],
  ['PERMISSION_CHANGE_TYPE_OPTIONS', constants.PERMISSION_CHANGE_TYPE_OPTIONS, PermissionChangeType],
  ['CONSTRAINT_TYPE_OPTIONS', constants.CONSTRAINT_TYPE_OPTIONS, ConstraintType],
  ['VIOLATION_ACTION_OPTIONS', constants.VIOLATION_ACTION_OPTIONS, ViolationAction],
  ['NOTIFICATION_STATUS_OPTIONS', constants.NOTIFICATION_STATUS_OPTIONS, NotificationStatus],
]

describe('下拉选项常量的自身结构', () => {
  it('能从 @/constants 入口枚举到全部选项常量', () => {
    expect(optionExports.length).toBeGreaterThanOrEqual(alignments.length)
    expect(optionExports.map(([name]) => name)).toContain('MEMBER_TYPE_OPTIONS')
  })

  it('每个选项都有非空中文/英文标签，不允许空串或纯空白占位', () => {
    const bad: string[] = []
    for (const [name, options] of optionExports) {
      options.forEach((option, index) => {
        if (typeof option.label !== 'string' || option.label.trim().length === 0) {
          bad.push(`${name}[${index}]`)
        }
      })
    }

    expect(bad).toEqual([])
  })

  it('每个选项的 value 都是字符串或数字，且非空串', () => {
    const bad: string[] = []
    for (const [name, options] of optionExports) {
      options.forEach((option, index) => {
        const ok = (typeof option.value === 'string' && option.value.length > 0)
          || typeof option.value === 'number'
        if (!ok) {
          bad.push(`${name}[${index}] → ${String(option.value)}`)
        }
      })
    }

    expect(bad).toEqual([])
  })

  it('同一组选项内 value 不重复——重复会让下拉选中态串到另一项', () => {
    const bad: string[] = []
    for (const [name, options] of optionExports) {
      if (new Set(options.map(option => option.value)).size !== options.length) {
        bad.push(name)
      }
    }

    expect(bad).toEqual([])
  })

  it('同一组选项内 label 不重复——重复的文案用户分不清选了哪个', () => {
    const bad: string[] = []
    for (const [name, options] of optionExports) {
      if (new Set(options.map(option => option.label)).size !== options.length) {
        bad.push(name)
      }
    }

    expect(bad).toEqual([])
  })

  it('选项数组非空——空下拉等于功能失效', () => {
    const empty = optionExports.filter(([, options]) => options.length === 0).map(([name]) => name)

    expect(empty).toEqual([])
  })
})

describe('选项常量与后端枚举的对齐', () => {
  it('选项里不出现后端枚举没有的值——多出来的值提交后会被后端拒绝', () => {
    const bad: string[] = []
    for (const [name, options, target] of alignments) {
      const allowed = enumValues(target)
      for (const option of options) {
        if (!allowed.has(option.value as number | string)) {
          bad.push(`${name}: ${String(option.value)}`)
        }
      }
    }

    expect(bad).toEqual([])
  })

  it('后端枚举成员被下拉全量覆盖——漏一个成员就有一类数据筛不出来', () => {
    const bad: string[] = []
    for (const [name, options, target] of alignments) {
      const picked = new Set(options.map(option => option.value))
      for (const value of enumValues(target)) {
        if (!picked.has(value)) {
          bad.push(`${name}: 缺 ${String(value)}`)
        }
      }
    }

    expect(bad).toEqual([])
  })

  it('对齐表覆盖到全部业务枚举下拉，新增下拉忘了登记时这里先失败', () => {
    const aligned = new Set(alignments.map(([name]) => name))
    const unaligned = optionExports
      .map(([name]) => name)
      .filter(name => !aligned.has(name))

    // 未登记的只允许是纯前端建议值（不对应后端枚举）
    expect(unaligned.sort()).toEqual([
      'MEMBER_TYPE_OPTIONS',
      'OPENAPI_CONTENT_SIGN_ALGORITHM_OPTIONS',
      'OPENAPI_ENCRYPT_ALGORITHM_OPTIONS',
      'OPENAPI_SIGNATURE_ALGORITHM_OPTIONS',
    ])
  })
})

describe('非枚举下拉的取值形状', () => {
  it('开放接口的三组算法选项 label 与 value 一一相同，直接作为字面量下发', () => {
    const groups = [
      constants.OPENAPI_SIGNATURE_ALGORITHM_OPTIONS,
      constants.OPENAPI_CONTENT_SIGN_ALGORITHM_OPTIONS,
      constants.OPENAPI_ENCRYPT_ALGORITHM_OPTIONS,
    ]

    for (const group of groups) {
      expect(group.filter(option => option.label !== option.value)).toEqual([])
    }
  })

  it('三组算法选项之间取值互不重叠，避免把签名算法填进加密算法字段', () => {
    const signature = new Set(constants.OPENAPI_SIGNATURE_ALGORITHM_OPTIONS.map(option => option.value))
    const encrypt = constants.OPENAPI_ENCRYPT_ALGORITHM_OPTIONS.map(option => option.value)

    expect(encrypt.filter(value => signature.has(value))).toEqual([])
  })
})

describe('菜单类型常量', () => {
  it('三个别名逐一映射到后端 MenuType 成员', () => {
    expect(constants.MENU_TYPE).toEqual({
      DIR: MenuType.Directory,
      MENU: MenuType.Menu,
      BUTTON: MenuType.Button,
    })
  })

  it('别名覆盖 MenuType 的全部成员，且取值不重复', () => {
    const mapped = Object.values(constants.MENU_TYPE)

    expect(new Set(mapped)).toEqual(enumValues(MenuType))
    expect(new Set(mapped).size).toBe(mapped.length)
  })
})
