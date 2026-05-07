<script setup lang="ts">
import type { LogDetailField } from './log-detail.types'
import {
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NScrollbar,
  NSpin,
} from 'naive-ui'
import { computed } from 'vue'
import { formatDate, getOptionLabel } from '~/utils'

const props = withDefaults(
  defineProps<{
    fields: LogDetailField[]
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

const visible = computed({
  get: () => props.show,
  set: value => emit('update:show', value),
})

const normalFields = computed(() => props.fields.filter(field => field.type !== 'code'))
const codeFields = computed(() => props.fields.filter(field => field.type === 'code'))

function getRawValue(field: LogDetailField) {
  return (props.record as Record<string, unknown> | null)?.[field.key]
}

function isEmptyValue(value: unknown) {
  return value === undefined || value === null || value === ''
}

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

function formatCode(value: unknown) {
  if (isEmptyValue(value))
    return '-'

  if (typeof value !== 'string') {
    return JSON.stringify(value, null, 2)
  }

  const text = value.trim()
  if (!text)
    return '-'

  if (text.startsWith('{') || text.startsWith('[')) {
    try {
      return JSON.stringify(JSON.parse(text), null, 2)
    }
    catch {
      return value
    }
  }

  return value
}

function formatValue(field: LogDetailField) {
  const value = getRawValue(field)
  if (isEmptyValue(value))
    return '-'

  switch (field.type) {
    case 'boolean':
      return value ? field.trueText ?? '是' : field.falseText ?? '否'
    case 'bytes':
      return formatSize(value)
    case 'code':
      return formatCode(value)
    case 'date':
      return formatDate(String(value))
    case 'duration':
      return `${value}ms`
    case 'enum':
      return getOptionLabel(field.options ?? [], value as number | string)
    default:
      return String(value)
  }
}
</script>

<template>
  <NDrawer v-model:show="visible" :width="width" placement="right">
    <NDrawerContent closable :title="title">
      <NSpin :show="loading">
        <NScrollbar class="log-detail-scroll">
          <NDescriptions
            v-if="record"
            bordered
            label-placement="left"
            :column="2"
            size="small"
          >
            <NDescriptionsItem
              v-for="field in normalFields"
              :key="field.key"
              :label="field.label"
              :span="field.span ?? 1"
            >
              <span class="log-detail-value">{{ formatValue(field) }}</span>
            </NDescriptionsItem>
          </NDescriptions>

          <div v-if="record" class="log-detail-blocks">
            <section v-for="field in codeFields" :key="field.key" class="log-detail-block">
              <div class="log-detail-block__title">
                {{ field.label }}
              </div>
              <pre class="log-detail-pre">{{ formatValue(field) }}</pre>
            </section>
          </div>
        </NScrollbar>
      </NSpin>
    </NDrawerContent>
  </NDrawer>
</template>

<style scoped>
.log-detail-scroll {
  max-height: calc(100vh - 112px);
}

.log-detail-value {
  white-space: pre-wrap;
  word-break: break-word;
}

.log-detail-blocks {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-top: 12px;
}

.log-detail-block__title {
  margin-bottom: 6px;
  color: var(--text-secondary);
  font-size: 12px;
}

.log-detail-pre {
  max-height: 360px;
  margin: 0;
  padding: 10px;
  overflow: auto;
  border: 1px solid var(--border-color);
  border-radius: 6px;
  background: var(--code-bg, rgba(128, 128, 128, 0.08));
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', monospace;
  font-size: 12px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
