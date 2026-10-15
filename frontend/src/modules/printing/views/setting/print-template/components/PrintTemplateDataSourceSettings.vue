<!--
  打印模板数据源契约设置。
  职责：集中管理可选数据源、自由模板切换、字段目录及模板字段兼容性提示，避免基础设置页承担过多契约展示逻辑。
-->
<script setup lang="ts">
import type { PrintTemplateFormModel } from './models'
import { XhButton, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhPopoverContent, XhPopoverPositioner, XhPopoverRoot, XhPopoverTitle, XhPopoverTrigger } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSelect } from '~/components'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { ensureRemotePrintDataSourcesLoaded, extractPrintSampleFormSchema, getPrintDataSource, listPrintDataSources } from '~/printing'

defineOptions({ name: 'PrintTemplateDataSourceSettings' })

const props = defineProps<{
  template: Record<string, unknown>
}>()

const model = defineModel<PrintTemplateFormModel>({ required: true })
const { t } = useI18n()

// 目录版本号：后端目录异步加载完成后递增，驱动下拉与字段目录重算（注册表本身非响应式）
const catalogVersion = ref(0)
onMounted(async () => {
  try {
    await ensureRemotePrintDataSourcesLoaded()
  }
  catch (error) {
    // 拉取失败明确提示，避免把网络或权限异常误呈现为「数据源不存在」
    toast.danger(t('setting.print_template.data_source_catalog_load_failed'))
    console.error(error)
  }
  finally {
    catalogVersion.value++
  }
})

const selectedSource = computed(() => {
  void catalogVersion.value
  return getPrintDataSource(model.value.dataSourceCode)
})
const dataSourceOptions = computed(() => {
  void catalogVersion.value
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
  <XhFieldRoot>
    <XhFieldLabel>{{ `${t('setting.print_template.data_source')} (${t('setting.print_template.optional')})` }}</XhFieldLabel>
    <div class="data-source-field">
      <XhFieldControl>
        <XSelect
          v-model:value="model.dataSourceCode"
          :options="dataSourceOptions"
          :placeholder="t('setting.print_template.data_source_optional_placeholder')"
          clearable
        />
      </XhFieldControl>

      <div class="data-source-guidance">
        <span style="display: inline-flex; color: var(--xh-fg-brand); font-size: 18px"><Icon icon="tabler:info-circle" /></span>
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
        <XhButton size="sm" variant="subtle" tone="brand" @click="switchToFreeTemplate">
          {{ t('setting.print_template.switch_to_free_template') }}
        </XhButton>
        <XhPopoverRoot placement="bottom-end">
          <XhPopoverTrigger class="xh-linklike-trigger">
            {{ t('setting.print_template.view_field_list') }}
            <span><Icon icon="tabler:list-details" /></span>
          </XhPopoverTrigger>
          <XhPopoverPositioner>
            <XhPopoverContent>
              <div class="data-source-field-list">
                <div class="field-list-heading">
                  <XhPopoverTitle>{{ selectedSource.name }}</XhPopoverTitle>
                  <span>{{ selectedSource.code }}</span>
                </div>
                <div v-for="field in selectedSource.fields" :key="field.key" class="field-list-row">
                  <span>{{ field.label }}</span>
                  <code>{{ field.key }}</code>
                </div>
              </div>
            </XhPopoverContent>
          </XhPopoverPositioner>
        </XhPopoverRoot>
      </div>

      <XhPopoverRoot v-if="unmatchedFieldCount > 0" placement="bottom">
        <XhPopoverTrigger class="xh-linklike-trigger">
          <Icon width="17" height="17" icon="tabler:alert-triangle" />
          <span>{{ t('setting.print_template.data_source_mismatch_compact', { count: unmatchedFieldCount }) }}</span>
          <Icon width="16" height="16" icon="tabler:chevron-right" />
        </XhPopoverTrigger>
        <XhPopoverPositioner>
          <XhPopoverContent :aria-label="t('setting.print_template.data_source_mismatch_compact', { count: unmatchedFieldCount })">
            {{ t('setting.print_template.data_source_mismatch_warning', { count: unmatchedFieldCount }) }}
          </XhPopoverContent>
        </XhPopoverPositioner>
      </XhPopoverRoot>
    </div>
    <XhFieldErrorText />
  </XhFieldRoot>
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
  color: var(--xh-fg-muted);
  /* 说明卡按品牌色淡淡着色，跟着主题色走 */
  border: 1px solid color-mix(in oklab, var(--xh-bg-brand) 28%, transparent);
  border-radius: 8px;
  background: color-mix(in oklab, var(--xh-bg-brand) 4%, transparent);
}

.data-source-guidance strong {
  display: block;
  color: var(--xh-fg-default);
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
  /* 警示条与组件库提示条同一套兑法：淡底 12%、描边 40%、文字往前景色兑到 60%；悬停时底与线各加深一档 */
  color: color-mix(in oklab, var(--xh-color-warning-600) 60%, var(--xh-fg-default));
  font: inherit;
  font-size: 12px;
  text-align: left;
  cursor: pointer;
  border: 1px solid color-mix(in oklab, var(--xh-color-warning-600) 40%, var(--xh-bg-surface));
  border-radius: 7px;
  background: color-mix(in oklab, var(--xh-color-warning-600) 12%, var(--xh-bg-surface));
}

.compatibility-warning span {
  flex: 1;
}

.compatibility-warning:hover {
  border-color: color-mix(in oklab, var(--xh-color-warning-600) 60%, var(--xh-bg-surface));
  background: color-mix(in oklab, var(--xh-color-warning-600) 18%, var(--xh-bg-surface));
}

.data-source-field-list {
  max-height: 320px;
  overflow-y: auto;
}

.field-list-heading {
  padding-bottom: 10px;
  border-bottom: 1px solid var(--xh-border-subtle);
}

.field-list-heading span,
.field-list-row code {
  color: var(--xh-fg-muted);
  font-size: 12px;
}

.field-list-row {
  padding: 8px 0;
}
</style>
