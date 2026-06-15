<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { ApiLogDetailDto, ApiLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { h, ref } from 'vue'
import { createPageRequest, logManagementApi, SignatureType } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogApiPage' })

const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<ApiLogDetailDto | null>(null)

const successOptions = [
  { label: '成功', value: 1 },
  { label: '失败', value: 0 },
]

const methodOptions = [
  { label: 'GET', value: 'GET' },
  { label: 'POST', value: 'POST' },
  { label: 'PUT', value: 'PUT' },
  { label: 'DELETE', value: 'DELETE' },
  { label: 'PATCH', value: 'PATCH' },
]

const signatureTypeOptions = [
  { label: '无签名', value: SignatureType.None },
  { label: 'HMAC-SHA256', value: SignatureType.HmacSha256 },
  { label: 'HMAC-SHA512', value: SignatureType.HmacSha512 },
  { label: 'RSA-SHA256', value: SignatureType.RsaSha256 },
  { label: 'RSA-SHA512', value: SignatureType.RsaSha512 },
  { label: 'SM2', value: SignatureType.Sm2 },
  { label: 'SM3', value: SignatureType.Sm3 },
  { label: 'Ed25519', value: SignatureType.Ed25519 },
  { label: 'MD5', value: SignatureType.Md5 },
]

function formatSize(bytes: number | string): string {
  // requestSize/responseSize 为后端 long，经 LongJsonConverter 序列化为字符串，这里显式转数值
  const size = Number(bytes)
  if (size < 1024) {
    return `${size}B`
  }
  if (size < 1048576) {
    return `${(size / 1024).toFixed(1)}KB`
  }
  return `${(size / 1048576).toFixed(1)}MB`
}

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields: ListFieldSchema[] = [
  // 仅搜索
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索路径/名称/用户', order: 0 },
  // 列（顺序对齐实体 SysOpenApiLog 属性声明）
  { key: 'userId', title: '用户主键', dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: '用户名', dataType: 'string', advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: '会话标识', dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'requestId', title: '请求标识', dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  { key: 'traceId', title: '链路追踪 ID', dataType: 'string', advancedSearch: true, minWidth: 160, order: 14 },
  { key: 'clientId', title: '客户端标识', dataType: 'string', advancedSearch: true, minWidth: 120, order: 15 },
  { key: 'appId', title: '应用标识', dataType: 'string', advancedSearch: true, minWidth: 120, order: 16 },
  {
    key: 'isSignatureValid',
    title: '签名是否有效',
    dataType: 'boolean',
    advancedSearch: true,
    options: [{ label: '有效', value: 1 }, { label: '无效', value: 0 }],
    width: 120,
    order: 17,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as ApiLogListItemDto).isSignatureValid ? 'success' : 'warning' }, () => (row as unknown as ApiLogListItemDto).isSignatureValid ? '有效' : '无效'),
  },
  {
    key: 'signatureType',
    title: '签名类型',
    dataType: 'enum',
    advancedSearch: true,
    options: signatureTypeOptions,
    width: 120,
    order: 18,
    render: row => getOptionLabel(signatureTypeOptions, (row as unknown as ApiLogListItemDto).signatureType),
  },
  { key: 'apiPath', title: 'API 路径', dataType: 'string', advancedSearch: true, minWidth: 240, order: 19 },
  { key: 'apiName', title: 'API 名称', dataType: 'string', minWidth: 120, order: 20 },
  { key: 'method', title: '请求方法', dataType: 'enum', searchable: true, options: methodOptions, searchPlaceholder: '请求方式', width: 100, order: 21 },
  { key: 'controllerName', title: '控制器', dataType: 'string', minWidth: 140, order: 22 },
  { key: 'actionName', title: '动作', dataType: 'string', minWidth: 140, order: 23 },
  { key: 'statusCode', title: '响应状态码', dataType: 'number', advancedSearch: true, width: 100, order: 24 },
  { key: 'requestIp', title: '请求 IP', dataType: 'string', searchable: true, searchPlaceholder: '搜索请求 IP', minWidth: 130, order: 25 },
  { key: 'requestLocation', title: '请求位置', dataType: 'string', minWidth: 160, order: 26 },
  { key: 'browser', title: '浏览器', dataType: 'string', minWidth: 120, order: 27 },
  { key: 'requestTime', title: '请求时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 28 },
  { key: 'responseTime', title: '响应时间', dataType: 'datetime', minWidth: 170, order: 29 },
  { key: 'executionTime', title: '执行耗时', dataType: 'number', sortable: true, width: 110, order: 30, render: row => `${(row as unknown as ApiLogListItemDto).executionTime}ms` },
  { key: 'requestSize', title: '请求大小', dataType: 'number', width: 110, order: 31, render: row => formatSize((row as unknown as ApiLogListItemDto).requestSize) },
  { key: 'responseSize', title: '响应大小', dataType: 'number', width: 110, order: 32, render: row => formatSize((row as unknown as ApiLogListItemDto).responseSize) },
  {
    key: 'isSuccess',
    title: '是否成功',
    dataType: 'boolean',
    searchable: true,
    options: successOptions,
    searchPlaceholder: '结果',
    width: 100,
    order: 33,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as ApiLogListItemDto).isSuccess ? 'success' : 'error' }, () => (row as unknown as ApiLogListItemDto).isSuccess ? '成功' : '失败'),
  },
  { key: 'apiVersion', title: 'API 版本', dataType: 'string', advancedSearch: true, minWidth: 90, order: 34 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 35 },
  // 仅高级搜索（不作为列，范围条件置于高级区末尾）
  { key: 'minExecutionTime', title: '最小耗时(ms)', dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: '最小耗时(ms)', order: 50 },
  { key: 'maxExecutionTime', title: '最大耗时(ms)', dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: '最大耗时(ms)', order: 51 },
  { key: 'requestTimeStart', title: '开始时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '开始时间', order: 52 },
  { key: 'requestTimeEnd', title: '结束时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '结束时间', order: 53 },
]

/** 过滤值辅助 */
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toNum(v: unknown): number | undefined {
  return v == null || v === '' ? undefined : Number(v)
}
function toBool(v: unknown): boolean | undefined {
  return v == null || v === '' ? undefined : v === 1 || v === true
}
function toIso(v: unknown): string | undefined {
  return v == null || v === '' ? undefined : new Date(v as number).toISOString()
}

/** 查询构建（resource.page 与导出快照复用；枚举保持数值以兼容服务端 JSON 反序列化） */
function buildApiQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
    keyword: toStr(f.keyword),
    isSuccess: toBool(f.isSuccess),
    method: toStr(f.method),
    userName: toStr(f.userName),
    userId: toStr(f.userId),
    apiPath: toStr(f.apiPath),
    requestIp: toStr(f.requestIp),
    statusCode: toNum(f.statusCode),
    signatureType: (f.signatureType as SignatureType | undefined) ?? undefined,
    isSignatureValid: toBool(f.isSignatureValid),
    clientId: toStr(f.clientId),
    appId: toStr(f.appId),
    apiVersion: toStr(f.apiVersion),
    traceId: toStr(f.traceId),
    requestId: toStr(f.requestId),
    sessionId: toStr(f.sessionId),
    minExecutionTime: toNum(f.minExecutionTime),
    maxExecutionTime: toNum(f.maxExecutionTime),
    requestTimeStart: toIso(f.requestTimeStart),
    requestTimeEnd: toIso(f.requestTimeEnd),
  }
}

const schema: PageSchema = {
  pageCode: 'log.api',
  exportPermission: 'saas:api-log:export',
  pageName: '开放接口日志',
  rowKey: 'basicId',
  scrollX: 2400,
  fields,
  resource: {
    page: params => logManagementApi.api.page(buildApiQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.api', buildQuery: buildApiQuery },
  },
  actions: [
    { key: 'view', title: '查看详情', scope: 'row', icon: 'lucide:eye' },
  ],
}

const detailFields: LogDetailField[] = [
  { key: 'basicId', label: '日志主键' },
  { key: 'sessionId', label: '会话标识' },
  { key: 'requestId', label: '请求标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'clientId', label: '客户端标识' },
  { key: 'appId', label: '应用标识' },
  { key: 'apiPath', label: 'API 路径', span: 2 },
  { key: 'apiName', label: 'API 名称' },
  { key: 'apiVersion', label: 'API 版本' },
  { key: 'controllerName', label: '控制器' },
  { key: 'actionName', label: '动作' },
  { key: 'method', label: '请求方法' },
  { key: 'statusCode', label: '响应状态码' },
  { key: 'isSuccess', falseText: '失败', label: '是否成功', trueText: '成功', type: 'boolean' },
  { key: 'isSignatureValid', falseText: '无效', label: '签名是否有效', trueText: '有效', type: 'boolean' },
  { key: 'signatureType', label: '签名类型', options: signatureTypeOptions, type: 'enum' },
  { key: 'executionTime', label: '执行耗时', type: 'duration' },
  { key: 'requestSize', label: '请求大小', type: 'bytes' },
  { key: 'responseSize', label: '响应大小', type: 'bytes' },
  { key: 'requestIp', label: '请求 IP' },
  { key: 'requestLocation', label: '请求位置' },
  { key: 'browser', label: '浏览器' },
  { key: 'referer', label: '来源地址', span: 2 },
  { key: 'requestTime', label: '请求时间', type: 'date' },
  { key: 'responseTime', label: '响应时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'remark', label: '备注', span: 2 },
  { key: 'userAgent', label: 'User-Agent', type: 'code' },
  { key: 'requestParams', label: '请求参数', type: 'code' },
  { key: 'requestBody', label: '请求体', type: 'code' },
  { key: 'responseBody', label: '响应结果', type: 'code' },
  { key: 'requestHeaders', label: '请求头', type: 'code' },
  { key: 'responseHeaders', label: '响应头', type: 'code' },
  { key: 'errorMessage', label: '错误消息', type: 'code' },
  { key: 'exceptionStackTrace', label: '异常堆栈', type: 'code' },
  { key: 'extendData', label: '扩展数据', type: 'code' },
]

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as ApiLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
}

async function handleDetail(row: ApiLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.api.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载 API 日志详情失败')
  }
  finally {
    detailLoading.value = false
  }
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <LogDetailDrawer
      v-model:show="detailVisible"
      :fields="detailFields"
      :loading="detailLoading"
      :record="detailData"
      title="API 日志详情"
    />
  </SchemaPage>
</template>
