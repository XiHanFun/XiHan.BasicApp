<script setup lang="ts">
import type { LogDetailEntry } from './log-detail-view'
import type { LogDetailField, LogDetailGroup, LogDetailSchema } from './log-detail.types'
import { XhButton, XhClipboardCopyTrigger, XhClipboardIndicator, XhClipboardRoot, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhSeparator, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Icon, XJsonBlock } from '~/components'
import { useEnumService } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'
import { buildLogDetailView } from './log-detail-view'
import { gotoTraceDimension } from './trace-nav'

/**
 * 日志详情抽屉（各日志页与链路追踪共用）。
 * 抬头一眼看懂「谁、什么时候、做了什么、成没成功」，其余字段按分组排成描边、标签在上的描述列表，
 * 原始数据（请求体、堆栈、User-Agent 等）排在最后；空值不显示，标识类的值可复制，追踪维度可点开链路追踪。
 */
const props = withDefaults(
  defineProps<{
    schema: LogDetailSchema
    loading?: boolean
    record?: object | null
    show: boolean
    title: string
    width?: number
  }>(),
  {
    loading: false,
    record: null,
    width: 820,
  },
)

const emit = defineEmits<{
  (event: 'update:show', value: boolean): void
}>()

const { t } = useI18n()
const router = useRouter()
const enumService = useEnumService()

// 声明了后端枚举名的字段：确保元数据已加载（整库取一次、并发去重），标签随后响应式刷新
watch(() => props.schema, (schema) => {
  for (const field of schema.fields) {
    if (field.enumName)
      void enumService.ensureEnum(field.enumName)
  }
}, { immediate: true })

const visible = computed({
  get: () => props.show,
  set: value => emit('update:show', value),
})

function formatSize(value: unknown) {
  const size = Number(value)
  if (!Number.isFinite(size))
    return String(value)
  if (size < 1024)
    return `${size}B`
  if (size < 1024 * 1024)
    return `${(size / 1024).toFixed(1)}KB`
  return `${(size / 1024 / 1024).toFixed(1)}MB`
}

function formatValue(field: LogDetailField, value: unknown) {
  switch (field.type) {
    case 'boolean':
      return value ? field.trueText ?? t('common.statuses.yes') : field.falseText ?? t('common.statuses.no')
    case 'bytes':
      return formatSize(value)
    case 'code':
      return typeof value === 'string' ? value : JSON.stringify(value)
    case 'date':
      return formatDate(String(value))
    case 'duration':
      return `${value}ms`
    case 'enum':
      return field.enumName
        ? enumService.getLabel(field.enumName, value as number | string)
        : getOptionLabel(field.options ?? [], value as number | string)
    default:
      return String(value)
  }
}

const view = computed(() => props.record ? buildLogDetailView(props.schema, props.record, formatValue) : null)

/** 记录的发生时间：跳链路追踪时把时间窗口居中到这一刻 */
const recordTime = computed(() => {
  const key = props.schema.header.timeKey
  const value = key ? (props.record as Record<string, unknown> | null)?.[key] : null
  return typeof value === 'string' ? value : null
})

const GROUP_LABEL_KEYS: Record<LogDetailGroup, string> = {
  subject: 'log.detail.group_subject',
  request: 'log.detail.group_request',
  client: 'log.detail.group_client',
  runtime: 'log.detail.group_runtime',
  handling: 'log.detail.group_handling',
  trace: 'log.detail.group_trace',
}

/** 放标识、地址这类长值的组排两列，其余排三列 */
function columnsOf(entries: LogDetailEntry[]) {
  return entries.some(entry => entry.field.mono) ? 2 : 3
}

function openTrace(entry: LogDetailEntry) {
  if (entry.field.trace && gotoTraceDimension(router, entry.field.trace, entry.raw, recordTime.value))
    visible.value = false
}

const clipboardTranslations = computed(() => ({ copy: t('common.actions.copy'), copied: t('log.detail.copied') }))
</script>

<template>
  <XhDrawerRoot v-model:open="visible" side="right">
    <XhDrawerContent :style="{ '--xh-drawer-size': `${width}px` }">
      <XhDrawerTitle>{{ title }}</XhDrawerTitle>
      <XhDrawerCloseTrigger />
      <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
        <div class="xh-loading-stage__veil">
          <XhSpinner />
        </div>
        <div>
          <div v-if="view" class="log-detail">
            <!-- 抬头：做了什么、成没成功，下一行是谁、什么时候、在哪 -->
            <header class="log-detail__header">
              <div class="log-detail__headline">
                <span class="log-detail__title">{{ view.title || title }}</span>
                <XhTagRoot v-if="view.status" variant="subtle" :tone="view.status.tone" size="sm">
                  <XhTagLabel>{{ view.status.text }}</XhTagLabel>
                </XhTagRoot>
                <XhTagRoot v-for="badge in view.badges" :key="badge.field.key" variant="outline" tone="neutral" size="sm">
                  <XhTagLabel>{{ badge.field.type === 'duration' || badge.field.type === 'enum' || badge.field.type === 'boolean' ? badge.text : `${badge.field.label} ${badge.text}` }}</XhTagLabel>
                </XhTagRoot>
              </div>
              <div v-if="view.meta.length" class="log-detail__meta">
                <template v-for="(entry, index) in view.meta" :key="entry.field.key">
                  <span v-if="index > 0" aria-hidden="true">·</span>
                  <XhButton
                    v-if="entry.field.trace"
                    variant="ghost"
                    tone="brand"
                    size="sm"
                    class="log-detail__link"
                    :aria-label="t('log.detail.open_trace', { label: entry.field.label, value: entry.text })"
                    @click="openTrace(entry)"
                  >
                    {{ entry.text }}
                  </XhButton>
                  <span v-else>{{ entry.text }}</span>
                </template>
              </div>
            </header>

            <!-- 分组：描边、标签在上 -->
            <section v-for="group in view.groups" :key="group.group" class="log-detail__section" :aria-label="t(GROUP_LABEL_KEYS[group.group])">
              <XhSeparator>{{ t(GROUP_LABEL_KEYS[group.group]) }}</XhSeparator>
              <XhDescriptionsRoot :columns="columnsOf(group.entries)" variant="outline" size="sm">
                <XhDescriptionsItem
                  v-for="entry in group.entries"
                  :key="entry.field.key"
                  :span="entry.field.span === 'full' ? columnsOf(group.entries) : undefined"
                >
                  <XhDescriptionsLabel>{{ entry.field.label }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    <span class="log-detail__value" :class="{ 'log-detail__value--mono': entry.field.mono }">
                      <XhButton
                        v-if="entry.field.trace"
                        variant="ghost"
                        tone="brand"
                        size="sm"
                        class="log-detail__link"
                        :aria-label="t('log.detail.open_trace', { label: entry.field.label, value: entry.text })"
                        @click="openTrace(entry)"
                      >
                        {{ entry.text }}
                      </XhButton>
                      <span v-else>{{ entry.text }}</span>
                      <XhClipboardRoot
                        v-if="entry.field.mono"
                        :value="entry.raw"
                        variant="ghost"
                        size="sm"
                        :translations="clipboardTranslations"
                      >
                        <XhClipboardCopyTrigger>
                          <XhClipboardIndicator>
                            <Icon icon="lucide:copy" />
                          </XhClipboardIndicator>
                          <XhClipboardIndicator copied>
                            <Icon icon="lucide:check" />
                          </XhClipboardIndicator>
                        </XhClipboardCopyTrigger>
                      </XhClipboardRoot>
                    </span>
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
              </XhDescriptionsRoot>
            </section>

            <!-- 原始数据：JSON 展开成树，其余原样；自带复制 -->
            <section v-for="block in view.blocks" :key="block.field.key" class="log-detail__section" :aria-label="block.field.label">
              <XhSeparator>{{ block.field.label }}</XhSeparator>
              <XJsonBlock :raw="block.text" :default-expanded-depth="2" max-height="22rem" />
            </section>
          </div>
        </div>
      </div>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
.log-detail {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-4);
}

.log-detail__header {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
}

.log-detail__headline {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.log-detail__title {
  font-size: var(--xh-text-heading-3-size);
  font-weight: var(--xh-text-heading-3-weight);
  word-break: break-all;
}

.log-detail__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-1);
  color: var(--xh-fg-muted);
}

.log-detail__section {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.log-detail__value {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1);
  max-inline-size: 100%;
  white-space: pre-wrap;
  word-break: break-word;
}

.log-detail__value--mono {
  font-family: var(--xh-font-family-mono);
  word-break: break-all;
}

/* 追踪链接贴着文字排，不占按钮的内距 */
.log-detail__link {
  padding-inline: 0;
  block-size: auto;
  min-block-size: 0;
}
</style>
