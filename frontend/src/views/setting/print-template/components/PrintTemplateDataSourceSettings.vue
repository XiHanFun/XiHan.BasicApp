<!--
  打印模板数据源契约设置。
  职责：集中管理可选数据源、自由模板切换、字段目录及模板字段兼容性提示，避免基础设置页承担过多契约展示逻辑。
-->
<script setup lang="ts">
import type { PrintTemplateFormModel } from './models'
import {
  NButton,
  NFormItem,
  NIcon,
  NPopover,
  NSelect,
} from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { extractPrintSampleFormSchema, getPrintDataSource, listPrintDataSources } from '~/printing'

defineOptions({ name: 'PrintTemplateDataSourceSettings' })

const props = defineProps<{
  template: Record<string, unknown>
}>()

const model = defineModel<PrintTemplateFormModel>({ required: true })
const { t } = useI18n()

const selectedSource = computed(() => getPrintDataSource(model.value.dataSourceCode))
const dataSourceOptions = computed(() => {
  const options = listPrintDataSources().map(source => ({
    label: `${source.name} (${source.code})`,
    value: source.code,
  }))
  const currentCode = model.value.dataSourceCode?.trim()
  if (currentCode && !options.some(option => option.value === currentCode)) {
    options.unshift({
      label: t('setting.print_template.data_source_missing_option', { code: currentCode }),
      value: currentCode,
    })
  }
  return options
})
const unmatchedFieldCount = computed(() => {
  if (!selectedSource.value)
    return 0
  try {
    return extractPrintSampleFormSchema(props.template, selectedSource.value.code)
      .fields
      .filter(field => !field.registered)
      .length
  }
  catch {
    // 模板结构错误由保存或 JSON 编辑器统一处理；设置抽屉只展示非阻断的兼容性提示。
    return 0
  }
})

/** 切换为自由模板；这里只修改抽屉草稿，保存成功前不会重建设计器。 */
function switchToFreeTemplate(): void {
  model.value.dataSourceCode = null
}
</script>

<template>
  <NFormItem :label="`${t('setting.print_template.data_source')} (${t('setting.print_template.optional')})`">
    <div class="data-source-field">
      <NSelect
        v-model:value="model.dataSourceCode"
        :options="dataSourceOptions"
        :placeholder="t('setting.print_template.data_source_optional_placeholder')"
        clearable
        filterable
      />

      <div class="data-source-guidance">
        <NIcon :size="18" color="#4f7cf7">
          <Icon icon="tabler:info-circle" />
        </NIcon>
        <div>
          <strong>{{ t('setting.print_template.data_source_guidance_title') }}</strong>
          <p>
            {{ selectedSource
              ? t('setting.print_template.data_source_registered_compact_help')
              : t('setting.print_template.data_source_free_compact_help') }}
          </p>
        </div>
      </div>

      <div v-if="selectedSource" class="data-source-actions">
        <NButton size="small" secondary type="primary" @click="switchToFreeTemplate">
          {{ t('setting.print_template.switch_to_free_template') }}
        </NButton>
        <NPopover trigger="click" placement="bottom-end" :width="320">
          <template #trigger>
            <NButton text type="primary" size="small">
              {{ t('setting.print_template.view_field_list') }}
              <template #icon>
                <NIcon>
                  <Icon icon="tabler:list-details" />
                </NIcon>
              </template>
            </NButton>
          </template>
          <div class="data-source-field-list">
            <div class="field-list-heading">
              <strong>{{ selectedSource.name }}</strong>
              <span>{{ selectedSource.code }}</span>
            </div>
            <div v-for="field in selectedSource.fields" :key="field.key" class="field-list-row">
              <span>{{ field.label }}</span>
              <code>{{ field.key }}</code>
            </div>
          </div>
        </NPopover>
      </div>

      <NPopover v-if="unmatchedFieldCount > 0" trigger="click" placement="bottom" :width="340">
        <template #trigger>
          <button type="button" class="compatibility-warning">
            <NIcon :size="17">
              <Icon icon="tabler:alert-triangle" />
            </NIcon>
            <span>{{ t('setting.print_template.data_source_mismatch_compact', { count: unmatchedFieldCount }) }}</span>
            <NIcon :size="16">
              <Icon icon="tabler:chevron-right" />
            </NIcon>
          </button>
        </template>
        {{ t('setting.print_template.data_source_mismatch_warning', { count: unmatchedFieldCount }) }}
      </NPopover>
    </div>
  </NFormItem>
</template>

<style scoped>
.data-source-field {
  display: flex;
  width: 100%;
  flex-direction: column;
  gap: 10px;
}

.data-source-guidance {
  display: flex;
  align-items: flex-start;
  gap: 9px;
  padding: 10px 12px;
  color: #475569;
  border: 1px solid rgba(79, 124, 247, 0.28);
  border-radius: 8px;
  background: rgba(79, 124, 247, 0.04);
}

.data-source-guidance strong {
  display: block;
  color: #334155;
  font-size: 13px;
  line-height: 1.5;
}

.data-source-guidance p {
  margin: 3px 0 0;
  font-size: 12px;
  line-height: 1.6;
}

.data-source-actions,
.field-list-heading,
.field-list-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.compatibility-warning {
  display: flex;
  width: 100%;
  min-height: 34px;
  padding: 6px 10px;
  align-items: center;
  gap: 7px;
  color: #b45309;
  font: inherit;
  font-size: 12px;
  text-align: left;
  cursor: pointer;
  border: 1px solid rgba(245, 158, 11, 0.26);
  border-radius: 7px;
  background: rgba(245, 158, 11, 0.06);
}

.compatibility-warning span {
  flex: 1;
}

.compatibility-warning:hover {
  border-color: rgba(245, 158, 11, 0.5);
  background: rgba(245, 158, 11, 0.1);
}

.data-source-field-list {
  max-height: 320px;
  overflow-y: auto;
}

.field-list-heading {
  padding-bottom: 10px;
  border-bottom: 1px solid rgba(148, 163, 184, 0.24);
}

.field-list-heading span,
.field-list-row code {
  color: #64748b;
  font-size: 12px;
}

.field-list-row {
  padding: 8px 0;
}

.dark .data-source-guidance strong {
  color: #e2e8f0;
}

.dark .data-source-guidance,
.dark .field-list-heading span,
.dark .field-list-row code {
  color: #94a3b8;
}
</style>
