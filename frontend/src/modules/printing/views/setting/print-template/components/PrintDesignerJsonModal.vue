<!--
  打印设计器 JSON 模板编辑对话框。
  职责：在不占用常驻画布空间的前提下，提供当前设计导出到 TextArea、手工编辑和回写画布入口。
-->
<script setup lang="ts">
import { NAlert, NButton, NInput, NModal } from 'naive-ui'
import { useI18n } from 'vue-i18n'

defineOptions({ name: 'PrintDesignerJsonModal' })

defineProps<{
  applying: boolean
  disabled?: boolean
  error: string
  show: boolean
  value: string
}>()

const emit = defineEmits<{
  'apply': []
  'export': []
  'update:show': [show: boolean]
  'update:value': [value: string]
}>()

const { t } = useI18n()
const textareaId = `print-template-json-${crypto.randomUUID()}`
</script>

<template>
  <NModal
    :show="show"
    preset="card"
    class="json-template-modal"
    :title="t('setting.print_template.json_editor_title')"
    :mask-closable="!applying"
    :closable="!applying"
    :style="{ width: '920px', maxWidth: 'calc(100vw - 32px)' }"
    @update:show="emit('update:show', $event)"
  >
    <div class="json-editor-toolbar">
      <div class="json-editor-intro">
        <strong>{{ t('setting.print_template.json_template_content') }}</strong>
        <span>{{ t('setting.print_template.json_editor_hint') }}</span>
      </div>
      <NButton :disabled="disabled || applying" @click="emit('export')">
        {{ t('setting.print_template.export_json_to_textarea') }}
      </NButton>
    </div>

    <label class="json-textarea-field" :for="textareaId">
      <NInput
        :value="value"
        type="textarea"
        :autosize="{ minRows: 15, maxRows: 22 }"
        :disabled="disabled || applying"
        :placeholder="t('setting.print_template.json_textarea_placeholder')"
        :input-props="{ id: textareaId, spellcheck: 'false' }"
        @update:value="emit('update:value', $event)"
      />
    </label>

    <NAlert v-if="error" class="json-error" type="error" :show-icon="true">
      {{ error }}
    </NAlert>

    <p class="json-memory-tip">
      {{ t('setting.print_template.json_memory_only') }}
    </p>

    <template #footer>
      <div class="json-modal-actions">
        <NButton :disabled="applying" @click="emit('update:show', false)">
          {{ t('common.actions.cancel') }}
        </NButton>
        <NButton
          type="primary"
          :loading="applying"
          :disabled="disabled || !value.trim()"
          @click="emit('apply')"
        >
          {{ t('setting.print_template.apply_json_template') }}
        </NButton>
      </div>
    </template>
  </NModal>
</template>

<style scoped>
.json-editor-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 12px;
}

.json-editor-intro {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 3px;
  color: #334155;
}

.json-editor-intro span,
.json-memory-tip {
  color: #64748b;
  font-size: 12px;
  line-height: 1.6;
}

.json-textarea-field {
  display: block;
}

.json-textarea-field :deep(textarea) {
  font-family: 'Cascadia Code', 'JetBrains Mono', Consolas, monospace;
  font-size: 12px;
  line-height: 1.6;
  tab-size: 2;
}

.json-error {
  margin-top: 12px;
}

.json-memory-tip {
  margin: 10px 0 0;
}

.json-modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

:global(.dark) .json-editor-intro {
  color: #e2e8f0;
}

:global(.dark) .json-editor-intro span,
:global(.dark) .json-memory-tip {
  color: #94a3b8;
}
</style>
