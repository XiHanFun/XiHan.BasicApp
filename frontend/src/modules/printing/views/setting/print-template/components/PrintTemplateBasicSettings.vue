<!--
  打印模板基础设置页。
  职责：编辑模板名称、不可变编码及可选数据源，并展示必填项完成状态与高级设置入口。
-->
<script setup lang="ts">
import type { PrintTemplateFormModel } from './models'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NTag,
} from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import PrintTemplateDataSourceSettings from './PrintTemplateDataSourceSettings.vue'

defineOptions({ name: 'PrintTemplateBasicSettings' })

const props = defineProps<{
  editing: boolean
  template: Record<string, unknown>
}>()

const emit = defineEmits<{
  next: []
}>()

const model = defineModel<PrintTemplateFormModel>({ required: true })
const { t } = useI18n()

const completedRequiredCount = computed(() => [
  model.value.templateName,
  model.value.templateCode,
].filter(value => value.trim()).length)
const basicComplete = computed(() => completedRequiredCount.value === 2)
</script>

<template>
  <section class="basic-settings-panel" aria-labelledby="print-template-basic-tab">
    <NForm :model="model" label-placement="top">
      <NFormItem required :label="t('setting.print_template.template_name')">
        <NInput
          v-model:value="model.templateName"
          maxlength="100"
          :placeholder="t('setting.print_template.template_name_placeholder')"
        />
      </NFormItem>

      <NFormItem required :label="t('setting.print_template.template_code')">
        <NInput
          v-model:value="model.templateCode"
          :disabled="editing"
          maxlength="100"
          :placeholder="t('setting.print_template.template_code_placeholder')"
        >
          <template v-if="editing" #suffix>
            <NTag size="small" :bordered="false">
              {{ t('setting.print_template.template_code_immutable') }}
            </NTag>
          </template>
        </NInput>
      </NFormItem>

      <PrintTemplateDataSourceSettings v-model="model" :template="props.template" />
    </NForm>

    <div class="basic-completion" :class="{ 'is-complete': basicComplete }">
      <NIcon :size="22">
        <Icon :icon="basicComplete ? 'tabler:circle-check-filled' : 'tabler:alert-circle-filled'" />
      </NIcon>
      <div class="completion-copy">
        <strong>
          {{ basicComplete
            ? t('setting.print_template.basic_complete')
            : t('setting.print_template.basic_incomplete') }}
        </strong>
        <span>
          {{ basicComplete
            ? t('setting.print_template.basic_complete_help')
            : t('setting.print_template.basic_incomplete_help', { count: 2 - completedRequiredCount }) }}
        </span>
      </div>
      <NButton v-if="basicComplete" secondary type="primary" size="small" @click="emit('next')">
        {{ t('setting.print_template.continue_advanced') }}
        <template #icon>
          <NIcon>
            <Icon icon="tabler:arrow-right" />
          </NIcon>
        </template>
      </NButton>
    </div>
  </section>
</template>

<style scoped>
.basic-settings-panel {
  padding: 20px 22px 28px;
}

.basic-completion {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 8px;
  padding: 14px 16px;
  color: #b45309;
  border: 1px solid rgba(245, 158, 11, 0.28);
  border-radius: 8px;
  background: rgba(245, 158, 11, 0.05);
}

.basic-completion.is-complete {
  color: #16a34a;
  border-color: rgba(34, 197, 94, 0.28);
  background: rgba(34, 197, 94, 0.05);
}

.completion-copy {
  display: flex;
  min-width: 0;
  flex: 1;
  flex-direction: column;
  gap: 2px;
}

.completion-copy strong {
  color: #334155;
  font-size: 13px;
}

.completion-copy span {
  color: #64748b;
  font-size: 12px;
}

.dark .completion-copy strong {
  color: #e2e8f0;
}

.dark .completion-copy span {
  color: #94a3b8;
}
</style>
