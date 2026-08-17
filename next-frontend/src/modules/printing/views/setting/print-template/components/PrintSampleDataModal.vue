<!--
  模拟打印数据填写弹窗。
  职责：根据当前模板实际字段绑定生成表单，管理单对象/对象数组样例，并仅在内存中向预览入口提交纯打印数据。
-->
<script setup lang="ts">
import type { PrintSampleFormField, PrintSampleFormSchema } from '~/printing'
import { XhAlertDescription, XhAlertRoot, XhBadge, XhButton, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateRoot, XhFieldControl, XhFieldLabel, XhFieldRoot, XhFlex, XhFormRoot, XhSpinner } from '@xihan-ui/vue'
import { computed, onBeforeUnmount, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import {
  clonePrintSampleRecord,
  createBlankPrintSampleRecord,
  createPrintSamplePayload,
  ensureRemotePrintDataSourcesLoaded,
  extractPrintSampleFormSchema,
  getPrintDataSource,
  getPrintSampleValue,
  normalizePrintSampleData,
  setPrintSampleValue,
} from '~/printing'
import TableEditor from './PrintSampleTableEditor.vue'
import ValueEditor from './PrintSampleValueEditor.vue'

defineOptions({ name: 'PrintSampleDataModal' })

const props = defineProps<{
  dataSourceCode: null | string
  show: boolean
  submitting: boolean
  template: null | Record<string, unknown>
  templateCode?: string
  templateName?: string
}>()

const emit = defineEmits<{
  'preview': [data: Record<string, unknown> | Record<string, unknown>[]]
  'update:show': [value: boolean]
}>()

interface EditableRecord {
  /** 仅用于页签稳定渲染，不写入打印数据。 */
  id: string
  data: Record<string, unknown>
}

const { t } = useI18n()
const loading = ref(false)
const loadError = ref('')
const schema = ref<PrintSampleFormSchema>({ fields: [], warnings: [] })
const records = shallowRef<EditableRecord[]>([])
const isCollection = ref(false)
const activeRecordId = ref('')
const dataSourceName = ref('')
let initializationVersion = 0

const currentRecord = computed(() => records.value.find(record => record.id === activeRecordId.value)
  ?? records.value[0]
  ?? null)
const unregisteredFields = computed(() => props.dataSourceCode
  ? schema.value.fields.filter(field => !field.registered)
  : [])
const canPreview = computed(() => !loading.value
  && !loadError.value
  && records.value.length > 0
  && Boolean(props.template))

watch(
  () => props.show,
  (show) => {
    if (show)
      void initialize()
    else
      clearSession()
  },
)

onBeforeUnmount(() => {
  initializationVersion += 1
  clearSession()
})

/**
 * 初始化模板字段结构；契约增强模式使用注册样例，自由模式按模板字段创建空白数据。
 * @returns 初始化完成信号。
 * @throws 所有异常都会转换为弹窗内错误，不向事件循环泄漏。
 */
async function initialize(): Promise<void> {
  const requestVersion = ++initializationVersion
  loading.value = true
  loadError.value = ''
  try {
    if (!props.template)
      throw new Error(t('setting.print_template.sample_template_missing'))
    // 字段结构与样例工厂读取数据源注册表，先确保后端目录已加载
    await ensureRemotePrintDataSourcesLoaded()
    const nextSchema = extractPrintSampleFormSchema(props.template, props.dataSourceCode)
    const source = getPrintDataSource(props.dataSourceCode)
    const sample = source
      ? await source.createSampleData()
      : createBlankPrintSampleRecord(nextSchema.fields)
    const dataSet = normalizePrintSampleData(sample)
    if (requestVersion !== initializationVersion || !props.show)
      return

    dataSourceName.value = source?.name ?? t('setting.print_template.free_template')
    schema.value = nextSchema
    isCollection.value = dataSet.isCollection
    setRecords(dataSet.records)
  }
  catch (error) {
    if (requestVersion === initializationVersion)
      loadError.value = (error as Error).message || t('setting.print_template.sample_load_failed')
  }
  finally {
    if (requestVersion === initializationVersion)
      loading.value = false
  }
}

/**
 * 重新执行样例工厂，恢复当前数据源最新默认数据。
 * @returns 重置完成信号。
 */
async function resetDefaultSample(): Promise<void> {
  if (loading.value || props.submitting)
    return
  await initialize()
}

/**
 * 更新当前记录的普通字段或整个明细表。
 * @param field 模板表单字段。
 * @param value 新值。
 * @returns 无返回值。
 */
function updateField(field: PrintSampleFormField, value: unknown): void {
  const current = currentRecord.value
  if (!current)
    return
  const nextData = clonePrintSampleRecord(current.data)
  setPrintSampleValue(nextData, field.key, value)
  records.value = records.value.map(record => record.id === current.id
    ? { ...record, data: nextData }
    : record)
}

/** 新增一条按当前模板字段初始化的空白顶层记录。 */
function addRecord(): void {
  const record = {
    id: crypto.randomUUID(),
    data: createBlankPrintSampleRecord(schema.value.fields),
  }
  records.value = [...records.value, record]
  activeRecordId.value = record.id
}

/** 复制当前记录及其嵌套明细，确保新记录不存在共享引用。 */
function duplicateRecord(): void {
  const current = currentRecord.value
  if (!current)
    return
  const currentIndex = records.value.findIndex(record => record.id === current.id)
  const duplicate = { id: crypto.randomUUID(), data: clonePrintSampleRecord(current.data) }
  records.value = [
    ...records.value.slice(0, currentIndex + 1),
    duplicate,
    ...records.value.slice(currentIndex + 1),
  ]
  activeRecordId.value = duplicate.id
}

/** 删除当前记录；顶层数组至少保留一条，避免提交空载荷。 */
function deleteRecord(): void {
  if (records.value.length <= 1)
    return
  const currentIndex = records.value.findIndex(record => record.id === activeRecordId.value)
  const nextRecords = records.value.filter(record => record.id !== activeRecordId.value)
  records.value = nextRecords
  activeRecordId.value = nextRecords[Math.min(Math.max(currentIndex, 0), nextRecords.length - 1)]!.id
}

/**
 * 提交不含组件 UUID 的模拟数据；真正预览由所属入口负责并使用 loading 锁。
 * @returns 无返回值。
 */
function submitPreview(): void {
  if (!canPreview.value || props.submitting)
    return
  const payload = createPrintSamplePayload(records.value.map(record => record.data), isCollection.value)
  emit('preview', payload)
}

/** 拒绝在提交期间关闭，避免预览结果与用户看到的表单状态错位。 */
function requestVisible(value: boolean): void {
  if (!value && !props.submitting)
    emit('update:show', false)
}

/** 用独立数据和 UUID 替换全部记录。 */
function setRecords(values: readonly Record<string, unknown>[]): void {
  records.value = values.map(data => ({ id: crypto.randomUUID(), data: clonePrintSampleRecord(data) }))
  activeRecordId.value = records.value[0]?.id ?? ''
}

/** 清理所有模拟数据引用，确保关闭后不会在页面状态中保留打印内容。 */
function clearSession(): void {
  initializationVersion += 1
  loading.value = false
  loadError.value = ''
  schema.value = { fields: [], warnings: [] }
  records.value = []
  isCollection.value = false
  activeRecordId.value = ''
  dataSourceName.value = ''
}
</script>

<template>
  <XhDialogRoot
    :open="show"
    :close-on-interact-outside="false"
    @update:open="requestVisible"
  >
    <XhDialogContent class="print-sample-data-modal">
      <XhDialogTitle>{{ t('setting.print_template.sample_data_title') }}</XhDialogTitle>
      <XhDialogCloseTrigger>✕</XhDialogCloseTrigger>
      <template #header-extra>
        <XhFlex align="center" size="sm">
          <XhBadge variant="subtle" size="sm" tone="info">
            {{ dataSourceName || dataSourceCode || t('setting.print_template.free_template') }}
          </XhBadge>
          <XhBadge v-if="templateCode" variant="subtle" size="sm">
            {{ templateCode }}
          </XhBadge>
        </XhFlex>
      </template>

      <div class="xh-loading-stage">
        <div v-if="loading" class="xh-loading-stage__veil">
          <XhSpinner />
        </div>
        <div class="sample-data-body">
          <XhAlertRoot v-if="loadError" tone="danger">
            <XhAlertDescription>
              {{ loadError }}
            </XhAlertDescription>
          </XhAlertRoot>

          <template v-else>
            <div class="sample-data-intro">
              <div>
                <strong>{{ templateName || templateCode || t('setting.print_template.sample_current_template') }}</strong>
                <p>{{ t('setting.print_template.sample_memory_only') }}</p>
              </div>
              <XhButton
                variant="subtle"
                :disabled="submitting"
                :loading="loading"
                @click="resetDefaultSample"
              >
                <span><Icon icon="tabler:restore" /></span>
                {{ t('setting.print_template.sample_reset_default') }}
              </XhButton>
            </div>

            <XhAlertRoot v-for="field in unregisteredFields" :key="field.key" tone="warning">
              <XhAlertDescription>
                {{ t('setting.print_template.sample_unregistered_field', { field: field.key, source: dataSourceCode }) }}
              </XhAlertDescription>
            </XhAlertRoot>

            <section v-if="isCollection" class="sample-records">
              <div class="sample-records-heading">
                <span>{{ t('setting.print_template.sample_records', { count: records.length }) }}</span>
                <XhFlex size="sm">
                  <XhButton size="sm" variant="subtle" @click="addRecord">
                    <span><Icon icon="tabler:plus" /></span>
                    {{ t('setting.print_template.sample_add_record') }}
                  </XhButton>
                  <XhButton size="sm" variant="subtle" @click="duplicateRecord">
                    <span><Icon icon="tabler:copy" /></span>
                    {{ t('setting.print_template.sample_duplicate_record') }}
                  </XhButton>
                  <XhButton size="sm" variant="subtle" tone="danger" :disabled="records.length <= 1" @click="deleteRecord">
                    <span><Icon icon="tabler:trash" /></span>
                    {{ t('setting.print_template.sample_delete_record') }}
                  </XhButton>
                </XhFlex>
              </div>
              <XhTabsRoot
                v-model:value="activeRecordId"
                variant="card"
                size="sm"
                class="sample-record-tabs"
              >
                <XhTabsList>
                  <XhTabsTrigger
                    v-for="(record, index) in records"
                    :key="record.id"
                    :value="record.id"
                  >
                    {{ t('setting.print_template.sample_record_index', { index: index + 1 }) }}
                  </XhTabsTrigger>
                </XhTabsList>
              </XhTabsRoot>
            </section>

            <XhEmptyStateRoot
              v-if="schema.fields.length === 0"
              class="sample-fields-empty"
            >
              <XhEmptyStateDescription>{{ t('setting.print_template.sample_no_bound_fields') }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
            <XhFormRoot
              v-else-if="currentRecord"
              validate-on="blur"
              class="sample-fields-grid"
            >
              <XhFieldRoot
                v-for="field in schema.fields"
                :key="field.key"
                :class="{ 'sample-field--table': field.kind === 'table' }"
              >
                <XhFieldLabel>{{ field.label }}</XhFieldLabel>
                <XhFieldControl>
                  <TableEditor
                    v-if="field.kind === 'table'"
                    :field="field"
                    :value="getPrintSampleValue(currentRecord.data, field.key)"
                    class="w-full"
                    @update:value="updateField(field, $event)"
                  />
                  <ValueEditor
                    v-else
                    :field="field"
                    :value="getPrintSampleValue(currentRecord.data, field.key)"
                    class="w-full"
                    @update:value="updateField(field, $event)"
                  />
                </XhFieldControl>
              </XhFieldRoot>
            </XhFormRoot>
          </template>
        </div>
      </div>

      <div class="xh-dialog-footer">
        <XhFlex justify="between" align="center">
          <span class="sample-data-footer-tip">
            {{ t('setting.print_template.sample_fields_count', { count: schema.fields.length }) }}
          </span>
          <XhFlex>
            <XhButton :disabled="submitting" @click="requestVisible(false)">
              {{ t('common.actions.cancel') }}
            </XhButton>
            <XhButton
              data-testid="print-sample-preview-submit"
              tone="brand"
              :loading="submitting"
              :disabled="!canPreview"
              @click="submitPreview"
            >
              <span><Icon icon="tabler:eye" /></span>
              {{ t('setting.print_template.sample_open_preview') }}
            </XhButton>
          </XhFlex>
        </XhFlex>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style>
.print-sample-data-modal {
  width: 960px !important;
  max-width: 94vw;
}

.print-sample-data-modal > .n-card-content {
  max-height: 68vh;
  overflow: auto;
}

.sample-data-body {
  display: flex;
  min-height: 220px;
  flex-direction: column;
  gap: 14px;
}

.sample-data-intro,
.sample-records-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.sample-data-intro p {
  margin: 4px 0 0;
  color: #64748b;
  font-size: 12px;
}

.sample-records {
  padding: 12px 12px 0;
  border: 1px solid rgba(148, 163, 184, 0.28);
  border-radius: 8px;
}

.sample-records-heading {
  margin-bottom: 10px;
  color: #475569;
  font-size: 12px;
}

.sample-record-tabs .n-tabs-pane-wrapper {
  display: none;
}

.sample-fields-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 18px;
}

.sample-field--table {
  grid-column: 1 / -1;
}

.sample-fields-empty {
  min-height: 220px;
}

.sample-data-footer-tip {
  color: #64748b;
  font-size: 12px;
}

.dark .sample-data-intro p,
.dark .sample-records-heading,
.dark .sample-data-footer-tip {
  color: #94a3b8;
}

@media (max-width: 720px) {
  .sample-fields-grid {
    grid-template-columns: 1fr;
  }

  .sample-field--table {
    grid-column: auto;
  }

  .sample-data-intro,
  .sample-records-heading {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
