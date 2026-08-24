<script setup lang="ts">
import { useFieldControl, XhButton } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { Icon } from '../../iconify'
import XEditModal from './EditModal.vue'

defineOptions({ name: 'XContentEditorField' })

const props = withDefaults(defineProps<{
  /** 弹窗标题 */
  title?: string
  /** 空内容时的占位文案 */
  placeholder?: string
  /** 摘要区按钮文案 */
  editText?: string
  /** 弹窗底部确定/取消文案 */
  confirmText?: string
  cancelText?: string
  /** 字数标签，接收字符数 */
  countLabel?: (count: number) => string
  /** 弹窗宽度 */
  width?: number | string
  /** 摘要区最多展示的字符数 */
  summaryLimit?: number
  disabled?: boolean
}>(), {
  width: 'min(1400px, calc(100vw - 64px))',
  summaryLimit: 160,
})

const model = defineModel<string>({ default: '' })

// 字段接线落在真正可聚焦的摘要钮上，标签的 for 才指得到
const fieldControl = useFieldControl()

const visible = ref(false)
/** 弹窗内编辑副本：确定才回写，取消丢弃 */
const draft = ref<string>('')

watch(visible, (open) => {
  if (open) {
    draft.value = model.value
  }
})

const charCount = computed(() => model.value.length)

const summary = computed(() => {
  const text = model.value.trim()
  if (!text) {
    return ''
  }
  const flat = text.replace(/\s+/g, ' ')
  return flat.length > props.summaryLimit ? `${flat.slice(0, props.summaryLimit)}…` : flat
})

function open() {
  if (!props.disabled) {
    visible.value = true
  }
}

function confirm() {
  model.value = draft.value
  visible.value = false
}

function updateDraft(value: null | string) {
  draft.value = value ?? ''
}
</script>

<template>
  <div class="xh-content-field">
    <button v-bind="fieldControl" type="button" class="xh-content-field__summary" :disabled="disabled" @click="open">
      <span v-if="summary" class="xh-content-field__text">{{ summary }}</span>
      <span v-else class="xh-content-field__placeholder">{{ placeholder }}</span>
    </button>
    <div class="xh-content-field__bar">
      <span class="xh-content-field__count">{{ countLabel ? countLabel(charCount) : charCount }}</span>
      <XhButton size="sm" variant="outline" :disabled="disabled" @click="open">
        <Icon icon="lucide:pencil" />
        {{ editText }}
      </XhButton>
    </div>

    <XEditModal
      v-model:show="visible"
      :title="title"
      :width="width"
      :save-text="confirmText"
      :cancel-text="cancelText"
      @save="confirm"
    >
      <!-- 编辑器由调用方提供：Markdown / 纯文本 / 富文本各页自选 -->
      <div class="xh-content-field__editor">
        <slot name="editor" :value="draft" :update="updateDraft" />
      </div>
      <!-- 底部附加操作位：如模板的「校验语法」，作用于当前草稿而非已回写的值 -->
      <template #footer-extra>
        <slot name="footer-extra" :value="draft" />
      </template>
    </XEditModal>
  </div>
</template>

<style scoped>
.xh-content-field {
  width: 100%;
  min-width: 0;
}

.xh-content-field__summary {
  display: block;
  width: 100%;
  min-height: 72px;
  padding: 8px 12px;
  text-align: left;
  font: inherit;
  color: inherit;
  background-color: var(--xh-bg-surface);
  border: 1px solid var(--xh-border-default);
  border-radius: 3px;
  cursor: pointer;
  transition: border-color 0.2s;
}

.xh-content-field__summary:hover:not(:disabled) {
  border-color: var(--xh-border-control-hover);
}

.xh-content-field__summary:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.xh-content-field__text {
  display: -webkit-box;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 3;
  line-clamp: 3;
  overflow: hidden;
  font-size: 13px;
  line-height: 1.6;
  word-break: break-word;
}

.xh-content-field__placeholder {
  font-size: 13px;
  opacity: 0.45;
}

.xh-content-field__bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-top: 6px;
}

/* 编辑区按视口定高并让内部编辑器撑满：md-editor-v3 自带 500px 固定高，
   不覆盖的话弹窗放多大编辑区都还是那么高 */
.xh-content-field__editor {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 240px);
  min-height: 420px;
}

.xh-content-field__editor > :deep(*) {
  flex: 1;
  min-height: 0;
}

.xh-content-field__editor :deep(.md-editor) {
  height: 100%;
}

/* 纯文本编辑同样撑满，不再受 autosize 行数限制 */
.xh-content-field__editor :deep([data-scope='text-field'][data-part='root']),
.xh-content-field__editor :deep([data-scope='text-field'][data-part='input']) {
  height: 100%;
}

.xh-content-field__count {
  font-size: 11px;
  opacity: 0.55;
}
</style>
