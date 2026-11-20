<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { KnowledgeDetail } from '../use-knowledge-documents'
import { XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhSeparator, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { formatDate } from '~/utils'
import { sourceTypeLabelKey, splitByHit, STATUS_TONE, statusLabelKey } from '../use-knowledge-documents'

/**
 * 知识文档详情抽屉：索引状态、来源与原文
 * 从命中片段打开时在原文里标出片段并滚到那里；空值不显示
 */
const props = withDefaults(
  defineProps<{
    document?: KnowledgeDetail | null
    hit?: null | string
    loading?: boolean
    show: boolean
  }>(),
  {
    document: null,
    hit: null,
    loading: false,
  },
)

const emit = defineEmits<{
  (event: 'update:show', value: boolean): void
}>()

const { t } = useI18n()
const visible = computed({
  get: () => props.show,
  set: value => emit('update:show', value),
})

const entries = computed(() => {
  const document = props.document
  if (!document)
    return []
  return [
    {
      key: 'sourceType',
      label: t('ai.knowledge_documents.col_source_type'),
      value: t(sourceTypeLabelKey(document.sourceType)),
    },
    {
      key: 'source',
      label: t('ai.knowledge_documents.col_source'),
      value: document.source,
    },
    {
      key: 'chunkCount',
      label: t('ai.knowledge_documents.col_chunk_count'),
      value: String(document.chunkCount),
    },
    {
      key: 'provider',
      label: t('ai.knowledge_documents.form_provider'),
      value: document.embeddingProviderCode,
    },
    {
      key: 'createdAt',
      label: t('ai.knowledge_documents.col_created_at'),
      value: formatDate(document.createdAt),
    },
    {
      key: 'modifiedAt',
      label: t('ai.knowledge_documents.col_modified_at'),
      value: document.modifiedAt
        ? formatDate(document.modifiedAt)
        : null,
    },
    {
      key: 'error',
      label: t('ai.knowledge_documents.col_error'),
      value: document.errorMessage,
      full: true,
    },
    {
      key: 'remark',
      label: t('ai.common.remark'),
      value: document.remark,
      full: true,
    },
  ].filter(entry => entry.value)
})

const content = computed(() => splitByHit(props.document?.rawContent ?? '', props.hit))
const hitMark = ref<HTMLElement | null>(null)

// 原文加载好后滚到标出的片段
watch(() => props.document, async (document) => {
  if (!document || !content.value.hit)
    return
  await nextTick()
  hitMark.value?.scrollIntoView({ block: 'center' })
})
</script>

<template>
  <XhDrawerRoot v-model:open="visible" side="right">
    <XhDrawerContent class="w-4xl">
      <XhDrawerTitle>{{ t('ai.knowledge_documents.detail_title') }}</XhDrawerTitle>
      <XhDrawerCloseTrigger />
      <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
        <div class="xh-loading-stage__veil">
          <XhSpinner />
        </div>
        <div v-if="document" class="knowledge-detail">
          <header class="knowledge-detail__header">
            <span class="knowledge-detail__title">{{ document.title }}</span>
            <XhTagRoot variant="subtle" :tone="STATUS_TONE[document.status]" size="sm">
              <XhTagLabel>{{ t(statusLabelKey(document.status)) }}</XhTagLabel>
            </XhTagRoot>
          </header>
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem v-for="entry in entries" :key="entry.key" :span="entry.full ? 2 : undefined">
              <XhDescriptionsLabel>{{ entry.label }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <span class="knowledge-detail__value">{{ entry.value }}</span>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <section class="knowledge-detail__section" :aria-label="t('ai.knowledge_documents.detail_raw')">
            <XhSeparator>{{ t('ai.knowledge_documents.detail_raw') }}</XhSeparator>
            <div class="knowledge-detail__raw">
              {{ content.before }}<mark v-if="content.hit" ref="hitMark" class="knowledge-detail__hit">{{ content.hit }}</mark>{{ content.after }}
            </div>
          </section>
        </div>
      </div>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
.knowledge-detail {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.knowledge-detail__header {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2);
  align-items: center;
}

.knowledge-detail__title {
  font-size: var(--xh-text-heading-3-size);
  font-weight: var(--xh-text-heading-3-weight);
  overflow-wrap: anywhere;
}

.knowledge-detail__value {
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.knowledge-detail__section {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.knowledge-detail__raw {
  padding: var(--xh-space-3);
  font-size: var(--xh-font-size-sm);
  line-height: 1.7;
  color: var(--xh-fg-muted);
  overflow-wrap: anywhere;
  white-space: pre-wrap;
  background: var(--xh-bg-subtle);
  border-radius: var(--xh-shape-control);
}

.knowledge-detail__hit {
  color: var(--xh-fg-default);
  background: var(--xh-bg-mark);
  border-radius: var(--xh-shape-inset);
}
</style>
