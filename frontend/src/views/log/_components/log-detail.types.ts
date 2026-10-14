import type { Tone } from '@xihan-ui/core'

export type LogDetailFieldType = 'boolean' | 'bytes' | 'code' | 'date' | 'duration' | 'enum' | 'text'

/**
 * 详情分组（抽屉按此顺序逐组排版，组内无边框、标签在上）：
 * subject 业务主体、request 请求、client 客户端、runtime 运行环境、handling 处理、trace 追踪标识。
 * 代码块字段（type: 'code'）不分组，统一排在最后。
 */
export type LogDetailGroup = 'subject' | 'request' | 'client' | 'runtime' | 'handling' | 'trace'

export interface LogDetailOption {
  label: string
  value: number | string
}

export interface LogDetailField {
  /** 后端枚举类型名：标签取后端枚举元数据，前端不另抄一份，后端加了成员也照样显示；与 options 二选一 */
  enumName?: string
  falseText?: string
  /** 所在分组；未写时归入业务主体 */
  group?: LogDetailGroup
  key: string
  label: string
  /** 等宽显示并带复制按钮（主键、追踪标识、地址这类要拿去别处用的值） */
  mono?: boolean
  options?: LogDetailOption[]
  /** 占满整行（长文本、地址） */
  span?: 'full'
  /** 取值的语气（结果、等级这类字段在抬头里显示成带色标签） */
  tone?: (value: unknown) => Tone
  /** 链路追踪维度：值可点击，跳到「日志链路追踪」按该维度查（与后端 TraceDimension 一致） */
  trace?: string
  trueText?: string
  type?: LogDetailFieldType
}

/**
 * 详情抬头：打开即看懂「谁、什么时候、做了什么、成没成功」。
 * 抬头用到的字段不再在分组里重复出现。
 */
export interface LogDetailHeader {
  /** 标题段：按顺序取非空值，以「 · 」连接 */
  title: string[]
  /** 结果字段：显示成带语气的标签（语气取字段的 tone） */
  status?: string
  /** 补充标签：耗时、状态码这类短值 */
  badges?: string[]
  /** 副信息：用户、时间、IP 等，按顺序取非空值，以「 · 」连接 */
  meta: string[]
  /** 记录的发生时间字段：跳链路追踪时把时间窗口居中到这一刻 */
  timeKey?: string
}

/** 一种日志的详情定义：抬头 + 字段（字段按 group 分组，代码块排在最后） */
export interface LogDetailSchema {
  header: LogDetailHeader
  fields: LogDetailField[]
}
