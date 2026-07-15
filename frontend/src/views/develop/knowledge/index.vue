<script setup lang="ts">
import type {
  KnowledgeCitationDto,
  KnowledgeIngestDto,
  KnowledgeListItemDto,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NCard,
  NEmpty,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSpace,
  NSwitch,
  NTabPane,
  NTabs,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  KNOWLEDGE_INDEX_STATUS_OPTIONS,
  KNOWLEDGE_SOURCE_TYPE_OPTIONS,
  knowledgeApi,
  KnowledgeIndexStatus,
  KnowledgeSourceType,
  querySortsFromSchema,
} from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'DevelopKnowledgePage' })

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()

const activeTab = ref<'documents' | 'playground'>('documents')

// ── 文档列表 ─────────────────────────────────────────────────────
const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

function statusTagType(status: KnowledgeIndexStatus) {
  if (status === KnowledgeIndexStatus.Indexed) {
    return 'success'
  }
  return status === KnowledgeIndexStatus.Failed ? 'error' : 'warning'
}

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('develop.knowledge.col_title'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('develop.knowledge.search_placeholder'), order: 0 },
  { key: 'title', title: t('develop.knowledge.col_title'), dataType: 'string', minWidth: 200, fixed: 'left', sortable: true, order: 1 },
  {
    key: 'sourceType',
    title: t('develop.knowledge.col_source_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: KNOWLEDGE_SOURCE_TYPE_OPTIONS,
    searchPlaceholder: t('develop.knowledge.col_source_type'),
    width: 110,
    order: 2,
    render: row => getOptionLabel(KNOWLEDGE_SOURCE_TYPE_OPTIONS, (row as unknown as KnowledgeListItemDto).sourceType),
  },
  { key: 'source', title: t('develop.knowledge.col_source'), dataType: 'string', minWidth: 140, order: 3 },
  { key: 'chunkCount', title: t('develop.knowledge.col_chunk_count'), dataType: 'number', width: 90, sortable: true, order: 4 },
  {
    key: 'status',
    title: t('develop.knowledge.col_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: KNOWLEDGE_INDEX_STATUS_OPTIONS,
    searchPlaceholder: t('develop.knowledge.col_status'),
    width: 100,
    order: 5,
    render: (row) => {
      const r = row as unknown as KnowledgeListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: statusTagType(r.status) }, () => getOptionLabel(KNOWLEDGE_INDEX_STATUS_OPTIONS, r.status))
    },
  },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', minWidth: 170, sortable: true, order: 6 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.ai.knowledge',
  pageName: t('develop.knowledge.tabs.documents'),
  rowKey: 'basicId',
  scrollX: 1000,
  batchRemovable: true,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return knowledgeApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => knowledgeApi.delete(id),
  },
  actions: [
    { key: 'create', title: t('develop.knowledge.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'reindex', title: t('develop.knowledge.action_reindex'), scope: 'row', type: 'info', icon: 'lucide:refresh-cw' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as KnowledgeListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'reindex':
      if (row) {
        handleReindex(row)
      }
      break
    case 'delete':
      if (row) {
        handleDelete(row)
      }
      break
  }
}

function handleReindex(row: KnowledgeListItemDto) {
  dialog.info({
    title: t('develop.knowledge.action_reindex'),
    content: t('develop.knowledge.confirm_reindex'),
    positiveText: t('common.actions.confirm'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await knowledgeApi.reindex(row.basicId)
        message.success(t('develop.knowledge.reindex_success'))
        reload()
      }
      catch {
        message.error(t('develop.knowledge.reindex_failed'))
      }
    },
  })
}

function handleDelete(row: KnowledgeListItemDto) {
  dialog.warning({
    title: t('common.actions.delete'),
    content: t('develop.knowledge.confirm_delete'),
    positiveText: t('common.actions.confirm'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await knowledgeApi.delete(row.basicId)
        message.success(t('common.messages.delete_success'))
        reload()
      }
      catch {
        message.error(t('common.messages.delete_failed'))
      }
    },
  })
}

// ── 摄取弹窗 ─────────────────────────────────────────────────────
interface IngestFormModel {
  title: string
  sourceType: KnowledgeSourceType
  source?: string | null
  text: string
  embeddingProviderCode?: string | null
  remark?: string | null
}

const modalVisible = ref(false)
const submitLoading = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)
const form = ref<IngestFormModel>(createDefaultForm())

function createDefaultForm(): IngestFormModel {
  return {
    title: '',
    sourceType: KnowledgeSourceType.PasteText,
    source: null,
    text: '',
    embeddingProviderCode: null,
    remark: null,
  }
}

function handleAdd() {
  form.value = createDefaultForm()
  modalVisible.value = true
}

function triggerFilePicker() {
  fileInput.value?.click()
}

function onFileSelected(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) {
    return
  }
  const reader = new FileReader()
  reader.onload = () => {
    form.value.text = String(reader.result ?? '')
    form.value.source = file.name
    form.value.sourceType = KnowledgeSourceType.UploadFile
    if (!form.value.title.trim()) {
      form.value.title = file.name
    }
  }
  reader.onerror = () => message.error(t('develop.knowledge.file_read_failed'))
  reader.readAsText(file)
  // 允许再次选同一文件
  target.value = ''
}

async function handleSubmit() {
  if (!form.value.title.trim()) {
    message.warning(t('develop.knowledge.validate_title'))
    return
  }
  if (!form.value.text.trim()) {
    message.warning(t('develop.knowledge.validate_text'))
    return
  }
  submitLoading.value = true
  try {
    const input: KnowledgeIngestDto = {
      title: form.value.title.trim(),
      sourceType: form.value.sourceType,
      source: form.value.source?.trim() || null,
      text: form.value.text,
      embeddingProviderCode: form.value.embeddingProviderCode?.trim() || null,
      remark: form.value.remark,
    }
    const result = await knowledgeApi.ingest(input)
    if (result.status === KnowledgeIndexStatus.Failed) {
      message.error(t('develop.knowledge.ingest_index_failed', { msg: result.errorMessage || '' }))
    }
    else {
      message.success(t('develop.knowledge.ingest_success', { count: result.chunkCount }))
    }
    modalVisible.value = false
    reload()
  }
  catch {
    message.error(t('develop.knowledge.ingest_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

// ── 检索试玩 ─────────────────────────────────────────────────────
const queryText = ref('')
const queryTopK = ref<number>(5)
const queryProvider = ref('')
const queryAnswer = ref(true)
const queryLoading = ref(false)
const answerText = ref<string | null>(null)
const citations = ref<KnowledgeCitationDto[]>([])
const hasQueried = ref(false)

async function handleQuery() {
  if (!queryText.value.trim()) {
    message.warning(t('develop.knowledge.validate_query'))
    return
  }
  queryLoading.value = true
  try {
    const result = await knowledgeApi.query({
      query: queryText.value.trim(),
      topK: queryTopK.value,
      provider: queryProvider.value.trim() || null,
      answer: queryAnswer.value,
    })
    answerText.value = result.answer ?? null
    citations.value = result.citations ?? []
    hasQueried.value = true
  }
  catch {
    message.error(t('develop.knowledge.query_failed'))
  }
  finally {
    queryLoading.value = false
  }
}
</script>

<template>
  <div class="knowledge">
    <NTabs v-model:value="activeTab" animated class="knowledge__tabs" type="line">
      <NTabPane name="documents" :tab="t('develop.knowledge.tabs.documents')">
        <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
          <NModal
            v-model:show="modalVisible"
            :auto-focus="false"
            :bordered="false"
            :title="t('develop.knowledge.modal_add_title')"
            preset="card"
            style="width: 760px; max-width: 92vw"
          >
            <NForm :model="form" class="xh-edit-form-grid" label-placement="top">
              <NFormItem :label="t('develop.knowledge.form_title')" path="title">
                <NInput v-model:value="form.title" clearable :placeholder="t('develop.knowledge.form_title_placeholder')" />
              </NFormItem>
              <NFormItem :label="t('develop.knowledge.form_provider')" path="embeddingProviderCode">
                <NInput v-model:value="form.embeddingProviderCode" clearable :placeholder="t('develop.knowledge.form_provider_placeholder')" />
              </NFormItem>
              <NFormItem class="xh-form-full" :label="t('develop.knowledge.form_text')" path="text">
                <div class="knowledge__text">
                  <NSpace class="knowledge__text-bar" justify="space-between">
                    <NButton size="small" @click="triggerFilePicker">
                      {{ t('develop.knowledge.form_pick_file') }}
                    </NButton>
                    <span v-if="form.source" class="knowledge__source">{{ form.source }}</span>
                  </NSpace>
                  <NInput
                    v-model:value="form.text"
                    :placeholder="t('develop.knowledge.form_text_placeholder')"
                    :rows="12"
                    type="textarea"
                  />
                  <input ref="fileInput" accept=".txt,.md,.markdown,.cs,.ts,.vue,.js,.json,.py,.java,.go,.sql,.yml,.yaml,.html,.css" style="display: none" type="file" @change="onFileSelected">
                </div>
              </NFormItem>
              <NFormItem class="xh-form-full" :label="t('common.fields.remark')" path="remark">
                <NInput v-model:value="form.remark" clearable :rows="2" type="textarea" />
              </NFormItem>
            </NForm>

            <template #footer>
              <NSpace justify="end">
                <NButton @click="modalVisible = false">
                  {{ t('common.actions.cancel') }}
                </NButton>
                <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
                  {{ t('develop.knowledge.ingest') }}
                </NButton>
              </NSpace>
            </template>
          </NModal>
        </SchemaPage>
      </NTabPane>

      <NTabPane name="playground" :tab="t('develop.knowledge.tabs.playground')">
        <div class="playground">
          <NCard :bordered="false" size="small">
            <NForm :model="{}" label-placement="top">
              <NFormItem :label="t('develop.knowledge.query_label')">
                <NInput
                  v-model:value="queryText"
                  :placeholder="t('develop.knowledge.query_placeholder')"
                  :rows="3"
                  type="textarea"
                  @keydown.enter.exact.prevent="handleQuery"
                />
              </NFormItem>
              <NSpace align="center" :wrap="true">
                <NFormItem :label="t('develop.knowledge.query_topk')" label-placement="left">
                  <NInputNumber v-model:value="queryTopK" :max="20" :min="1" style="width: 120px" />
                </NFormItem>
                <NFormItem :label="t('develop.knowledge.query_provider')" label-placement="left">
                  <NInput v-model:value="queryProvider" clearable :placeholder="t('develop.knowledge.query_provider_placeholder')" style="width: 200px" />
                </NFormItem>
                <NFormItem :label="t('develop.knowledge.query_answer')" label-placement="left">
                  <NSwitch v-model:value="queryAnswer" />
                </NFormItem>
                <NButton :loading="queryLoading" type="primary" @click="handleQuery">
                  {{ t('develop.knowledge.query_submit') }}
                </NButton>
              </NSpace>
            </NForm>
          </NCard>

          <NCard v-if="answerText" class="playground__answer" :bordered="false" size="small" :title="t('develop.knowledge.answer_title')">
            <div class="playground__answer-text">
              {{ answerText }}
            </div>
          </NCard>

          <div v-if="citations.length > 0" class="playground__citations">
            <div class="playground__citations-title">
              {{ t('develop.knowledge.citations_title', { count: citations.length }) }}
            </div>
            <NCard v-for="(citation, idx) in citations" :key="`${citation.documentId}-${citation.index}`" class="playground__citation" :bordered="true" size="small">
              <template #header>
                <NSpace align="center" :size="8">
                  <NTag :bordered="false" round size="small" type="info">
                    [{{ idx + 1 }}]
                  </NTag>
                  <span class="playground__citation-title">{{ citation.title || citation.source || citation.documentId }}</span>
                  <NTag v-if="citation.score != null" :bordered="false" round size="tiny">
                    {{ citation.score.toFixed(3) }}
                  </NTag>
                </NSpace>
              </template>
              <div class="playground__citation-text">
                {{ citation.text }}
              </div>
            </NCard>
          </div>

          <NEmpty v-if="hasQueried && citations.length === 0" class="playground__empty" :description="t('develop.knowledge.no_result')" />
        </div>
      </NTabPane>
    </NTabs>
  </div>
</template>

<style scoped>
.knowledge {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 12px;
  box-sizing: border-box;
  overflow: hidden;
}

.knowledge__tabs {
  flex: 1;
  min-height: 0;
}

.knowledge__tabs :deep(.n-tabs-pane-wrapper) {
  flex: 1;
  min-height: 0;
}

.knowledge__tabs :deep(.n-tab-pane) {
  height: 100%;
  padding-top: 8px;
  box-sizing: border-box;
}

.knowledge__text {
  width: 100%;
}

.knowledge__text-bar {
  margin-bottom: 6px;
}

.knowledge__source {
  font-size: 12px;
  color: var(--text-color-3, #999);
  align-self: center;
}

.playground {
  height: 100%;
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.playground__answer-text {
  white-space: pre-wrap;
  line-height: 1.7;
}

.playground__citations-title {
  font-weight: 600;
  margin-bottom: 8px;
}

.playground__citation {
  margin-bottom: 10px;
}

.playground__citation-title {
  font-weight: 500;
}

.playground__citation-text {
  white-space: pre-wrap;
  font-size: 13px;
  line-height: 1.6;
  color: var(--text-color-2, #666);
}

.playground__empty {
  margin-top: 32px;
}
</style>
