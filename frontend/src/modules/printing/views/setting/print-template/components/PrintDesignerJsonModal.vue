<!--
  打印设计器 JSON 模板编辑对话框。
  职责：在不占用常驻画布空间的前提下，提供当前设计导出到 TextArea、手工编辑和回写画布入口。
-->
<script setup lang="ts">
import { XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot, XhButton, XhButtonIndicator, XhButtonLabel, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { XInput } from '~/components'
import { Icon } from '~/iconify'

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
  <XhDialogRoot
    :open="show"
    @update:open="(open: boolean) => emit('update:show', open)"
  >
    <XhDialogContent class="json-template-modal" style="--xh-dialog-max-w: 920px">
      <XhDialogHeader>
        <XhDialogTitle>{{ t('setting.print_template.json_editor_title') }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody class="xh-overlay-body">
        <div class="json-editor-toolbar">
          <div class="json-editor-intro">
            <strong :id="`${textareaId}-label`">{{ t('setting.print_template.json_template_content') }}</strong>
            <span :id="`${textareaId}-hint`">{{ t('setting.print_template.json_editor_hint') }}</span>
          </div>
          <XhButton variant="subtle" :disabled="disabled || applying" @click="emit('export')">
            {{ t('setting.print_template.export_json_to_textarea') }}
          </XhButton>
        </div>

        <!-- 包着输入框的 label 里没有文字，名字取上方的标题行 -->
        <label class="json-textarea-field" :for="textareaId">
          <XInput
            :id="textareaId"
            :aria-labelledby="`${textareaId}-label`"
            :aria-describedby="`${textareaId}-hint`"
            :value="value"
            type="textarea"
            :autosize="{ minRows: 15, maxRows: 22 }"
            :disabled="disabled || applying"
            :placeholder="t('setting.print_template.json_textarea_placeholder')"
            spellcheck="false"
            @update:value="emit('update:value', $event)"
          />
        </label>

        <XhAlertRoot v-if="error" tone="danger" class="json-error">
          <XhAlertIndicator>
            <Icon icon="lucide:circle-alert" width="16" />
          </XhAlertIndicator>
          <XhAlertContent>
            <XhAlertDescription>
              {{ error }}
            </XhAlertDescription>
          </XhAlertContent>
        </XhAlertRoot>

        <p class="json-memory-tip">
          {{ t('setting.print_template.json_memory_only') }}
        </p>
      </XhDialogBody>
      <XhDialogFooter>
        <XhButton variant="subtle" :disabled="applying" @click="emit('update:show', false)">
          {{ t('common.actions.cancel') }}
        </XhButton>
        <XhButton
          variant="subtle"
          tone="brand"
          :loading="applying"
          :disabled="disabled || !value.trim()"
          @click="emit('apply')"
        >
          <XhButtonIndicator />
          <XhButtonLabel>{{ t('setting.print_template.apply_json_template') }}</XhButtonLabel>
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
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
  color: var(--xh-fg-default);
}

.json-editor-intro span,
.json-memory-tip {
  color: var(--xh-fg-muted);
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
</style>
