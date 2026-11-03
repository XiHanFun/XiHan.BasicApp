<!--
  AI / 知识库（照 4.x develop/knowledge）：文档与检索试玩两个页签
  接口取自后端契约（knowledge-documents.*）；平台与租户各自维护，只看调用方一侧。没有编辑，只有摄取、重建索引与删除
-->
<script setup lang="ts">
import type { KnowledgeIndexStatus, KnowledgeRow, KnowledgeSourceType } from './use-knowledge-documents'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { isComposingEvent } from '@xihan-ui/core'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhCardContent, XhCardHeader, XhCardRoot, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormRoot, XhSwitch, XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage, XEditModal, XInput, XNumberInput } from '~/components'
import { toast } from '~/composables'
import { knowledgeDocuments } from '~/contracts'
import { Icon } from '~/iconify'
import { citationTitle, INGEST_FILE_ACCEPT, knowledgeDocumentsResource, useKnowledgeIngest, useKnowledgePlayground } from './use-knowledge-documents'

defineOptions({ name: 'AiKnowledgeDocumentsPage' })

const { t } = useI18n()
const resource = knowledgeDocumentsResource()
const ingest = useKnowledgeIngest()
const playground = useKnowledgePlayground()

const activeTab = ref<'documents' | 'playground'>('documents')

/** 摄取弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as KnowledgeRow

function sourceTypeLabel(type: KnowledgeSourceType): string {
  return type === 'UploadFile' ? t('ai.knowledge_documents.source_upload') : t('ai.knowledge_documents.source_paste')
}

const STATUS_TONE = { Pending: 'warning', Indexed: 'success', Failed: 'danger' } as const

function statusLabel(status: KnowledgeIndexStatus): string {
  if (status === 'Indexed')
    return t('ai.knowledge_documents.status_indexed')
  return status === 'Failed' ? t('ai.knowledge_documents.status_failed') : t('ai.knowledge_documents.status_pending')
}

const sourceTypeOptions = computed<SelectOption[]>(() => (['PasteText', 'UploadFile'] as const).map(type => ({ label: sourceTypeLabel(type), value: type })))
const statusOptions = computed<SelectOption[]>(() => (['Pending', 'Indexed', 'Failed'] as const).map(status => ({ label: statusLabel(status), value: status })))

const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('ai.knowledge_documents.col_title'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('ai.knowledge_documents.search_placeholder'), order: 0 },
  { key: 'title', title: t('ai.knowledge_documents.col_title'), dataType: 'string', minWidth: 200, fixed: 'left', sortable: true, order: 1 },
  {
    key: 'sourceType',
    title: t('ai.knowledge_documents.col_source_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: sourceTypeOptions.value,
    searchPlaceholder: t('ai.knowledge_documents.col_source_type'),
    width: 110,
    order: 2,
    render: row => sourceTypeLabel(asRow(row).sourceType),
  },
  { key: 'source', title: t('ai.knowledge_documents.col_source'), dataType: 'string', minWidth: 140, order: 3 },
  { key: 'chunkCount', title: t('ai.knowledge_documents.col_chunk_count'), dataType: 'number', width: 90, sortable: true, order: 4 },
  {
    key: 'status',
    title: t('ai.common.status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: statusOptions.value,
    searchPlaceholder: t('ai.common.status'),
    width: 100,
    order: 5,
    render: row => renderTag(statusLabel(asRow(row).status), STATUS_TONE[asRow(row).status]),
  },
  { key: 'errorMessage', title: t('ai.knowledge_documents.col_error'), dataType: 'string', minWidth: 200, order: 6 },
  { key: 'createdAt', title: t('ai.knowledge_documents.col_created_at'), dataType: 'datetime', minWidth: 170, sortable: true, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'ai.knowledge-documents',
  batchRemovable: true,
  removePermission: 'ai.knowledge-documents.delete',
  pageName: t('ai.knowledge_documents.tabs_documents'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('ai.knowledge_documents.add'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'ai.knowledge-documents.create' },
    { key: 'reindex', title: t('ai.knowledge_documents.action_reindex'), scope: 'row', tone: 'info', icon: 'lucide:refresh-cw', confirm: true, confirmText: () => t('ai.knowledge_documents.confirm_reindex'), permission: 'ai.knowledge-documents.update' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', tone: 'danger', icon: 'lucide:trash-2', confirm: true, confirmText: () => t('ai.knowledge_documents.confirm_delete'), permission: 'ai.knowledge-documents.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      ingest.startCreate()
      break
    case 'reindex':
      if (row)
        void handleReindex(row)
      break
    case 'delete':
      if (row)
        void handleDelete(row)
      break
  }
}

function notifyIndexed(status: KnowledgeIndexStatus, errorMessage: null | string | undefined, onSuccess: () => void) {
  if (status === 'Failed')
    toast.danger(t('ai.knowledge_documents.ingest_index_failed', { msg: errorMessage ?? '' }))
  else
    onSuccess()
}

async function handleReindex(row: KnowledgeRow) {
  try {
    const document = await knowledgeDocuments.reindex({ id: row.id })
    notifyIndexed(document.status, document.errorMessage, () => toast.success(t('ai.knowledge_documents.reindex_success')))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('ai.knowledge_documents.reindex_failed'))
  }
}

async function handleDelete(row: KnowledgeRow) {
  try {
    await resource.remove!(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

function onFileSelected(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file)
    return
  const reader = new FileReader()
  reader.onload = () => ingest.applyFile(file.name, String(reader.result ?? ''))
  reader.onerror = () => toast.danger(t('ai.knowledge_documents.file_read_failed'))
  reader.readAsText(file)
  // 允许再次选同一文件
  target.value = ''
}

async function onSubmit() {
  const document = await ingest.submit()
  if (document) {
    notifyIndexed(document.status, document.errorMessage, () => toast.success(t('ai.knowledge_documents.ingest_success', { count: document.chunkCount })))
    reload()
  }
  else if (ingest.submitError.value) {
    toast.danger(ingest.submitError.value)
  }
}

async function handleQuery() {
  try {
    if (!await playground.run())
      toast.warning(t('ai.knowledge_documents.validate_query'))
  }
  catch (error) {
    toast.danger((error as Error).message || t('ai.knowledge_documents.query_failed'))
  }
}

/** 问题框里回车即检索、Shift+Enter 换行；输入法组合中的回车是在候选框里选词，不拦 */
function onQueryKeydown(event: KeyboardEvent) {
  if (isComposingEvent(event))
    return
  event.preventDefault()
  void handleQuery()
}
</script>

<template>
  <div class="knowledge">
    <XhTabsRoot v-model:value="activeTab" variant="line" class="knowledge__tabs">
      <XhTabsList>
        <XhTabsTrigger value="documents">
          {{ t('ai.knowledge_documents.tabs_documents') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="playground">
          {{ t('ai.knowledge_documents.tabs_playground') }}
        </XhTabsTrigger>
        <XhTabsIndicator />
      </XhTabsList>
      <XhTabsContent value="documents">
        <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
          <XEditModal
            v-model:show="ingest.open.value"
            :title="t('ai.knowledge_documents.modal_add_title')"
            :loading="ingest.submitting.value"
            :save-text="t('ai.knowledge_documents.ingest')"
            :form-id="editFormId"
          >
            <XhFormRoot :id="editFormId" class="xh-edit-form-grid" novalidate @submit="onSubmit">
              <XhFieldRoot :invalid="!!ingest.fieldError('title')" required>
                <XhFieldLabel>{{ t('ai.knowledge_documents.form_title') }}</XhFieldLabel>
                <XhFieldControl>
                  <XInput v-model:value="ingest.form.title" name="title" clearable :placeholder="t('ai.knowledge_documents.form_title_placeholder')" />
                </XhFieldControl>
                <XhFieldErrorText v-if="ingest.fieldError('title')">
                  {{ ingest.fieldError('title') }}
                </XhFieldErrorText>
              </XhFieldRoot>
              <XhFieldRoot :invalid="!!ingest.fieldError('embeddingProviderCode')">
                <XhFieldLabel>{{ t('ai.knowledge_documents.form_provider') }}</XhFieldLabel>
                <XhFieldControl>
                  <XInput v-model:value="ingest.form.embeddingProviderCode" name="embeddingProviderCode" clearable :placeholder="t('ai.knowledge_documents.form_provider_placeholder')" />
                </XhFieldControl>
                <XhFieldErrorText v-if="ingest.fieldError('embeddingProviderCode')">
                  {{ ingest.fieldError('embeddingProviderCode') }}
                </XhFieldErrorText>
              </XhFieldRoot>
              <XhFieldRoot :invalid="!!ingest.fieldError('text')" required class="xh-span-2">
                <XhFieldLabel>{{ t('ai.knowledge_documents.form_text') }}</XhFieldLabel>
                <XhFlex class="knowledge__text-bar" justify="between" align="center">
                  <XhButton variant="subtle" size="sm" type="button" @click="fileInput?.click()">
                    {{ t('ai.knowledge_documents.form_pick_file') }}
                  </XhButton>
                  <span v-if="ingest.form.source" class="knowledge__source">{{ ingest.form.source }}</span>
                </XhFlex>
                <XhFieldControl>
                  <XInput v-model:value="ingest.form.text" name="text" :rows="12" type="textarea" :placeholder="t('ai.knowledge_documents.form_text_placeholder')" />
                </XhFieldControl>
                <input ref="fileInput" :accept="INGEST_FILE_ACCEPT" class="knowledge__file" type="file" @change="onFileSelected">
                <XhFieldErrorText v-if="ingest.fieldError('text')">
                  {{ ingest.fieldError('text') }}
                </XhFieldErrorText>
              </XhFieldRoot>
              <XhFieldRoot :invalid="!!ingest.fieldError('remark')" class="xh-span-2">
                <XhFieldLabel>{{ t('ai.common.remark') }}</XhFieldLabel>
                <XhFieldControl>
                  <XInput v-model:value="ingest.form.remark" name="remark" clearable :rows="2" type="textarea" />
                </XhFieldControl>
                <XhFieldErrorText v-if="ingest.fieldError('remark')">
                  {{ ingest.fieldError('remark') }}
                </XhFieldErrorText>
              </XhFieldRoot>
            </XhFormRoot>
          </XEditModal>
        </SchemaPage>
      </XhTabsContent>
      <XhTabsContent value="playground">
        <div class="playground">
          <XhCardRoot variant="ghost">
            <XhCardContent>
              <XhFormRoot novalidate @submit.prevent="handleQuery">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('ai.knowledge_documents.query_label') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XInput
                      v-model:value="playground.question.value"
                      name="question"
                      :placeholder="t('ai.knowledge_documents.query_placeholder')"
                      :rows="3"
                      type="textarea"
                      @keydown.enter.exact="onQueryKeydown"
                    />
                  </XhFieldControl>
                </XhFieldRoot>
                <XhFlex class="playground__options" align="end" gap="md" :wrap="true">
                  <XhFieldRoot class="playground__top">
                    <XhFieldLabel>{{ t('ai.knowledge_documents.query_top') }}</XhFieldLabel>
                    <XhFieldControl>
                      <XNumberInput v-model:value="playground.top.value" name="top" :max="20" :min="1" />
                    </XhFieldControl>
                  </XhFieldRoot>
                  <XhFieldRoot class="playground__provider">
                    <XhFieldLabel>{{ t('ai.knowledge_documents.query_provider') }}</XhFieldLabel>
                    <XhFieldControl>
                      <XInput v-model:value="playground.provider.value" name="provider" clearable :placeholder="t('ai.knowledge_documents.query_provider_placeholder')" />
                    </XhFieldControl>
                  </XhFieldRoot>
                  <XhFieldRoot>
                    <XhFieldLabel>{{ t('ai.knowledge_documents.query_answer') }}</XhFieldLabel>
                    <XhFieldControl>
                      <XhSwitch v-model:checked="playground.wantAnswer.value" />
                    </XhFieldControl>
                  </XhFieldRoot>
                  <XhButton variant="subtle" size="sm" type="submit" tone="brand" :loading="playground.loading.value">
                    <XhButtonIndicator />
                    <XhButtonLabel>{{ t('ai.knowledge_documents.query_submit') }}</XhButtonLabel>
                  </XhButton>
                </XhFlex>
              </XhFormRoot>
            </XhCardContent>
          </XhCardRoot>

          <XhCardRoot v-if="playground.answer.value" variant="ghost">
            <XhCardContent>
              <div class="playground__answer">
                {{ playground.answer.value }}
              </div>
            </XhCardContent>
          </XhCardRoot>

          <div v-if="playground.citations.value.length > 0" class="playground__citations">
            <div class="playground__citations-title">
              {{ t('ai.knowledge_documents.citations_title', { count: playground.citations.value.length }) }}
            </div>
            <XhCardRoot v-for="(citation, index) in playground.citations.value" :key="`${citation.documentId}-${citation.index}`" variant="outline">
              <XhCardHeader>
                <XhFlex align="center" gap="sm" :wrap="true">
                  <XhTagRoot variant="subtle" size="sm" tone="info">
                    <XhTagLabel>[{{ index + 1 }}]</XhTagLabel>
                  </XhTagRoot>
                  <span class="playground__citation-title">{{ citationTitle(citation) }}</span>
                  <XhTagRoot variant="subtle" size="sm">
                    <XhTagLabel>{{ citation.score.toFixed(3) }}</XhTagLabel>
                  </XhTagRoot>
                </XhFlex>
              </XhCardHeader>
              <XhCardContent>
                <div class="playground__citation-text">
                  {{ citation.text }}
                </div>
              </XhCardContent>
            </XhCardRoot>
          </div>

          <XhEmptyStateRoot v-if="playground.searched.value && playground.citations.value.length === 0">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:search-x" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('ai.knowledge_documents.no_result_title') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('ai.knowledge_documents.no_result') }}</XhEmptyStateDescription>
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
  overflow: hidden;
}

.knowledge__tabs {
  flex: 1;
  min-height: 0;
}

.knowledge__tabs :deep([data-scope='tabs'][data-part='content']) {
  flex: 1;
  min-height: 0;
  padding-top: var(--xh-space-2);
}

.knowledge__text-bar {
  margin-bottom: var(--xh-space-1_5);
}

.knowledge__source {
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-subtle);
}

.knowledge__file {
  display: none;
}

.playground {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
  height: 100%;
  padding: var(--xh-space-3);
  overflow: auto;
}

.playground__options {
  margin-top: var(--xh-space-3);
}

.playground__top {
  width: 7.5rem;
}

.playground__provider {
  width: 12.5rem;
}

.playground__answer {
  white-space: pre-wrap;
  line-height: 1.7;
}

.playground__citations {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.playground__citations-title {
  font-weight: 600;
}

.playground__citation-title {
  font-weight: 500;
  overflow-wrap: anywhere;
}

.playground__citation-text {
  white-space: pre-wrap;
  font-size: var(--xh-font-size-sm);
  line-height: 1.6;
  color: var(--xh-fg-muted);
  overflow-wrap: anywhere;
}
</style>
