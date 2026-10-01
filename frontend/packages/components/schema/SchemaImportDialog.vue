<script setup lang="ts">
import type { FileUploadFileRejectDetails, TableColumnDef } from '@xihan-ui/headless'
import type { ListFieldSchema } from './types'
import type { ImportSummary } from './useSchemaImport'
import {
  XhAlertContent,
  XhAlertDescription,
  XhAlertIndicator,
  XhAlertRoot,
  XhButton,
  XhButtonIndicator,
  XhButtonLabel,
  XhDialogCloseTrigger,
  XhDialogContent,
  XhDialogRoot,
  XhDialogTitle,
  XhFileUploadDropzone,
  XhFileUploadHiddenInput,
  XhFileUploadRoot,
  XhProgress,
  XhTableBody,
  XhTableCell,
  XhTableColumnHeader,
  XhTableColumnLabel,
  XhTableHeader,
  XhTableRoot,
  XhTableRow,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'
import { useSchemaImport } from './useSchemaImport'

defineOptions({ name: 'SchemaImportDialog' })

const props = defineProps<{
  /** 导入字段（已按权限过滤、字典选项已注入） */
  fields: ListFieldSchema[]
  /** 页面码（模板/失败行文件名前缀 + 留痕维度） */
  pageCode: string
  /** 后端资源码（留痕用，可空） */
  resourceCode?: string
  /** 创建单条（来自 resource.create） */
  create: (record: Record<string, unknown>) => Promise<unknown>
}>()

const emit = defineEmits<{
  /** 导入执行完毕（无论成败），页面可据此刷新/留痕 */
  finished: [summary: ImportSummary]
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const importHistoryApi = useAppContext().apis.importHistoryApi

const importer = useSchemaImport({
  fields: () => props.fields,
  fileName: () => props.pageCode,
  create: record => props.create(record),
})
const { phase, rows, fileErrors, validRows, errorRows, progress, summary } = importer

/** 选择时被组件拒收的文件（类型不对、一次拖了多份）：与文件级错误并排列出 */
const rejectErrors = ref<string[]>([])

/** 最近导入记录（当前用户 × 当前页面，端点未就绪时静默为空） */
type RecentImport = Awaited<ReturnType<typeof importHistoryApi.recent>>[number]
const recentImports = ref<RecentImport[]>([])

async function loadRecent(): Promise<void> {
  try {
    recentImports.value = await importHistoryApi.recent(props.pageCode, 5)
  }
  catch {
    recentImports.value = []
  }
}

// 每次打开重置到初始态并拉取最近导入
watch(show, (value) => {
  if (value) {
    importer.reset()
    rejectErrors.value = []
    void loadRecent()
  }
})

/** 导入留痕上报（尽力而为，失败静默不影响导入结果） */
function reportHistory(result: ImportSummary): void {
  const errors = errorRows.value
    .flatMap(row => row.errors)
    .slice(0, 50)
    .map(error => ({ row: error.row, field: error.field ?? null, message: error.message }))
  void importHistoryApi
    .create({
      pageCode: props.pageCode,
      resourceCode: props.resourceCode ?? null,
      fileName: importer.sourceFileName.value || `${props.pageCode}.csv`,
      totalCount: result.total,
      successCount: result.success,
      failCount: result.failed,
      errorSummary: errors.length > 0 ? JSON.stringify(errors) : null,
    })
    .then(() => loadRecent())
    .catch(() => undefined)
}

/**
 * 已选文件受控为空：组件只当选择器用，每次选中的文件交给导入器解析后不留在组件里。
 * 否则选过一份之后 maxFiles=1 已满，重选的文件会被当成「数量超限」拒收
 */
const NO_FILES: File[] = []

/** 选择文件即解析校验；不给 upload 回调即不会发起任何真实上传 */
function onFileAccept(details: { files: File[] }): void {
  const file = details.files[0]
  if (file) {
    rejectErrors.value = []
    void importer.loadFile(file)
  }
}

function onFileReject(details: FileUploadFileRejectDetails): void {
  rejectErrors.value = details.files.map(({ file, reasons }) =>
    reasons.includes('too-many-files')
      ? t('component.schema_import.err_reject_count', { name: file.name })
      : t('component.schema_import.err_reject_type', { name: file.name }),
  )
}

/** 选择被拒的理由在前，已载入文件的问题在后 */
const fileProblems = computed(() => [...rejectErrors.value, ...fileErrors.value])

/** 校验/创建错误平铺为表格行 */
const errorItems = computed(() =>
  errorRows.value.flatMap(row =>
    row.errors.map((error, index) => ({
      key: `${row.row}-${index}`,
      row: row.row,
      field: error.field ?? '-',
      message: error.message,
    })),
  ),
)

const errorColumns = computed<TableColumnDef[]>(() => [
  { id: 'row', label: t('component.schema_import.col_row'), width: 70 },
  { id: 'field', label: t('component.schema_import.col_field'), width: 120 },
  { id: 'message', label: t('component.schema_import.col_problem') },
])

/** 行号与行序的事实源；这张表不排序不选中，只报身份 */
const errorRowDefs = computed(() => errorItems.value.map(item => ({ id: item.key })))

const importPercent = computed(() =>
  validRows.value.length === 0 ? 0 : Math.round((progress.value / validRows.value.length) * 100),
)

const canRun = computed(() => phase.value === 'ready' && fileErrors.value.length === 0 && validRows.value.length > 0)

async function handleRun(): Promise<void> {
  if (!canRun.value) {
    return
  }
  const result = await importer.run()
  reportHistory(result)
  emit('finished', result)
}

function handleClose(): void {
  show.value = false
}
</script>

<template>
  <XhDialogRoot
    :open="show"
    :close-on-escape="phase !== 'importing'"
    :close-on-interact-outside="phase !== 'importing'"
    @update:open="(value: boolean) => (show = value)"
  >
    <XhDialogContent class="xh-import-modal" style="--xh-dialog-max-w: 720px">
      <XhDialogTitle>{{ t('component.schema_import.title') }}</XhDialogTitle>
      <XhDialogCloseTrigger v-if="phase !== 'importing'" />

      <div class="xh-import-body">
        <!-- 模板说明 + 下载 -->
        <XhAlertRoot tone="info">
          <XhAlertIndicator>
            <Icon icon="lucide:info" width="16" height="16" />
          </XhAlertIndicator>
          <XhAlertContent>
            <XhAlertDescription>
              <div class="xh-import-tip">
                <span>{{ t('component.schema_import.tip') }}</span>
                <XhButton size="sm" variant="ghost" @click="importer.downloadTemplate">
                  <Icon icon="lucide:file-down" />
                  {{ t('component.schema_import.download_template') }}
                </XhButton>
              </div>
            </XhAlertDescription>
          </XhAlertContent>
        </XhAlertRoot>

        <!-- 选择文件（idle / ready 可重选） -->
        <XhFileUploadRoot
          v-if="phase === 'idle' || phase === 'ready'"
          accept=".csv,text/csv"
          :max-files="1"
          :files="NO_FILES"
          @file-accept="onFileAccept"
          @file-reject="onFileReject"
        >
          <XhFileUploadDropzone>
            <div class="xh-import-dragger">
              <Icon icon="lucide:upload" class="xh-import-dragger__icon" />
              <span>{{ t('component.schema_import.dragger') }}</span>
            </div>
          </XhFileUploadDropzone>
          <XhFileUploadHiddenInput />
        </XhFileUploadRoot>

        <!-- 文件级错误：一条警示装下全部，多条时列成清单（每条一个 role=alert 会让读屏连播好几遍） -->
        <XhAlertRoot v-if="fileProblems.length > 0" tone="danger">
          <XhAlertIndicator>
            <Icon icon="lucide:circle-alert" width="16" height="16" />
          </XhAlertIndicator>
          <XhAlertContent>
            <XhAlertDescription>
              <template v-if="fileProblems.length === 1">
                {{ fileProblems[0] }}
              </template>
              <ul v-else class="xh-import-problems">
                <li v-for="problem in fileProblems" :key="problem">
                  {{ problem }}
                </li>
              </ul>
            </XhAlertDescription>
          </XhAlertContent>
        </XhAlertRoot>

        <!-- 解析结果汇总 -->
        <div v-if="phase !== 'idle' && rows.length > 0" class="xh-import-summary">
          <XhTagRoot variant="subtle" size="sm" tone="neutral">
            <XhTagLabel>
              {{ t('component.schema_import.total_rows', { count: rows.length }) }}
            </XhTagLabel>
          </XhTagRoot>
          <XhTagRoot variant="subtle" size="sm" tone="success">
            <XhTagLabel>
              {{ t('component.schema_import.valid_rows', { count: validRows.length }) }}
            </XhTagLabel>
          </XhTagRoot>
          <XhTagRoot v-if="errorItems.length > 0" variant="subtle" size="sm" tone="danger">
            <XhTagLabel>
              {{ phase === 'done' ? t('component.schema_import.failed_rows', { count: errorRows.length }) : t('component.schema_import.validation_failed_rows', { count: errorRows.length }) }}
            </XhTagLabel>
          </XhTagRoot>
        </div>

        <!-- 错误明细：限高的类名放在外层 div 上。表格根渲染成片段，带不上本组件的 scoped 标记，
             类名写在它身上样式永远命中不了；限高槽是继承的自定义属性，外层给即可 -->
        <div v-if="errorItems.length > 0" class="xh-import-errors">
          <XhTableRoot
            ruled
            size="md"
            sticky-header
            :columns="errorColumns"
            :rows="errorRowDefs"
          >
            <XhTableHeader>
              <XhTableRow>
                <XhTableColumnHeader v-for="col in errorColumns" :key="col.id" :value="col.id">
                  <XhTableColumnLabel>{{ col.label }}</XhTableColumnLabel>
                </XhTableColumnHeader>
              </XhTableRow>
            </XhTableHeader>
            <XhTableBody>
              <XhTableRow v-for="item in errorItems" :key="item.key" :value="item.key">
                <XhTableCell value="row">
                  {{ item.row }}
                </XhTableCell>
                <XhTableCell value="field">
                  {{ item.field }}
                </XhTableCell>
                <XhTableCell value="message" :title="item.message">
                  {{ item.message }}
                </XhTableCell>
              </XhTableRow>
            </XhTableBody>
          </XhTableRoot>
        </div>

        <!-- 导入进度 -->
        <XhProgress v-if="phase === 'importing'" :value="importPercent" :aria-label="t('component.schema_import.title')" />

        <!-- 完成汇总 -->
        <XhAlertRoot
          v-if="phase === 'done' && summary"
          :tone="summary.failed === 0 ? 'success' : 'warning'"
        >
          <XhAlertIndicator>
            <Icon :icon="summary.failed === 0 ? 'lucide:circle-check' : 'lucide:triangle-alert'" width="16" height="16" />
          </XhAlertIndicator>
          <XhAlertContent>
            <XhAlertDescription>
              {{ t('component.schema_import.import_done', { success: summary.success, failed: summary.failed }) }}
              <template v-if="summary.failed > 0">
                {{ t('component.schema_import.redownload_hint') }}
              </template>
            </XhAlertDescription>
          </XhAlertContent>
        </XhAlertRoot>

        <!-- 最近导入（当前用户 × 当前页面） -->
        <div v-if="phase === 'idle' && recentImports.length > 0" class="xh-import-recent">
          <div class="xh-import-recent__title">
            {{ t('component.schema_import.recent_title') }}
          </div>
          <div v-for="item in recentImports" :key="item.basicId" class="xh-import-recent__row">
            <span class="xh-import-recent__time">{{ formatDate(item.createdTime) }}</span>
            <span class="xh-import-recent__file" :title="item.fileName">{{ item.fileName }}</span>
            <XhTagRoot variant="subtle" size="sm" tone="success">
              <XhTagLabel>
                {{ t('component.schema_import.recent_success', { count: item.successCount }) }}
              </XhTagLabel>
            </XhTagRoot>
            <XhTagRoot v-if="item.failCount > 0" variant="subtle" size="sm" tone="danger">
              <XhTagLabel>
                {{ t('component.schema_import.recent_failed', { count: item.failCount }) }}
              </XhTagLabel>
            </XhTagRoot>
          </div>
        </div>
      </div>

      <div class="xh-import-footer">
        <XhButton
          v-if="errorRows.length > 0"
          size="sm"
          variant="outline"
          @click="importer.downloadErrors"
        >
          <Icon icon="lucide:file-x" />
          {{ t('component.schema_import.download_errors') }}
        </XhButton>
        <XhButton size="sm" variant="outline" :disabled="phase === 'importing'" @click="handleClose">
          {{ phase === 'done' ? t('component.schema_import.done_btn') : t('common.actions.cancel') }}
        </XhButton>
        <XhButton
          v-if="phase !== 'done'"
          size="sm"
          variant="solid"
          :disabled="!canRun"
          :loading="phase === 'importing'"
          @click="handleRun"
        >
          <XhButtonIndicator />
          <XhButtonLabel>{{ t('component.schema_import.start_import') }}</XhButtonLabel>
        </XhButton>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 内容纵向堆叠，替掉原先的 NSpace vertical */
.xh-import-body {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.xh-import-summary {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

/* 错误明细表：限高，超出内部滚动（槽由表格根继承） */
.xh-import-errors {
  --xh-table-max-h: var(--xh-viewport-h-md);
}

.xh-import-problems {
  margin: 0;
  padding-inline-start: var(--xh-space-4);
  list-style: disc;
}

.xh-import-dragger__icon {
  font-size: 32px;
}

/* 底部按钮行右对齐，替掉原先的 NSpace justify=end */
.xh-import-footer {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  margin-block-start: 12px;
}

.xh-import-tip {
  display: flex;
  gap: 8px;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
}

.xh-import-dragger {
  display: flex;
  flex-direction: column;
  gap: 8px;
  align-items: center;
  padding: 12px 0;
  font-size: 13px;
  color: var(--xh-fg-muted);
}

.xh-import-recent__title {
  margin-bottom: 4px;
  font-size: 13px;
  font-weight: 600;
  color: var(--xh-fg-default);
}

.xh-import-recent__row {
  display: flex;
  gap: 8px;
  align-items: center;
  padding: 2px 0;
  font-size: 12px;
  color: var(--xh-fg-muted);
}

.xh-import-recent__file {
  max-width: 280px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
