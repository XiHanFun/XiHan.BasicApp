<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<!--
  打印 / 打印模板页面
  职责：按租户或平台作用域管理模板、切换可用全局模板，并从统一公共 API 发起样例预览与 FIFO 直打
  接口取自后端契约（print-templates.*）：租户的「可用全局模板」是列表加作用域筛选 Global；写入的一侧由登录上下文决定
-->
<script setup lang="ts">
import type { PrintTemplateDetail, PrintTemplateRow, PrintTemplateScope } from './components/models'
import type PrintTemplateEditor from './components/PrintTemplateEditor.vue'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaResource } from '~/components'
import type { PrintDocument } from '~/printing/designer'
import { XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { onBeforeRouteLeave } from 'vue-router'
import { SchemaPage, statusConfirmText, XIconButton } from '~/components'
import { dialog, toast } from '~/composables'
import { printTemplates, toPageRequest, toSchemaPageResult } from '~/contracts'
import {
  createDefaultPrintSampleData,
  directPrintByCode,
  ensureRemotePrintDataSourcesLoaded,
  parsePrintTemplateJson,
  previewPrintByCode,
  PrintTemplateVersionChangedError,
} from '~/printing'
import { useAccessStore, useUserStore } from '~/stores'
import { PRINT_TEMPLATE_PAGE_CODE } from './components/models'
import SampleDataModal from './components/PrintSampleDataModal.vue'
import TemplateEditor from './components/PrintTemplateEditor.vue'
import MetadataModal from './components/PrintTemplateMetadataModal.vue'

defineOptions({ name: 'PrintingPrintTemplatesView' })

const { t } = useI18n()
const userStore = useUserStore()
const accessStore = useAccessStore()
const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)
const editorRef = ref<InstanceType<typeof PrintTemplateEditor> | null>(null)
const editorVisible = ref(false)
const editorDetail = ref<PrintTemplateDetail | null>(null)
const metadataVisible = ref(false)
/** 编辑弹窗里的模板详情；null 是新增 */
const metadataDetail = ref<PrintTemplateDetail | null>(null)
const actionLoading = ref(false)
const previewLoading = ref(false)
const samplePreviewVisible = ref(false)
const samplePreviewContext = ref<{
  detail: PrintTemplateDetail
  document: PrintDocument
  scope: PrintTemplateScope
} | null>(null)
/** 当前是否在平台：取用户信息（守卫每次整页加载都会重取，切换上下文会整页重载） */
const isPlatform = computed(() => userStore.userInfo?.isPlatform ?? false)
const activeScope = ref<PrintTemplateScope>(isPlatform.value ? 'Global' : 'Tenant')

const canMaintain = computed(() => activeScope.value === 'Tenant'
  || (isPlatform.value && accessStore.hasCode('printing.print-templates.global-manage')))
const currentScopeLabel = computed(() => activeScope.value === 'Tenant'
  ? t('printing.print_templates.tenant_templates')
  : t('printing.print_templates.available_global_templates'))
const targetScopeLabel = computed(() => activeScope.value === 'Tenant'
  ? t('printing.print_templates.available_global_templates')
  : t('printing.print_templates.tenant_templates'))
const scopeSwitchLabel = computed(() => t('printing.print_templates.switch_scope_tip', {
  current: currentScopeLabel.value,
  target: targetScopeLabel.value,
}))

watch(isPlatform, platform => activeScope.value = platform ? 'Global' : 'Tenant')
watch(samplePreviewVisible, (show) => {
  if (!show) {
    samplePreviewContext.value = null
  }
})

onBeforeRouteLeave(async () => !editorVisible.value || (await editorRef.value?.confirmDiscard()) !== false)

/** 在租户私有模板与开放的全局模板之间切换 */
function switchScope(): void {
  if (isPlatform.value)
    return
  activeScope.value = activeScope.value === 'Tenant'
    ? 'Global'
    : 'Tenant'
}

const fields = computed<ListFieldSchema[]>(() => [
  {
    key: 'keyword',
    title: t('printing.print_templates.keyword'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('printing.print_templates.keyword_placeholder'),
    order: 0,
  },
  {
    key: 'templateCode',
    title: t('printing.print_templates.template_code'),
    dataType: 'string',
    sortable: true,
    minWidth: 170,
    order: 1,
  },
  {
    key: 'templateName',
    title: t('printing.print_templates.template_name'),
    dataType: 'string',
    sortable: true,
    minWidth: 170,
    order: 2,
  },
  {
    key: 'dataSourceCode',
    title: t('printing.print_templates.data_source'),
    dataType: 'string',
    sortable: true,
    minWidth: 170,
    order: 3,
    render: (row) => {
      const code = (row as unknown as PrintTemplateRow).dataSourceCode
      return code || h(XhTagRoot, { variant: 'subtle', tone: 'neutral' }, () => h(XhTagLabel, () => t('printing.print_templates.free_template')))
    },
  },
  {
    key: 'allowTenantUse',
    title: t('printing.print_templates.allow_tenant_use'),
    dataType: 'boolean',
    width: 110,
    order: 5,
    visible: activeScope.value === 'Global',
    render: row => h(XhTagRoot, {
      variant: 'subtle',
      tone: (row as unknown as PrintTemplateRow).allowTenantUse ? 'success' : 'neutral',
    }, () => h(XhTagLabel, () => (row as unknown as PrintTemplateRow).allowTenantUse
      ? t('common.statuses.yes')
      : t('common.statuses.no'))),
  },
  {
    key: 'status',
    title: t('printing.print_templates.status'),
    dataType: 'enum',
    width: 90,
    sortable: true,
    order: 6,
    render: row => h(XhTagRoot, {
      variant: 'subtle',
      tone: (row as unknown as PrintTemplateRow).status === 'Active'
        ? 'success'
        : 'danger',
    }, () => h(XhTagLabel, () => (row as unknown as PrintTemplateRow).status === 'Active'
      ? t('common.statuses.enabled')
      : t('common.statuses.disabled'))),
  },
  {
    key: 'order',
    title: t('printing.print_templates.order'),
    dataType: 'number',
    width: 80,
    sortable: true,
    order: 7,
  },
  {
    key: 'remark',
    title: t('printing.print_templates.remark'),
    dataType: 'string',
    minWidth: 180,
    order: 8,
  },
])

/** 分页：租户的「可用全局模板」加作用域筛选 Global（只有启用且开放的）；平台缺省就是全局模板 */
const resource: SchemaResource<PrintTemplateRow> = {
  page: async (params) => {
    const request = toPageRequest(params)
    const filters = !isPlatform.value && activeScope.value === 'Global'
      ? [
          ...request.filters,
          { field: 'scope', operator: 'eq', value: 'Global' },
        ]
      : request.filters
    return toSchemaPageResult(await printTemplates.list({ ...request, filters }), params)
  },
}

const schema = computed<PageSchema>(() => ({
  pageCode: PRINT_TEMPLATE_PAGE_CODE,
  pageName: t('printing.print_templates.page_name'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    ...(canMaintain.value
      ? [
          {
            key: 'create',
            title: t('printing.print_templates.action_create'),
            scope: 'page' as const,
            tone: 'brand' as const,
            icon: 'lucide:plus',
            permission: 'printing.print-templates.create',
          },
        ]
      : []),
    {
      key: 'preview',
      title: t('printing.print_templates.action_preview'),
      scope: 'row',
      icon: 'lucide:scan-eye',
      permission: 'printing.print-templates.use',
      disabled: row => (row as unknown as PrintTemplateRow).status !== 'Active',
    },
    {
      key: 'direct',
      title: t('printing.print_templates.action_direct'),
      scope: 'row',
      icon: 'lucide:printer',
      permission: 'printing.print-templates.use',
      disabled: row => (row as unknown as PrintTemplateRow).status !== 'Active',
    },
    {
      key: 'edit',
      title: t('common.actions.edit'),
      scope: 'row',
      icon: 'lucide:pencil',
      permission: 'printing.print-templates.update',
      visible: () => canMaintain.value,
    },
    {
      key: 'design',
      title: t('printing.print_templates.action_design'),
      scope: 'row',
      icon: 'lucide:pen-tool',
      permission: 'printing.print-templates.update',
      visible: () => canMaintain.value,
    },
    {
      key: 'toggle',
      title: t('printing.print_templates.action_toggle'),
      scope: 'row',
      icon: 'lucide:power',
      confirm: true,
      confirmText: row => statusConfirmText(t, (row as unknown as PrintTemplateRow).status === 'Active', (row as unknown as PrintTemplateRow).templateName),
      permission: 'printing.print-templates.status',
      visible: () => canMaintain.value,
    },
    {
      key: 'delete',
      title: t('printing.print_templates.action_delete'),
      scope: 'row',
      tone: 'danger',
      icon: 'lucide:trash-2',
      permission: 'printing.print-templates.delete',
      visible: () => canMaintain.value,
      disabled: row => (row as unknown as PrintTemplateRow).status !== 'Disabled',
    },
  ],
}))

/** 分发页面与行操作 */
function onAction(payload: SchemaActionPayload): void {
  const row = payload.row as unknown as PrintTemplateRow | undefined
  if (payload.key === 'create') {
    metadataDetail.value = null
    metadataVisible.value = true
    return
  }
  if (!row)
    return
  if (payload.key === 'edit')
    void openMetadata(row)
  else if (payload.key === 'design')
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

/** 加载最新详情（并发戳）后打开编辑弹窗 */
async function openMetadata(row: PrintTemplateRow): Promise<void> {
  try {
    metadataDetail.value = await printTemplates.get({ id: row.id })
    metadataVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('printing.print_templates.not_found'))
  }
}

/** 元数据存好后刷新列表；新建的模板版面是空白的，接着打开设计器 */
function onMetadataSaved(detail: PrintTemplateDetail, created: boolean): void {
  void schemaPageRef.value?.reload()
  if (!created)
    return
  editorDetail.value = detail
  editorVisible.value = true
}

/** 加载完整 JSON 后打开全屏设计器 */
async function openEditor(row: PrintTemplateRow): Promise<void> {
  try {
    editorDetail.value = await printTemplates.get({ id: row.id })
    editorVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('printing.print_templates.not_found'))
  }
}

/** 加载已保存模板详情，并打开模板驱动的内存模拟数据表单 */
async function openSamplePreview(row: PrintTemplateRow): Promise<void> {
  if (actionLoading.value)
    return
  actionLoading.value = true
  try {
    const scope = activeScope.value
    const detail = await printTemplates.get({ id: row.id })

    // 表单必须基于详情接口返回的版本构建，列表摘要不足以保证并发预览安全；旧格式的模板在这里就明确报错
    const document = parsePrintTemplateJson(detail.templateJson, detail.templateCode)
    samplePreviewContext.value = { detail, document, scope }
    samplePreviewVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('printing.print_templates.preview_failed'))
  }
  finally {
    actionLoading.value = false
  }
}

/**
 * 使用用户填写的模拟数据预览已保存模板
 * @param sample 模拟数据弹窗产生的单对象或对象数组，仅在本次调用内存中使用
 * @returns 预览窗口打开完成信号
 */
async function previewSampleData(sample: Record<string, unknown> | Record<string, unknown>[]): Promise<void> {
  if (previewLoading.value)
    return
  const context = samplePreviewContext.value
  if (!context) {
    toast.danger(t('printing.print_templates.sample_template_missing'))
    return
  }

  previewLoading.value = true
  try {
    await previewPrintByCode(context.detail.templateCode, sample, {
      scope: context.scope,
      title: context.detail.templateName,
      expectedRowVersion: context.detail.concurrencyStamp,
    })
    samplePreviewVisible.value = false
  }
  catch (error) {
    toast.danger(error instanceof PrintTemplateVersionChangedError
      ? t('printing.print_templates.sample_version_changed')
      : (error as Error).message || t('printing.print_templates.preview_failed'))
  }
  finally {
    previewLoading.value = false
  }
}

/** 使用注册样例或自由模板空白字段从公共 FIFO API 直打，不自动回退浏览器预览 */
async function direct(row: PrintTemplateRow): Promise<void> {
  if (actionLoading.value)
    return
  actionLoading.value = true
  try {
    const detail = await printTemplates.get({ id: row.id })
    // 样例生成读取数据源注册表，先确保后端目录已加载
    await ensureRemotePrintDataSourcesLoaded()
    const sample = await createDefaultPrintSampleData(detail.dataSourceCode ?? null)
    await directPrintByCode(row.templateCode, sample, { scope: activeScope.value, title: row.templateName })
    toast.success(t('printing.print_templates.direct_success'))
  }
  catch (error) {
    toast.danger((error as Error).message || t('printing.print_templates.direct_failed'))
  }
  finally {
    actionLoading.value = false
  }
}

/** 使用列表返回的并发戳执行启停，防止覆盖并行编辑 */
async function toggleStatus(row: PrintTemplateRow): Promise<void> {
  if (actionLoading.value)
    return
  actionLoading.value = true
  try {
    await printTemplates.setStatus({
      id: row.id,
      concurrencyStamp: row.concurrencyStamp,
      status: row.status === 'Active'
        ? 'Disabled'
        : 'Active',
    })
    toast.success(t('printing.print_templates.status_success'))
    void schemaPageRef.value?.reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('printing.print_templates.status_failed'))
  }
  finally {
    actionLoading.value = false
  }
}

/** 确认并删除已停用模板 */
function remove(row: PrintTemplateRow): void {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('printing.print_templates.delete_title'),
    content: t('printing.print_templates.delete_confirm', { code: row.templateCode }),
    okText: t('common.actions.delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      if (actionLoading.value)
        return false
      actionLoading.value = true
      try {
        // 删除是批量形态：逐项结果里没有成功即报失败（启用中的模板后端逐项拒绝）
        const result = await printTemplates.delete({
          ids: [row.id],
        })
        if (result.failed.length > 0)
          throw new Error(t('printing.print_templates.delete_failed'))
        toast.success(t('printing.print_templates.delete_success'))
        void schemaPageRef.value?.reload()
        return true
      }
      catch (error) {
        toast.danger((error as Error).message || t('printing.print_templates.delete_failed'))
        return false
      }
      finally {
        actionLoading.value = false
      }
    },
  })
}
</script>

<template>
  <div class="flex h-full min-h-0 flex-col gap-3">
    <SchemaPage ref="schemaPageRef" :key="activeScope" class="min-h-0 flex-1" :schema="schema" @action="onAction">
      <!-- 作用域切换钮与同排的刷新、导出等一样是 XIconButton：同一套尺寸、悬停与按下 -->
      <template v-if="!isPlatform" #toolbar>
        <XIconButton
          :icon="activeScope === 'Tenant' ? 'lucide:globe-2' : 'lucide:building-2'"
          :label="scopeSwitchLabel"
          @click="switchScope"
        />
      </template>
    </SchemaPage>

    <TemplateEditor
      ref="editorRef"
      v-model:show="editorVisible"
      :detail="editorDetail"
      :scope="activeScope"
      :global-mode="activeScope === 'Global'"
      @saved="schemaPageRef?.reload()"
    />

    <MetadataModal
      v-model:show="metadataVisible"
      :detail="metadataDetail"
      :global-mode="activeScope === 'Global'"
      @saved="onMetadataSaved"
    />

    <SampleDataModal
      v-model:show="samplePreviewVisible"
      :data-source-code="samplePreviewContext?.detail.dataSourceCode ?? null"
      :submitting="previewLoading"
      :template="samplePreviewContext?.document ?? null"
      :template-code="samplePreviewContext?.detail.templateCode"
      :template-name="samplePreviewContext?.detail.templateName"
      @preview="previewSampleData"
    />
  </div>
</template>
