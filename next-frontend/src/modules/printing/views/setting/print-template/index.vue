<!--
  系统设置 / 打印模板页面。
  职责：按租户或平台作用域管理模板、切换可用全局模板，并从统一公共 API 发起样例预览与 FIFO 直打。
-->
<script setup lang="ts">
import type { PrintTemplateDetailDto, PrintTemplateListItemDto } from '../../../api/print-template.types'
import type PrintTemplateEditor from './components/PrintTemplateEditor.vue'
import type { PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhBadge, XhButton, XhSpinner } from '@xihan-ui/vue'
import { computed, h, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { onBeforeRouteLeave } from 'vue-router'
import {
  createPageRequest,
  EnableStatus,
  querySortsFromSchema,
  tenantApi,
} from '@/api'
import { SchemaPage } from '~/components'
import { dialog, toast } from '~/composables'
import { Icon } from '~/iconify'
import {
  createDefaultPrintSampleData,
  directPrintByCode,
  ensureRemotePrintDataSourcesLoaded,
  previewPrintByCode,
  PrintTemplateVersionChangedError,
} from '~/printing'
import { useUserStore } from '~/stores'
import { printTemplateApi } from '../../../api/print-template'
import { PrintTemplateScope } from '../../../api/print-template.types'
import SampleDataModal from './components/PrintSampleDataModal.vue'
import TemplateEditor from './components/PrintTemplateEditor.vue'

defineOptions({ name: 'SettingPrintTemplatePage' })

const { t } = useI18n()
const userStore = useUserStore()
const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)
const editorRef = ref<InstanceType<typeof PrintTemplateEditor> | null>(null)
const editorVisible = ref(false)
const editorDetail = ref<PrintTemplateDetailDto | null>(null)
const actionLoading = ref(false)
const previewLoading = ref(false)
const samplePreviewVisible = ref(false)
const samplePreviewTemplate = ref<Record<string, unknown> | null>(null)
const samplePreviewContext = ref<{
  detail: PrintTemplateDetailDto
  scope: PrintTemplateScope
} | null>(null)
const isPlatform = ref(!userStore.userInfo?.tenantId)
const contextResolved = ref(false)
const activeScope = ref<PrintTemplateScope>(isPlatform.value ? PrintTemplateScope.Global : PrintTemplateScope.Tenant)

const canMaintain = computed(() => activeScope.value === PrintTemplateScope.Tenant
  || (isPlatform.value && userStore.hasPermission('print-template:global-manage')))
const currentScopeLabel = computed(() => activeScope.value === PrintTemplateScope.Tenant
  ? t('setting.print_template.tenant_templates')
  : t('setting.print_template.available_global_templates'))
const targetScopeLabel = computed(() => activeScope.value === PrintTemplateScope.Tenant
  ? t('setting.print_template.available_global_templates')
  : t('setting.print_template.tenant_templates'))
const scopeSwitchLabel = computed(() => t('setting.print_template.switch_scope_tip', {
  current: currentScopeLabel.value,
  target: targetScopeLabel.value,
}))

watch(isPlatform, platform => activeScope.value = platform ? PrintTemplateScope.Global : PrintTemplateScope.Tenant)
watch(samplePreviewVisible, (show) => {
  if (!show) {
    samplePreviewTemplate.value = null
    samplePreviewContext.value = null
  }
})

onMounted(() => void resolveCurrentContext())

onBeforeRouteLeave(async () => !editorVisible.value || (await editorRef.value?.confirmDiscard()) !== false)

/** 使用当前令牌的租户切换列表确定平台/租户事实状态。 */
async function resolveCurrentContext(): Promise<void> {
  try {
    const tenants = await tenantApi.myAvailableTenants()
    isPlatform.value = !tenants.some(tenant => tenant.isCurrent)
  }
  catch {
    isPlatform.value = !userStore.userInfo?.tenantId
  }
  finally {
    contextResolved.value = true
  }
}

/** 在租户私有模板与开放的全局模板之间切换。 */
function switchScope(): void {
  if (isPlatform.value)
    return
  activeScope.value = activeScope.value === PrintTemplateScope.Tenant
    ? PrintTemplateScope.Global
    : PrintTemplateScope.Tenant
}

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('setting.print_template.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('setting.print_template.keyword_placeholder'), order: 0 },
  { key: 'templateCode', title: t('setting.print_template.template_code'), dataType: 'string', sortable: true, minWidth: 170, order: 1 },
  { key: 'templateName', title: t('setting.print_template.template_name'), dataType: 'string', sortable: true, minWidth: 170, order: 2 },
  {
    key: 'dataSourceCode',
    title: t('setting.print_template.data_source'),
    dataType: 'string',
    sortable: true,
    minWidth: 170,
    order: 3,
    render: (row) => {
      const code = (row as unknown as PrintTemplateListItemDto).dataSourceCode
      return code || h(XhBadge, { variant: 'subtle', tone: 'neutral', size: 'sm' }, () => t('setting.print_template.free_template'))
    },
  },
  { key: 'engineVersion', title: t('setting.print_template.engine_version'), dataType: 'string', width: 110, order: 4 },
  {
    key: 'allowTenantUse',
    title: t('setting.print_template.allow_tenant_use'),
    dataType: 'boolean',
    width: 110,
    order: 5,
    visible: activeScope.value === PrintTemplateScope.Global,
    render: row => h(XhBadge, { variant: 'subtle', tone: (row as unknown as PrintTemplateListItemDto).allowTenantUse ? 'success' : 'neutral', size: 'sm' }, () => (row as unknown as PrintTemplateListItemDto).allowTenantUse ? t('common.statuses.yes') : t('common.statuses.no')),
  },
  {
    key: 'status',
    title: t('setting.print_template.status'),
    dataType: 'enum',
    width: 90,
    sortable: true,
    order: 6,
    dictionaryCode: 'EnableStatus',
    render: row => h(XhBadge, { variant: 'subtle', tone: (row as unknown as PrintTemplateListItemDto).status === EnableStatus.Enabled ? 'success' : 'danger', size: 'sm' }, () => (row as unknown as PrintTemplateListItemDto).status === EnableStatus.Enabled ? t('common.statuses.enabled') : t('common.statuses.disabled')),
  },
  { key: 'sort', title: t('setting.print_template.sort'), dataType: 'number', width: 80, sortable: true, order: 7 },
  { key: 'remark', title: t('setting.print_template.remark'), dataType: 'string', minWidth: 180, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'setting.print-template',
  pageName: t('setting.print_template.page_name'),
  rowKey: 'basicId',
  scrollX: 1200,
  fields: fields.value,
  resource: {
    page: (params) => {
      const input = {
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        scope: activeScope.value,
        keyword: String(params.filters.keyword ?? '').trim() || undefined,
      }
      const request = !isPlatform.value && activeScope.value === PrintTemplateScope.Global
        ? printTemplateApi.availableGlobalPage(input)
        : printTemplateApi.page(input)
      return request as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    ...(canMaintain.value
      ? [{ key: 'create', title: t('setting.print_template.action_create'), scope: 'page' as const, type: 'primary' as const, icon: 'lucide:plus', permission: 'print-template:create' }]
      : []),
    { key: 'preview', title: t('setting.print_template.action_preview'), scope: 'row', icon: 'lucide:scan-eye', permission: 'print-template:use', disabled: row => (row as unknown as PrintTemplateListItemDto).status !== EnableStatus.Enabled },
    { key: 'direct', title: t('setting.print_template.action_direct'), scope: 'row', icon: 'lucide:printer', permission: 'print-template:use', disabled: row => (row as unknown as PrintTemplateListItemDto).status !== EnableStatus.Enabled },
    { key: 'edit', title: t('setting.print_template.action_edit'), scope: 'row', icon: 'lucide:pen', permission: 'print-template:update', visible: () => canMaintain.value },
    { key: 'toggle', title: t('setting.print_template.action_toggle'), scope: 'row', icon: 'lucide:power', permission: 'print-template:status', visible: () => canMaintain.value },
    { key: 'delete', title: t('setting.print_template.action_delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2', permission: 'print-template:delete', visible: () => canMaintain.value, disabled: row => (row as unknown as PrintTemplateListItemDto).status !== EnableStatus.Disabled },
  ],
}))

/** 分发页面与行操作。 */
function onAction(payload: SchemaActionPayload): void {
  const row = payload.row as unknown as PrintTemplateListItemDto | undefined
  if (payload.key === 'create') {
    editorDetail.value = null
    editorVisible.value = true
    return
  }
  if (!row)
    return
  if (payload.key === 'edit')
    void openEditor(row)
  else if (payload.key === 'toggle')
    void toggleStatus(row)
  else if (payload.key === 'delete')
    remove(row)
  else if (payload.key === 'preview')
    void openSamplePreview(row)
  else if (payload.key === 'direct')
    void direct(row)
}

/** 加载完整 JSON 后打开全屏设计器。 */
async function openEditor(row: PrintTemplateListItemDto): Promise<void> {
  try {
    editorDetail.value = await printTemplateApi.detail(row.basicId, activeScope.value)
    if (!editorDetail.value)
      throw new Error(t('setting.print_template.not_found'))
    editorVisible.value = true
  }
  catch (error) {
    toast.error((error as Error).message || t('setting.print_template.not_found'))
  }
}

/** 加载已保存模板详情，并打开模板驱动的内存模拟数据表单。 */
async function openSamplePreview(row: PrintTemplateListItemDto): Promise<void> {
  if (actionLoading.value)
    return
  actionLoading.value = true
  try {
    const scope = activeScope.value
    const detail = await printTemplateApi.detail(row.basicId, scope)
    if (!detail)
      throw new Error(t('setting.print_template.not_found'))

    // 表单必须基于详情接口返回的完整 JSON 和版本构建，列表摘要不足以保证并发预览安全。
    samplePreviewTemplate.value = parseTemplateJson(detail.templateJson)
    samplePreviewContext.value = { detail, scope }
    samplePreviewVisible.value = true
  }
  catch (error) {
    toast.error((error as Error).message || t('setting.print_template.preview_failed'))
  }
  finally {
    actionLoading.value = false
  }
}

/**
 * 使用用户填写的模拟数据预览已保存模板。
 * @param sample 模拟数据弹窗产生的单对象或对象数组，仅在本次调用内存中使用。
 * @returns 预览窗口打开完成信号。
 */
async function previewSampleData(sample: Record<string, unknown> | Record<string, unknown>[]): Promise<void> {
  if (previewLoading.value)
    return
  const context = samplePreviewContext.value
  if (!context) {
    toast.error(t('setting.print_template.sample_template_missing'))
    return
  }

  previewLoading.value = true
  try {
    await previewPrintByCode(context.detail.templateCode, sample, {
      scope: context.scope,
      title: context.detail.templateName,
      expectedRowVersion: context.detail.rowVersion,
    })
    samplePreviewVisible.value = false
  }
  catch (error) {
    toast.error(error instanceof PrintTemplateVersionChangedError
      ? t('setting.print_template.sample_version_changed')
      : (error as Error).message || t('setting.print_template.preview_failed'))
  }
  finally {
    previewLoading.value = false
  }
}

/** 使用注册样例或自由模板空白字段从公共 FIFO API 直打，不自动回退浏览器预览。 */
async function direct(row: PrintTemplateListItemDto): Promise<void> {
  if (actionLoading.value)
    return
  actionLoading.value = true
  try {
    const detail = await printTemplateApi.detail(row.basicId, activeScope.value)
    if (!detail)
      throw new Error(t('setting.print_template.not_found'))
    // 样例生成读取数据源注册表，先确保后端目录已加载
    await ensureRemotePrintDataSourcesLoaded()
    const sample = await createDefaultPrintSampleData(
      parseTemplateJson(detail.templateJson),
      detail.dataSourceCode,
    )
    await directPrintByCode(row.templateCode, sample, { scope: activeScope.value, title: row.templateName })
    toast.success(t('setting.print_template.direct_success'))
  }
  catch (error) {
    toast.error((error as Error).message || t('setting.print_template.direct_failed'))
  }
  finally {
    actionLoading.value = false
  }
}

/** 使用列表返回的行版本执行启停，防止覆盖并行编辑。 */
async function toggleStatus(row: PrintTemplateListItemDto): Promise<void> {
  if (actionLoading.value)
    return
  actionLoading.value = true
  try {
    await printTemplateApi.updateStatus({
      basicId: row.basicId,
      scope: activeScope.value,
      rowVersion: row.rowVersion,
      status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
    })
    toast.success(t('setting.print_template.status_success'))
    void schemaPageRef.value?.reload()
  }
  catch (error) {
    toast.error((error as Error).message || t('setting.print_template.status_failed'))
  }
  finally {
    actionLoading.value = false
  }
}

/** 确认并删除已停用模板。 */
function remove(row: PrintTemplateListItemDto): void {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('setting.print_template.delete_title'),
    content: t('setting.print_template.delete_confirm', { code: row.templateCode }),
    okText: t('common.actions.delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      if (actionLoading.value)
        return false
      actionLoading.value = true
      try {
        await printTemplateApi.delete({ basicId: row.basicId, scope: activeScope.value, rowVersion: row.rowVersion })
        toast.success(t('setting.print_template.delete_success'))
        void schemaPageRef.value?.reload()
        return true
      }
      catch (error) {
        toast.error((error as Error).message || t('setting.print_template.delete_failed'))
        return false
      }
      finally {
        actionLoading.value = false
      }
    },
  })
}

/**
 * 解析列表详情中的 hiprint 根模板对象。
 * @param value 后端返回的模板 JSON 文本。
 * @returns 可供字段解析器读取的模板根对象。
 * @throws JSON 损坏、根节点不是对象或缺少有效 panels 时抛出友好错误。
 */
function parseTemplateJson(value: string): Record<string, unknown> {
  let parsed: unknown
  try {
    parsed = JSON.parse(value)
  }
  catch {
    throw new Error(t('setting.print_template.json_syntax_invalid'))
  }
  if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed))
    throw new Error(t('setting.print_template.invalid_json'))
  const panels = (parsed as { panels?: unknown }).panels
  if (!Array.isArray(panels) || panels.length === 0)
    throw new Error(t('setting.print_template.json_panels_required'))
  return parsed as Record<string, unknown>
}
</script>

<template>
  <div class="flex h-full min-h-0 flex-col gap-3">
    <XhSpinner v-if="!contextResolved" class="flex-1 py-12" />
    <SchemaPage v-else ref="schemaPageRef" :key="activeScope" class="min-h-0 flex-1" :schema="schema" @action="onAction">
      <template v-if="!isPlatform" #toolbar>
        <XhButton :title="scopeSwitchLabel" circle variant="ghost" size="sm" :aria-label="scopeSwitchLabel" @click="switchScope">
          <span><Icon :icon="activeScope === PrintTemplateScope.Tenant ? 'lucide:globe-2' : 'lucide:building-2'" /></span>
        </XhButton>
      </template>
    </SchemaPage>

    <TemplateEditor
      ref="editorRef"
      v-model:show="editorVisible"
      :detail="editorDetail"
      :scope="activeScope"
      :global-mode="activeScope === PrintTemplateScope.Global"
      @saved="schemaPageRef?.reload()"
    />

    <SampleDataModal
      v-model:show="samplePreviewVisible"
      :data-source-code="samplePreviewContext?.detail.dataSourceCode ?? null"
      :submitting="previewLoading"
      :template="samplePreviewTemplate"
      :template-code="samplePreviewContext?.detail.templateCode"
      :template-name="samplePreviewContext?.detail.templateName"
      @preview="previewSampleData"
    />
  </div>
</template>
