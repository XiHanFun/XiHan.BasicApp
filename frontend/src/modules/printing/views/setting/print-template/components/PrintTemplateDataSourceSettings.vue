<!--
  打印模板的数据源：可选的数据源、它的用途说明、切换为自由模板与字段清单。
-->
<script setup lang="ts">
import type { PrintTemplateFormModel } from './models'
import type { PrintFieldKind } from '~/printing'
import {
  XhButton,
  XhFieldControl,
  XhFieldDescription,
  XhFieldErrorText,
  XhFieldLabel,
  XhFieldRoot,
  XhFormFieldGroup,
  XhPopoverContent,
  XhPopoverPositioner,
  XhPopoverRoot,
  XhPopoverTitle,
  XhPopoverTrigger,
  XhScrollAreaContent,
  XhScrollAreaRoot,
  XhScrollAreaScrollbar,
  XhScrollAreaThumb,
  XhScrollAreaTrack,
  XhScrollAreaViewport,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSelect } from '~/components'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { ensureRemotePrintDataSourcesLoaded, getPrintDataSource, listPrintDataSources } from '~/printing'

defineOptions({ name: 'PrintTemplateDataSourceSettings' })

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
/** 字段清单里各内容种类的图标 */
const FIELD_KIND_ICONS: Record<PrintFieldKind, string> = {
  text: 'tabler:abc',
  image: 'tabler:photo',
  barcode: 'tabler:barcode',
  qrcode: 'tabler:qrcode',
  table: 'tabler:table',
}

/** 切换为自由模板；只改弹窗里的草稿，保存成功才生效。 */
function switchToFreeTemplate(): void {
  model.value.dataSourceCode = null
}
</script>

<template>
  <XhFormFieldGroup name="dataSourceCode">
    <XhFieldRoot>
      <XhFieldLabel>{{ `${t('setting.print_template.data_source')} (${t('setting.print_template.optional')})` }}</XhFieldLabel>
      <XhFieldControl>
        <XSelect
          v-model:value="model.dataSourceCode"
          :options="dataSourceOptions"
          :placeholder="t('setting.print_template.data_source_optional_placeholder')"
          clearable
        />
      </XhFieldControl>
      <XhFieldDescription>
        {{ selectedSource
          ? t('setting.print_template.data_source_registered_compact_help')
          : t('setting.print_template.data_source_free_compact_help') }}
      </XhFieldDescription>
      <XhFieldErrorText />
    </XhFieldRoot>
  </XhFormFieldGroup>

  <div v-if="selectedSource" class="data-source-actions">
    <XhButton size="sm" variant="subtle" @click="switchToFreeTemplate">
      {{ t('setting.print_template.switch_to_free_template') }}
    </XhButton>
    <XhPopoverRoot placement="bottom-end">
      <XhPopoverTrigger as-child>
        <XhButton size="sm" variant="ghost">
          <Icon icon="tabler:list-details" />
          {{ t('setting.print_template.view_field_list') }}
        </XhButton>
      </XhPopoverTrigger>
      <XhPopoverPositioner>
        <!-- 字段清单：数据源名称、编码与字段数在上面，字段一行一个，列表自己滚、表头不动 -->
        <XhPopoverContent class="field-list">
          <div class="field-list__header">
            <div class="field-list__heading">
              <XhPopoverTitle>{{ selectedSource.name }}</XhPopoverTitle>
              <code class="field-list__code">{{ selectedSource.code }}</code>
            </div>
            <XhTagRoot variant="subtle" size="sm">
              <XhTagLabel>{{ t('setting.print_template.field_count', { count: selectedSource.fields.length }) }}</XhTagLabel>
            </XhTagRoot>
          </div>
          <XhScrollAreaRoot size="sm">
            <XhScrollAreaViewport class="field-list__viewport">
              <XhScrollAreaContent>
                <ul class="field-list__items">
                  <li v-for="field in selectedSource.fields" :key="field.key" class="field-list__item">
                    <Icon class="field-list__kind" :icon="FIELD_KIND_ICONS[field.kind ?? 'text']" aria-hidden="true" />
                    <span class="field-list__label" :title="field.label">{{ field.label }}</span>
                    <code class="field-list__key" :title="field.key">{{ field.key }}</code>
                  </li>
                </ul>
              </XhScrollAreaContent>
            </XhScrollAreaViewport>
            <XhScrollAreaScrollbar orientation="vertical">
              <XhScrollAreaTrack>
                <XhScrollAreaThumb />
              </XhScrollAreaTrack>
            </XhScrollAreaScrollbar>
          </XhScrollAreaRoot>
        </XhPopoverContent>
      </XhPopoverPositioner>
    </XhPopoverRoot>
  </div>
</template>

<style scoped>
.data-source-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: var(--xh-space-2);
}

/* 字段清单：定宽，表头不动，字段列表封顶后自己滚 */
.field-list {
  --xh-popover-max-w: var(--xh-overlay-max-w-lg);
  --xh-popover-max-h: var(--xh-viewport-h-lg);
  --xh-popover-gap: var(--xh-space-2);

  inline-size: var(--xh-overlay-max-w-lg);
}

.field-list__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--xh-space-3);
  padding-block-end: var(--xh-space-3);
  border-block-end: 1px solid var(--xh-border-subtle);
}

.field-list__heading {
  display: flex;
  min-inline-size: 0;
  flex-direction: column;
  gap: var(--xh-space-1);
}

.field-list__code,
.field-list__key {
  overflow: hidden;
  color: var(--xh-fg-muted);
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-text-caption-size);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.field-list__viewport {
  max-block-size: var(--xh-viewport-h-md);
}

.field-list__items {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
}

.field-list__item {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  padding: var(--xh-space-1) var(--xh-space-2);
  border-radius: var(--xh-shape-control);
}

.field-list__item:hover {
  background: var(--xh-bg-subtle-hover);
}

.field-list__kind {
  flex: none;
  color: var(--xh-fg-subtle);
}

.field-list__label {
  overflow: hidden;
  flex: 1 1 auto;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.field-list__key {
  flex: 0 1 auto;
  text-align: end;
}
</style>
