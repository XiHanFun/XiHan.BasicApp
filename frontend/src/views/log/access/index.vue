<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { AccessLogDetailDto, AccessLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { h, ref } from 'vue'
import { AccessResult, createPageRequest, logManagementApi } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogAccessPage' })

const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<AccessLogDetailDto | null>(null)

const accessResultOptions = [
  { label: '成功', value: AccessResult.Success },
  { label: '失败', value: AccessResult.Failed },
  { label: '权限不足', value: AccessResult.Forbidden },
  { label: '未授权', value: AccessResult.Unauthorized },
  { label: '资源不存在', value: AccessResult.NotFound },
  { label: '服务器错误', value: AccessResult.ServerError },
]

const methodOptions = [
  { label: 'GET', value: 'GET' },
  { label: 'POST', value: 'POST' },
  { label: 'PUT', value: 'PUT' },
  { label: 'DELETE', value: 'DELETE' },
  { label: 'PATCH', value: 'PATCH' },
]

function accessResultType(result: AccessResult) {
  switch (result) {
    case AccessResult.Success: return 'success'
    case AccessResult.Failed: return 'error'
    case AccessResult.Forbidden:
    case AccessResult.Unauthorized: return 'warning'
    default: return 'default'
  }
}

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields: ListFieldSchema[] = [
  // 仅搜索（不作为列）
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索路径/用户', width: 220, order: 0 },
  // 列（顺序对齐实体 SysAccessLog 属性声明）
  { key: 'userId', title: '用户主键', dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: '用户名', dataType: 'string', advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: '会话标识', dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'traceId', title: '链路追踪 ID', dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  { key: 'resourcePath', title: '资源路径', dataType: 'string', advancedSearch: true, minWidth: 240, order: 14 },
  { key: 'resourceName', title: '资源名称', dataType: 'string', minWidth: 120, order: 15 },
  { key: 'resourceType', title: '资源类型', dataType: 'string', advancedSearch: true, minWidth: 100, order: 16 },
  {
    key: 'method',
    title: '请求方法',
    dataType: 'enum',
    searchable: true,
    options: methodOptions,
    searchPlaceholder: '请求方法',
    width: 100,
    order: 17,
    // 直接展示原始方法字符串：OPTIONS/HEAD 等不在搜索选项内，避免按枚举映射后显示为空
    render: row => (row as unknown as AccessLogListItemDto).method || '-',
  },
  {
    key: 'accessResult',
    title: '访问结果',
    dataType: 'enum',
    searchable: true,
    options: accessResultOptions,
    searchPlaceholder: '访问结果',
    width: 110,
    order: 18,
    render: (row) => {
      const r = row as unknown as AccessLogListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: accessResultType(r.accessResult) }, () => getOptionLabel(accessResultOptions, r.accessResult))
    },
  },
  { key: 'statusCode', title: '响应状态码', dataType: 'number', advancedSearch: true, width: 100, order: 19 },
  { key: 'accessIp', title: '访问 IP', dataType: 'string', searchable: true, searchPlaceholder: '搜索访问 IP', minWidth: 130, order: 20 },
  { key: 'accessLocation', title: '访问位置', dataType: 'string', minWidth: 160, order: 21 },
  { key: 'browser', title: '浏览器', dataType: 'string', minWidth: 120, order: 22 },
  { key: 'os', title: '操作系统', dataType: 'string', minWidth: 120, order: 23 },
  { key: 'device', title: '设备', dataType: 'string', minWidth: 120, order: 24 },
  { key: 'executionTime', title: '执行耗时', dataType: 'number', sortable: true, width: 110, order: 25, render: row => `${(row as unknown as AccessLogListItemDto).executionTime}ms` },
  { key: 'accessTime', title: '访问时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 26 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 27 },
  // 仅高级搜索（不作为列，范围条件置于高级区末尾）
  { key: 'minExecutionTime', title: '最小耗时(ms)', dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: '最小耗时(ms)', order: 40 },
  { key: 'maxExecutionTime', title: '最大耗时(ms)', dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: '最大耗时(ms)', order: 41 },
  { key: 'accessTimeStart', title: '开始时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '开始时间', order: 42 },
  { key: 'accessTimeEnd', title: '结束时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '结束时间', order: 43 },
]

/** 过滤值辅助：trim 字符串 / 转数字 / 时间戳转 ISO */
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toNum(v: unknown): number | undefined {
  return v == null || v === '' ? undefined : Number(v)
}
function toIso(v: unknown): string | undefined {
  return v == null || v === '' ? undefined : new Date(v as number).toISOString()
}

/** 查询构建（resource.page 与导出快照复用；枚举保持数值以兼容服务端 JSON 反序列化） */
function buildAccessQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
    keyword: toStr(f.keyword),
    accessResult: (f.accessResult as AccessResult | undefined) ?? undefined,
    method: toStr(f.method),
    accessIp: toStr(f.accessIp),
    userName: toStr(f.userName),
    userId: toStr(f.userId),
    resourcePath: toStr(f.resourcePath),
    resourceType: toStr(f.resourceType),
    sessionId: toStr(f.sessionId),
    traceId: toStr(f.traceId),
    statusCode: toNum(f.statusCode),
    minExecutionTime: toNum(f.minExecutionTime),
    maxExecutionTime: toNum(f.maxExecutionTime),
    accessTimeStart: toIso(f.accessTimeStart),
    accessTimeEnd: toIso(f.accessTimeEnd),
  }
}

const schema: PageSchema = {
  pageCode: 'log.access',
  exportPermission: 'saas:access-log:export',
  pageName: '访问日志',
  rowKey: 'basicId',
  scrollX: 2200,
  fields,
  resource: {
    page: params => logManagementApi.access.page(buildAccessQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.access', buildQuery: buildAccessQuery },
  },
  actions: [
    { key: 'view', title: '查看详情', scope: 'row', icon: 'lucide:eye' },
  ],
}

const detailFields: LogDetailField[] = [
  { key: 'basicId', label: '日志主键' },
  { key: 'sessionId', label: '会话标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'resourcePath', label: '资源路径', span: 2 },
  { key: 'resourceName', label: '资源名称' },
  { key: 'resourceType', label: '资源类型' },
  { key: 'method', label: '请求方法' },
  { key: 'statusCode', label: '响应状态码' },
  { key: 'accessResult', label: '访问结果', options: accessResultOptions, type: 'enum' },
  { key: 'executionTime', label: '执行耗时', type: 'duration' },
  { key: 'accessIp', label: '访问 IP' },
  { key: 'accessLocation', label: '访问位置' },
  { key: 'browser', label: '浏览器' },
  { key: 'os', label: '操作系统' },
  { key: 'device', label: '设备' },
  { key: 'referer', label: '来源地址', span: 2 },
  { key: 'accessTime', label: '访问时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'remark', label: '备注', span: 2 },
  { key: 'userAgent', label: 'User-Agent', type: 'code' },
  { key: 'errorMessage', label: '错误消息', type: 'code' },
  { key: 'extendData', label: '扩展数据', type: 'code' },
]

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as AccessLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
}

async function handleDetail(row: AccessLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.access.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载访问日志详情失败')
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
      title="访问日志详情"
    />
  </SchemaPage>
</template>
