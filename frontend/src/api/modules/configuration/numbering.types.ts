/**
 * 业务编号模块类型契约。
 * 这些枚举值和 DTO 字段与后端 Numbering Dynamic API 保持一一对应。
 */
import type { ApiId, BasicDto, PageRequest } from '../../types'
import type { EnableStatus } from '../shared/types'

/** 规则解析作用域。 */
export enum NumberingScope {
  /** 租户优先，找不到时回退到开放的全局规则；平台直接使用全局规则。 */
  Auto = 'Auto',
  /** 仅租户私有规则。 */
  Tenant = 'Tenant',
  /** 仅平台全局规则。 */
  Global = 'Global',
}

/** 流水重置周期。 */
export enum NumberingResetCycle {
  Never = 'Never',
  Daily = 'Daily',
  Monthly = 'Monthly',
  Yearly = 'Yearly',
}

/** 日期段格式白名单。 */
export enum NumberingDateFormat {
  None = 'None',
  Yyyy = 'Yyyy',
  YyyyMM = 'YyyyMM',
  YyyyMMdd = 'YyyyMMdd',
  YyMM = 'YyMM',
  YyMMdd = 'YyMMdd',
  MMdd = 'MMdd',
}

/** 规则列表与详情的公共字段。 */
export interface NumberingRuleListItemDto extends BasicDto {
  ruleCode: string
  ruleName: string
  prefix?: string | null
  separator: string
  dateFormat: NumberingDateFormat
  serialLength: number
  resetCycle: NumberingResetCycle
  timeZoneId: string
  /** 十进制字符串，避免 18 位流水超过 JavaScript 安全整数范围。 */
  currentValue: string
  currentPeriod?: string | null
  hasAllocated: boolean
  allowTenantUse: boolean
  isGlobal: boolean
  status: EnableStatus
  sort: number
  remark?: string | null
}

/** 规则详情。 */
export interface NumberingRuleDetailDto extends NumberingRuleListItemDto {
  createdTime: string
  modifiedTime?: string | null
}

/** 创建规则参数；不允许传入任意租户 ID。 */
export interface NumberingRuleCreateDto {
  scope: NumberingScope
  ruleCode: string
  ruleName: string
  prefix?: string | null
  separator: string
  dateFormat: NumberingDateFormat
  serialLength: number
  resetCycle: NumberingResetCycle
  timeZoneId: string
  allowTenantUse: boolean
  status: EnableStatus
  sort: number
  remark?: string | null
}

/** 更新规则参数；规则编码与归属不可修改。 */
export interface NumberingRuleUpdateDto extends Omit<NumberingRuleCreateDto, 'ruleCode' | 'status'> {
  basicId: ApiId
}

/** 更新规则状态参数。 */
export interface NumberingRuleStatusUpdateDto {
  basicId: ApiId
  scope: NumberingScope
  status: EnableStatus
  remark?: string | null
}

/** 安全重置参数。 */
export interface NumberingRuleResetDto {
  basicId: ApiId
  scope: NumberingScope
  /** 十进制字符串，避免 18 位流水精度丢失。 */
  nextValue: string
  reason: string
  confirmRuleCode?: string | null
}

/** 规则分页参数。 */
export interface NumberingRulePageQueryDto extends PageRequest {
  scope: NumberingScope
  keyword?: string | null
  status?: EnableStatus | null
}

/** 发号记录分页参数。 */
export interface NumberingAllocationPageQueryDto extends PageRequest {
  ruleId: ApiId
  scope: NumberingScope
  keyword?: string | null
}

/** 永久发号记录列表项。 */
export interface NumberingAllocationListItemDto extends BasicDto {
  ruleId: ApiId
  ruleCode: string
  requestTenantId: ApiId
  idempotencyKey: string
  count: number
  startValue: string
  endValue: string
  periodKey: string
  firstNumber: string
  lastNumber: string
  generatedAtUtc: string
  businessType?: string | null
  businessId?: string | null
}

/** 后端运行环境确认可解析的规则时区选项。 */
export interface NumberingTimeZoneOptionDto {
  id: string
  displayName: string
  baseUtcOffsetMinutes: number
  supportsDaylightSavingTime: boolean
}

/** 不消耗流水的格式预览参数。 */
export interface NumberingPreviewDto {
  prefix?: string | null
  separator: string
  dateFormat: NumberingDateFormat
  serialLength: number
  resetCycle: NumberingResetCycle
  timeZoneId: string
  sampleValue: string
}

/** 格式预览结果。 */
export interface NumberingPreviewResultDto {
  number: string
  ruleLocalTime: string
  periodKey: string
}

/** 单次连续批量预览允许返回的最大编号数量，与后端契约保持一致。 */
export const NUMBERING_BATCH_PREVIEW_MAX_COUNT = 50 as const

/** 不消耗流水的连续批量格式预览参数。 */
export interface NumberingBatchPreviewDto extends NumberingPreviewDto {
  /** 从 sampleValue 开始连续预览的数量，范围为 1～50。 */
  count: number
}

/** 连续批量格式预览结果。 */
export interface NumberingBatchPreviewResultDto {
  /** 十进制字符串形式的预览起始流水，避免 18 位整数精度丢失。 */
  startValue: string
  /** 十进制字符串形式的预览结束流水，避免 18 位整数精度丢失。 */
  endValue: string
  /** 按流水升序返回的连续编号，最多 50 个。 */
  numbers: string[]
  /** 执行预览时的规则时区本地时间。 */
  ruleLocalTime: string
  /** 执行预览时根据重置周期计算的周期键。 */
  periodKey: string
}

/** 单号生成参数；幂等键必填。 */
export interface NumberGenerateDto {
  ruleCode: string
  scope: NumberingScope
  idempotencyKey: string
  businessType?: string | null
  businessId?: string | null
}

/** 批量生成参数。 */
export interface NumberBatchGenerateDto extends NumberGenerateDto {
  count: number
}

/** 编号生成结果。 */
export interface NumberGenerationResultDto {
  ruleId: ApiId
  ruleCode: string
  resolvedScope: NumberingScope
  idempotencyKey: string
  periodKey: string
  startValue: string
  endValue: string
  numbers: string[]
  generatedAtUtc: string
  isIdempotentReplay: boolean
}
