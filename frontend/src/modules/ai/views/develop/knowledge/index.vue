<script setup lang="ts">
import type {
  KnowledgeCitationDto,
  KnowledgeIngestDto,
  KnowledgeListItemDto,
} from '../../../api'
import type {
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhButton, XhCardBody, XhCardHeader, XhCardRoot, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormFieldGroup, XhFormRoot, XhSwitch, XhTabsContent, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
} from '@/api'
import { SchemaPage, XEditModal, XInput, XNumberInput } from '~/components'
import { dialog, toast } from '~/composables'
import { Icon } from '~/iconify'
import { getOptionLabel } from '~/utils'
import {
  KNOWLEDGE_INDEX_STATUS_OPTIONS,
  KNOWLEDGE_SOURCE_TYPE_OPTIONS,
  knowledgeApi,
  KnowledgeIndexStatus,
  KnowledgeSourceType,
} from '../../../api'

defineOptions({ name: 'DevelopKnowledgePage' })

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

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
  return status === KnowledgeIndexStatus.Failed ? 'danger' : 'warning'
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
      return h(XhTagRoot, { variant: 'outline', tone: statusTagType(r.status) }, () => h(XhTagLabel, () => getOptionLabel(KNOWLEDGE_INDEX_STATUS_OPTIONS, r.status)))
    },
  },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', minWidth: 170, sortable: true, order: 6 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.ai.knowledge',
  pageName: t('develop.knowledge.tabs.documents'),
  rowKey: 'basicId',
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
  void dialog.confirm({
    badge: 'info',
    title: t('develop.knowledge.action_reindex'),
    content: t('develop.knowledge.confirm_reindex'),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await knowledgeApi.reindex(row.basicId)
        toast.success(t('develop.knowledge.reindex_success'))
        reload()
      }
      catch (error) {
        toast.error((error as Error)?.message || t('develop.knowledge.reindex_failed'))
      }
    },
  })
}

function handleDelete(row: KnowledgeListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('common.actions.delete'),
    content: t('develop.knowledge.confirm_delete'),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await knowledgeApi.delete(row.basicId)
        toast.success(t('common.messages.delete_success'))
        reload()
      }
      catch (error) {
        toast.error((error as Error)?.message || t('common.messages.delete_failed'))
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
  reader.onerror = () => toast.error(t('develop.knowledge.file_read_failed'))
  reader.readAsText(file)
  // 允许再次选同一文件
  target.value = ''
}

async function handleSubmit() {
  if (!form.value.title.trim()) {
    toast.warning(t('develop.knowledge.validate_title'))
    return
  }
  if (!form.value.text.trim()) {
    toast.warning(t('develop.knowledge.validate_text'))
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
      toast.error(t('develop.knowledge.ingest_index_failed', { msg: result.errorMessage || '' }))
    }
    else {
      toast.success(t('develop.knowledge.ingest_success', { count: result.chunkCount }))
    }
    modalVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.knowledge.ingest_failed'))
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
    toast.warning(t('develop.knowledge.validate_query'))
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
  catch (error) {
    toast.error((error as Error)?.message || t('develop.knowledge.query_failed'))
  }
  finally {
    queryLoading.value = false
  }
}
</script>

<template>
  <div class="knowledge">
    <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
    <XhTabsRoot v-model:value="activeTab" variant="line" class="knowledge__tabs">
      <XhTabsList>
        <XhTabsTrigger value="documents">
          {{ t('develop.knowledge.tabs.documents') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="playground">
          {{ t('develop.knowledge.tabs.playground') }}
        </XhTabsTrigger>
      </XhTabsList>
      <XhTabsContent value="documents">
        <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
          <XEditModal
            v-model:show="modalVisible"
            :title="t('develop.knowledge.modal_add_title')"
            :loading="submitLoading"
            :save-text="t('develop.knowledge.ingest')"
            :form-id="editFormId"
          >
            <XhFormRoot
              :id="editFormId"
              v-model:values="form"
              validate-on="blur"
              class="xh-edit-form-grid"
              @submit="handleSubmit"
            >
              <XhFormFieldGroup value="title">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('develop.knowledge.form_title') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XInput v-model:value="form.title" clearable :placeholder="t('develop.knowledge.form_title_placeholder')" />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
              <XhFormFieldGroup value="embeddingProviderCode">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('develop.knowledge.form_provider') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XInput v-model:value="form.embeddingProviderCode" clearable :placeholder="t('develop.knowledge.form_provider_placeholder')" />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
              <XhFormFieldGroup value="text" class="xh-span-2">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('develop.knowledge.form_text') }}</XhFieldLabel>
                  <div class="knowledge__text">
                    <XhFlex class="knowledge__text-bar" justify="between">
                      <XhButton size="sm" @click="triggerFilePicker">
                        {{ t('develop.knowledge.form_pick_file') }}
                      </XhButton>
                      <span v-if="form.source" class="knowledge__source">{{ form.source }}</span>
                    </XhFlex>
                    <XhFieldControl>
                      <XInput
                        v-model:value="form.text"
                        :placeholder="t('develop.knowledge.form_text_placeholder')"
                        :rows="12"
                        type="textarea"
                      />
                    </XhFieldControl>
                    <input ref="fileInput" accept=".txt,.md,.markdown,.cs,.ts,.vue,.js,.json,.py,.java,.go,.sql,.yml,.yaml,.html,.css" style="display: none" type="file" @change="onFileSelected">
                  </div>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
              <XhFormFieldGroup value="remark" class="xh-span-2">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('common.fields.remark') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XInput v-model:value="form.remark" clearable :rows="2" type="textarea" />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
            </XhFormRoot>
          </XEditModal>
        </SchemaPage>
      </XhTabsContent>
      <XhTabsContent value="playground">
        <div class="playground">
          <XhCardRoot variant="ghost">
            <XhCardBody>
              <XhFormRoot validate-on="blur">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('develop.knowledge.query_label') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XInput
                      v-model:value="queryText"
                      :placeholder="t('develop.knowledge.query_placeholder')"
                      :rows="3"
                      type="textarea"
                      @keydown.enter.exact.prevent="handleQuery"
                    />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
                <XhFlex align="center" gap="md" :wrap="true">
                  <XhFieldRoot>
                    <XhFieldLabel>{{ t('develop.knowledge.query_topk') }}</XhFieldLabel>
                    <XhFieldControl>
                      <XNumberInput v-model:value="queryTopK" :max="20" :min="1" style="width: 120px" />
                    </XhFieldControl>
                    <XhFieldErrorText />
                  </XhFieldRoot>
                  <XhFieldRoot>
                    <XhFieldLabel>{{ t('develop.knowledge.query_provider') }}</XhFieldLabel>
                    <XhFieldControl>
                      <XInput v-model:value="queryProvider" clearable :placeholder="t('develop.knowledge.query_provider_placeholder')" style="width: 200px" />
                    </XhFieldControl>
                    <XhFieldErrorText />
                  </XhFieldRoot>
                  <XhFieldRoot>
                    <XhFieldLabel>{{ t('develop.knowledge.query_answer') }}</XhFieldLabel>
                    <XhFieldControl>
                      <XhSwitch v-model:checked="queryAnswer" />
                    </XhFieldControl>
                    <XhFieldErrorText />
                  </XhFieldRoot>
                  <XhButton :loading="queryLoading" tone="brand" @click="handleQuery">
                    {{ t('develop.knowledge.query_submit') }}
                  </XhButton>
                </XhFlex>
              </XhFormRoot>
            </XhCardBody>
          </XhCardRoot>

          <XhCardRoot v-if="answerText" variant="ghost" class="playground__answer">
            <XhCardBody>
              <div class="playground__answer-text">
                {{ answerText }}
              </div>
            </XhCardBody>
          </XhCardRoot>

          <div v-if="citations.length > 0" class="playground__citations">
            <div class="playground__citations-title">
              {{ t('develop.knowledge.citations_title', { count: citations.length }) }}
            </div>
            <XhCardRoot v-for="(citation, idx) in citations" :key="`${citation.documentId}-${citation.index}`" variant="outline" class="playground__citation">
              <XhCardHeader>
                <XhFlex align="center" gap="sm">
                  <XhTagRoot variant="subtle" size="sm" tone="info">
                    <XhTagLabel>
                      [{{ idx + 1 }}]
                    </XhTagLabel>
                  </XhTagRoot>
                  <span class="playground__citation-title">{{ citation.title || citation.source || citation.documentId }}</span>
                  <XhTagRoot v-if="citation.score != null" variant="subtle" size="sm">
                    <XhTagLabel>
                      {{ citation.score.toFixed(3) }}
                    </XhTagLabel>
                  </XhTagRoot>
                </XhFlex>
              </XhCardHeader>
              <XhCardBody>
                <div class="playground__citation-text">
                  {{ citation.text }}
                </div>
              </XhCardBody>
            </XhCardRoot>
          </div>

          <XhEmptyStateRoot v-if="hasQueried && citations.length === 0" class="playground__empty">
            <XhEmptyStateIcon>
              <Icon icon="lucide:search-x" width="28" />
            </XhEmptyStateIcon>
            <XhEmptyStateTitle>{{ t('develop.knowledge.no_result_title') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('develop.knowledge.no_result') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
        </div>
      </XhTabsContent>
    </XhTabsRoot>
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

.knowledge__tabs :deep([data-scope='tabs'][data-part='content']) {
  flex: 1;
  min-height: 0;
}

.knowledge__tabs :deep([data-scope='tabs'][data-part='content']) {
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
