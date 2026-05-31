<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { ExceptionLogDetailDto, ExceptionLogListItemDto, PageResult } from '@/api'
import { NTag, useMessage } from 'naive-ui'
import { h, ref } from 'vue'
import { createPageRequest, DeviceType, logManagementApi } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogExceptionPage' })

const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<ExceptionLogDetailDto | null>(null)

const handledOptions = [
  { label: '已处理', value: 1 },
  { label: '未处理', value: 0 },
]

const severityOptions = [
  { label: '低', value: 1 },
  { label: '中', value: 2 },
  { label: '高', value: 3 },
  { label: '严重', value: 4 },
  { label: '致命', value: 5 },
]

const deviceTypeOptions = [
  { label: '未知', value: DeviceType.Unknown },
  { label: 'Web浏览器', value: DeviceType.Web },
  { label: 'iOS', value: DeviceType.iOS },
  { label: 'Android', value: DeviceType.Android },
  { label: 'Windows', value: DeviceType.Windows },
  { label: 'macOS', value: DeviceType.macOS },
  { label: 'Linux', value: DeviceType.Linux },
  { label: '平板', value: DeviceType.Tablet },
  { label: '小程序', value: DeviceType.MiniProgram },
  { label: 'API', value: DeviceType.Api },
]

function severityType(level: number) {
  switch (level) {
    case 1: return 'info'
    case 2: return 'warning'
    case 3:
    case 4:
    case 5: return 'error'
    default: return 'default'
  }
}

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索异常类型/位置/用户', order: 0 },
  {
    key: 'severityLevel',
    title: '严重级别',
    dataType: 'enum',
    searchable: true,
    options: severityOptions,
    searchPlaceholder: '严重等级',
    width: 90,
    order: 10,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: severityType((row as unknown as ExceptionLogListItemDto).severityLevel) }, () => getOptionLabel(severityOptions, (row as unknown as ExceptionLogListItemDto).severityLevel)),
  },
  {
    key: 'isHandled',
    title: '处理状态',
    dataType: 'boolean',
    searchable: true,
    options: handledOptions,
    searchPlaceholder: '处理状态',
    width: 100,
    order: 11,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as ExceptionLogListItemDto).isHandled ? 'success' : 'warning' }, () => (row as unknown as ExceptionLogListItemDto).isHandled ? '已处理' : '未处理'),
  },
  // 高级搜索 + 列
  { key: 'userName', title: '用户名', dataType: 'string', advancedSearch: true, minWidth: 100, order: 12 },
  { key: 'userId', title: '用户主键', dataType: 'string', advancedSearch: true, minWidth: 90, order: 13 },
  { key: 'exceptionType', title: '异常类型', dataType: 'string', advancedSearch: true, minWidth: 160, order: 14 },
  { key: 'exceptionSource', title: '异常源', dataType: 'string', advancedSearch: true, minWidth: 140, order: 15 },
  { key: 'exceptionLocation', title: '异常发生位置', dataType: 'string', advancedSearch: true, minWidth: 200, order: 16 },
  { key: 'requestPath', title: '请求路径', dataType: 'string', advancedSearch: true, minWidth: 200, order: 17 },
  { key: 'requestMethod', title: '请求方法', dataType: 'string', advancedSearch: true, width: 90, order: 18 },
  { key: 'statusCode', title: '响应状态码', dataType: 'number', advancedSearch: true, width: 100, order: 19 },
  { key: 'errorCode', title: '错误代码', dataType: 'string', advancedSearch: true, minWidth: 100, order: 20 },
  { key: 'applicationName', title: '应用程序名称', dataType: 'string', advancedSearch: true, minWidth: 130, order: 21 },
  { key: 'environmentName', title: '环境名称', dataType: 'string', advancedSearch: true, minWidth: 100, order: 22 },
  {
    key: 'deviceType',
    title: '设备类型',
    dataType: 'enum',
    advancedSearch: true,
    options: deviceTypeOptions,
    width: 100,
    order: 23,
    render: row => getOptionLabel(deviceTypeOptions, (row as unknown as ExceptionLogListItemDto).deviceType),
  },
  { key: 'traceId', title: '链路追踪 ID', dataType: 'string', advancedSearch: true, minWidth: 160, order: 24 },
  { key: 'requestId', title: '请求标识', dataType: 'string', advancedSearch: true, minWidth: 160, order: 25 },
  { key: 'sessionId', title: '会话标识', dataType: 'string', advancedSearch: true, minWidth: 160, order: 26 },
  // 仅高级搜索
  { key: 'exceptionTimeStart', title: '开始时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '开始时间', order: 27 },
  { key: 'exceptionTimeEnd', title: '结束时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '结束时间', order: 28 },
  // 仅列
  { key: 'exceptionMessage', title: '异常消息', dataType: 'string', minWidth: 260, order: 30 },
  { key: 'controllerName', title: '控制器', dataType: 'string', minWidth: 140, order: 31 },
  { key: 'actionName', title: '动作', dataType: 'string', minWidth: 140, order: 32 },
  { key: 'operationIp', title: '操作 IP', dataType: 'string', minWidth: 130, order: 33 },
  { key: 'operationLocation', title: '操作位置', dataType: 'string', minWidth: 160, order: 34 },
  { key: 'browser', title: '浏览器', dataType: 'string', minWidth: 120, order: 35 },
  { key: 'os', title: '操作系统', dataType: 'string', minWidth: 120, order: 36 },
  { key: 'applicationVersion', title: '应用程序版本', dataType: 'string', minWidth: 120, order: 37 },
  { key: 'serverHostName', title: '服务器主机', dataType: 'string', minWidth: 140, order: 38 },
  { key: 'threadId', title: '线程 ID', dataType: 'number', width: 90, order: 39 },
  { key: 'processId', title: '进程 ID', dataType: 'number', width: 90, order: 40 },
  { key: 'handledTime', title: '处理时间', dataType: 'datetime', minWidth: 170, order: 41 },
  { key: 'exceptionTime', title: '异常时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 42 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 43 },
]

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

const schema: PageSchema = {
  pageCode: 'log.exception',
  pageName: '异常日志',
  rowKey: 'basicId',
  scrollX: 2800,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return logManagementApi.exception.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        severityLevel: toNum(f.severityLevel),
        isHandled: toBool(f.isHandled),
        userName: toStr(f.userName),
        userId: toStr(f.userId),
        exceptionType: toStr(f.exceptionType),
        exceptionSource: toStr(f.exceptionSource),
        exceptionLocation: toStr(f.exceptionLocation),
        requestPath: toStr(f.requestPath),
        requestMethod: toStr(f.requestMethod),
        statusCode: toNum(f.statusCode),
        errorCode: toStr(f.errorCode),
        applicationName: toStr(f.applicationName),
        environmentName: toStr(f.environmentName),
        deviceType: (f.deviceType as DeviceType | undefined) ?? undefined,
        traceId: toStr(f.traceId),
        requestId: toStr(f.requestId),
        sessionId: toStr(f.sessionId),
        exceptionTimeStart: toIso(f.exceptionTimeStart),
        exceptionTimeEnd: toIso(f.exceptionTimeEnd),
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
  { key: 'requestId', label: '请求标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'exceptionType', label: '异常类型' },
  { key: 'errorCode', label: '错误代码' },
  { key: 'exceptionSource', label: '异常源' },
  { key: 'exceptionLocation', label: '异常发生位置', span: 2 },
  { key: 'severityLevel', label: '严重级别', options: severityOptions, type: 'enum' },
  { key: 'statusCode', label: '响应状态码' },
  { key: 'requestPath', label: '请求路径', span: 2 },
  { key: 'requestMethod', label: '请求方法' },
  { key: 'controllerName', label: '控制器' },
  { key: 'actionName', label: '动作' },
  { key: 'operationIp', label: '操作 IP' },
  { key: 'operationLocation', label: '操作位置' },
  { key: 'browser', label: '浏览器' },
  { key: 'os', label: '操作系统' },
  { key: 'deviceType', label: '设备类型', options: deviceTypeOptions, type: 'enum' },
  { key: 'applicationName', label: '应用程序名称' },
  { key: 'applicationVersion', label: '应用程序版本' },
  { key: 'environmentName', label: '环境名称' },
  { key: 'serverHostName', label: '服务器主机' },
  { key: 'threadId', label: '线程 ID' },
  { key: 'processId', label: '进程 ID' },
  { key: 'isHandled', falseText: '未处理', label: '处理状态', trueText: '已处理', type: 'boolean' },
  { key: 'handledBy', label: '处理人主键' },
  { key: 'handledTime', label: '处理时间', type: 'date' },
  { key: 'exceptionTime', label: '异常时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'handledRemark', label: '处理备注', span: 2 },
  { key: 'remark', label: '备注', span: 2 },
  { key: 'exceptionMessage', label: '异常消息', type: 'code' },
  { key: 'exceptionStackTrace', label: '异常堆栈', type: 'code' },
  { key: 'requestParams', label: '请求参数', type: 'code' },
  { key: 'requestBody', label: '请求体', type: 'code' },
  { key: 'requestHeaders', label: '请求头', type: 'code' },
  { key: 'userAgent', label: 'User-Agent', type: 'code' },
  { key: 'deviceInfo', label: '设备信息', type: 'code' },
  { key: 'extendData', label: '扩展数据', type: 'code' },
]

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as ExceptionLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
}

async function handleDetail(row: ExceptionLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.exception.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载异常日志详情失败')
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
      title="异常日志详情"
    />
  </SchemaPage>
</template>
