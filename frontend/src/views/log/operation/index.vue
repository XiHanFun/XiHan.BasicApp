<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { OperationLogDetailDto, OperationLogListItemDto, PageResult } from '@/api'
import { NTag, useMessage } from 'naive-ui'
import { h, ref } from 'vue'
import { createPageRequest, EnableStatus, logManagementApi, OperationType } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogOperationPage' })

const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<OperationLogDetailDto | null>(null)

const statusOptions = [
  { label: '成功', value: EnableStatus.Enabled },
  { label: '失败', value: EnableStatus.Disabled },
]

const operationTypeOptions = [
  { label: '登录', value: OperationType.Login },
  { label: '登出', value: OperationType.Logout },
  { label: '查询', value: OperationType.Query },
  { label: '新增', value: OperationType.Create },
  { label: '修改', value: OperationType.Update },
  { label: '删除', value: OperationType.Delete },
  { label: '导入', value: OperationType.Import },
  { label: '导出', value: OperationType.Export },
  { label: '其他', value: OperationType.Other },
]

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索标题/模块/用户', order: 0 },
  {
    key: 'operationType',
    title: '操作类型',
    dataType: 'enum',
    searchable: true,
    options: operationTypeOptions,
    searchPlaceholder: '操作类型',
    width: 90,
    order: 10,
    render: row => getOptionLabel(operationTypeOptions, (row as unknown as OperationLogListItemDto).operationType),
  },
  {
    key: 'status',
    title: '操作状态',
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: '状态',
    width: 90,
    order: 11,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as OperationLogListItemDto).status === EnableStatus.Enabled ? 'success' : 'error' }, () => (row as unknown as OperationLogListItemDto).status === EnableStatus.Enabled ? '成功' : '失败'),
  },
  // 高级搜索 + 列
  { key: 'userName', title: '用户名', dataType: 'string', advancedSearch: true, minWidth: 100, order: 12 },
  { key: 'userId', title: '用户主键', dataType: 'string', advancedSearch: true, minWidth: 90, order: 13 },
  { key: 'module', title: '操作模块', dataType: 'string', advancedSearch: true, minWidth: 120, order: 14 },
  { key: 'function', title: '操作功能', dataType: 'string', advancedSearch: true, minWidth: 120, order: 15 },
  { key: 'title', title: '操作标题', dataType: 'string', advancedSearch: true, minWidth: 160, order: 16 },
  { key: 'method', title: '请求方法', dataType: 'string', advancedSearch: true, width: 90, order: 17 },
  { key: 'traceId', title: '链路追踪 ID', dataType: 'string', advancedSearch: true, minWidth: 160, order: 18 },
  { key: 'sessionId', title: '会话标识', dataType: 'string', advancedSearch: true, minWidth: 160, order: 19 },
  // 仅高级搜索
  { key: 'minExecutionTime', title: '最小耗时(ms)', dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: '最小耗时(ms)', order: 20 },
  { key: 'maxExecutionTime', title: '最大耗时(ms)', dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: '最大耗时(ms)', order: 21 },
  { key: 'operationTimeStart', title: '开始时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '开始时间', order: 22 },
  { key: 'operationTimeEnd', title: '结束时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '结束时间', order: 23 },
  // 仅列
  { key: 'description', title: '操作描述', dataType: 'string', minWidth: 220, order: 30 },
  { key: 'requestUrl', title: '请求地址', dataType: 'string', minWidth: 240, order: 31 },
  { key: 'operationIp', title: '操作 IP', dataType: 'string', minWidth: 130, order: 32 },
  { key: 'operationLocation', title: '操作位置', dataType: 'string', minWidth: 160, order: 33 },
  { key: 'browser', title: '浏览器', dataType: 'string', minWidth: 120, order: 34 },
  { key: 'os', title: '操作系统', dataType: 'string', minWidth: 120, order: 35 },
  { key: 'executionTime', title: '执行耗时', dataType: 'number', sortable: true, width: 110, order: 36, render: row => `${(row as unknown as OperationLogListItemDto).executionTime}ms` },
  { key: 'operationTime', title: '操作时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 37 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 38 },
]

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toNum(v: unknown): number | undefined {
  return v == null || v === '' ? undefined : Number(v)
}
function toIso(v: unknown): string | undefined {
  return v == null || v === '' ? undefined : new Date(v as number).toISOString()
}

const schema: PageSchema = {
  pageCode: 'log.operation',
  pageName: '操作日志',
  rowKey: 'basicId',
  scrollX: 2200,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return logManagementApi.operation.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        operationType: (f.operationType as OperationType | undefined) ?? undefined,
        status: (f.status as EnableStatus | undefined) ?? undefined,
        userName: toStr(f.userName),
        userId: toStr(f.userId),
        module: toStr(f.module),
        function: toStr(f.function),
        title: toStr(f.title),
        method: toStr(f.method),
        traceId: toStr(f.traceId),
        sessionId: toStr(f.sessionId),
        minExecutionTime: toNum(f.minExecutionTime),
        maxExecutionTime: toNum(f.maxExecutionTime),
        operationTimeStart: toIso(f.operationTimeStart),
        operationTimeEnd: toIso(f.operationTimeEnd),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
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
  { key: 'operationType', label: '操作类型', options: operationTypeOptions, type: 'enum' },
  { key: 'status', label: '操作状态', options: statusOptions, type: 'enum' },
  { key: 'module', label: '操作模块' },
  { key: 'function', label: '操作功能' },
  { key: 'title', label: '操作标题', span: 2 },
  { key: 'description', label: '操作描述', span: 2 },
  { key: 'method', label: '请求方法' },
  { key: 'requestUrl', label: '请求地址', span: 2 },
  { key: 'executionTime', label: '执行耗时', type: 'duration' },
  { key: 'operationIp', label: '操作 IP' },
  { key: 'operationLocation', label: '操作位置' },
  { key: 'browser', label: '浏览器' },
  { key: 'os', label: '操作系统' },
  { key: 'operationTime', label: '操作时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'userAgent', label: 'User-Agent', type: 'code' },
  { key: 'errorMessage', label: '错误消息', type: 'code' },
]

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as OperationLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
}

async function handleDetail(row: OperationLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.operation.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载操作日志详情失败')
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
      title="操作日志详情"
    />
  </SchemaPage>
</template>
