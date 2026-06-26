<script setup lang="ts">
import type { UploadFileInfo } from 'naive-ui'
import type { ListFieldSchema } from './types'
import type { ImportSummary } from './useSchemaImport'
import {
  NAlert,
  NButton,
  NDataTable,
  NIcon,
  NModal,
  NProgress,
  NSpace,
  NTag,
  NUpload,
  NUploadDragger,
} from 'naive-ui'
import { computed, ref, watch } from 'vue'
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

const importHistoryApi = useAppContext().apis.importHistoryApi

const importer = useSchemaImport({
  fields: () => props.fields,
  fileName: () => props.pageCode,
  create: record => props.create(record),
})
const { phase, rows, fileErrors, validRows, errorRows, progress, summary } = importer

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

/** 选择文件即解析校验（阻止真实上传） */
function onBeforeUpload(data: { file: UploadFileInfo }): boolean {
  const file = data.file.file
  if (file) {
    void importer.loadFile(file)
  }
  return false
}

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

const errorColumns = [
  { key: 'row', title: '行号', width: 70 },
  { key: 'field', title: '字段', width: 120 },
  { key: 'message', title: '问题', ellipsis: { tooltip: true } },
]

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
  <NModal
    v-model:show="show"
    preset="card"
    title="导入数据"
    :auto-focus="false"
    :bordered="false"
    :mask-closable="phase !== 'importing'"
    :closable="phase !== 'importing'"
    style="width: 720px; max-width: calc(100vw - 32px)"
  >
    <NSpace vertical :size="12">
      <!-- 模板说明 + 下载 -->
      <NAlert type="info" :show-icon="true" :bordered="false">
        <div class="xh-import-tip">
          <span>请按模板表头填写 CSV 文件（首列以 # 开头的说明行会被自动忽略）。</span>
          <NButton size="tiny" quaternary type="primary" @click="importer.downloadTemplate">
            <template #icon>
              <NIcon><Icon icon="lucide:file-down" /></NIcon>
            </template>
            下载模板
          </NButton>
        </div>
      </NAlert>

      <!-- 选择文件（idle / ready 可重选） -->
      <NUpload
        v-if="phase === 'idle' || phase === 'ready'"
        accept=".csv,text/csv"
        :show-file-list="false"
        @before-upload="onBeforeUpload"
      >
        <NUploadDragger>
          <div class="xh-import-dragger">
            <NIcon :size="32" :depth="3">
              <Icon icon="lucide:upload" />
            </NIcon>
            <span>点击或拖拽 CSV 文件到此处</span>
          </div>
        </NUploadDragger>
      </NUpload>

      <!-- 文件级错误 -->
      <NAlert v-for="error in fileErrors" :key="error" type="error" :bordered="false">
        {{ error }}
      </NAlert>

      <!-- 解析结果汇总 -->
      <NSpace v-if="phase !== 'idle' && rows.length > 0" align="center" :size="8">
        <NTag size="small" :bordered="false">
          共 {{ rows.length }} 行
        </NTag>
        <NTag size="small" type="success" :bordered="false">
          可导入 {{ validRows.length }} 行
        </NTag>
        <NTag v-if="errorItems.length > 0" size="small" type="error" :bordered="false">
          {{ phase === 'done' ? '失败' : '校验失败' }} {{ errorRows.length }} 行
        </NTag>
      </NSpace>

      <!-- 错误明细 -->
      <NDataTable
        v-if="errorItems.length > 0"
        size="small"
        :columns="errorColumns"
        :data="errorItems"
        :max-height="220"
        :bordered="false"
      />

      <!-- 导入进度 -->
      <NProgress
        v-if="phase === 'importing'"
        type="line"
        :percentage="importPercent"
        indicator-placement="inside"
        processing
      />

      <!-- 完成汇总 -->
      <NAlert
        v-if="phase === 'done' && summary"
        :type="summary.failed === 0 ? 'success' : 'warning'"
        :bordered="false"
      >
        导入完成：成功 {{ summary.success }} 条，失败 {{ summary.failed }} 条。
        <template v-if="summary.failed > 0">
          可下载失败行修正后重新导入。
        </template>
      </NAlert>

      <!-- 最近导入（当前用户 × 当前页面） -->
      <div v-if="phase === 'idle' && recentImports.length > 0" class="xh-import-recent">
        <div class="xh-import-recent__title">
          最近导入
        </div>
        <div v-for="item in recentImports" :key="item.basicId" class="xh-import-recent__row">
          <span class="xh-import-recent__time">{{ formatDate(item.createdTime) }}</span>
          <span class="xh-import-recent__file" :title="item.fileName">{{ item.fileName }}</span>
          <NTag size="tiny" type="success" :bordered="false">
            成功 {{ item.successCount }}
          </NTag>
          <NTag v-if="item.failCount > 0" size="tiny" type="error" :bordered="false">
            失败 {{ item.failCount }}
          </NTag>
        </div>
      </div>
    </NSpace>

    <template #footer>
      <NSpace justify="end">
        <NButton
          v-if="errorRows.length > 0"
          size="small"
          @click="importer.downloadErrors"
        >
          <template #icon>
            <NIcon><Icon icon="lucide:file-x" /></NIcon>
          </template>
          下载失败行
        </NButton>
        <NButton size="small" :disabled="phase === 'importing'" @click="handleClose">
          {{ phase === 'done' ? '完成' : '取消' }}
        </NButton>
        <NButton
          v-if="phase !== 'done'"
          size="small"
          type="primary"
          :disabled="!canRun"
          :loading="phase === 'importing'"
          @click="handleRun"
        >
          开始导入
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>

<style scoped>
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
  color: var(--n-text-color-3, rgb(118 124 130));
}

.xh-import-recent__title {
  margin-bottom: 4px;
  font-size: 13px;
  font-weight: 600;
  color: var(--n-text-color);
}

.xh-import-recent__row {
  display: flex;
  gap: 8px;
  align-items: center;
  padding: 2px 0;
  font-size: 12px;
  color: var(--n-text-color-3, rgb(118 124 130));
}

.xh-import-recent__file {
  max-width: 280px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
