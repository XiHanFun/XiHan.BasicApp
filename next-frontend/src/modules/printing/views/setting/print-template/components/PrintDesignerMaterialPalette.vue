<!--
  hiprint 设计器素材面板。
  职责：按官网分组展示内置通用组件，并把可选数据源字段作为建议素材提供，而不是限制模板可用字段。
-->
<script setup lang="ts">
import type { PrintFieldKind } from '~/printing'
import { XhBadge, XhButton, XhCollapsibleContent, XhCollapsibleRoot } from '@xihan-ui/vue'
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { ensureRemotePrintDataSourcesLoaded, getPrintDataSource, getPrintFieldTid } from '~/printing'

defineOptions({ name: 'PrintDesignerMaterialPalette' })

const props = defineProps<{
  dataSourceCode: null | string
}>()

const emit = defineEmits<{
  clear: []
  redo: []
  undo: []
}>()

/** hiprint 默认 provider 中可直接拖入画布的素材定义。 */
interface BuiltInMaterial {
  /** 图标库名称。 */
  icon: string
  /** 本地化文案键后缀。 */
  key: string
  /** 必须与 vue-plugin-hiprint@0.0.60 默认 provider 保持一致。 */
  tid: string
}

/** 由代码数据源生成的建议字段素材。 */
interface DataSourceMaterial {
  icon: string
  key: string
  label: string
  tid: string
}

const { t } = useI18n()
// hiprint 会沿用鼠标在大素材卡片内的按下偏移；归零后，新元素左上角与用户实际落点一致。
const EXTERNAL_DRAG_POINTER_OFFSET_PX = 0
const dataSourceFieldsId = `print-data-source-fields-${crypto.randomUUID()}`
const dataSourceExpanded = ref(false)

const commonMaterials: readonly BuiltInMaterial[] = Object.freeze([
  { key: 'text', tid: 'defaultModule.text', icon: 'mdi:format-text' },
  { key: 'image', tid: 'defaultModule.image', icon: 'mdi:image' },
  { key: 'long_text', tid: 'defaultModule.longText', icon: 'tabler:text-caption' },
  { key: 'table', tid: 'defaultModule.table', icon: 'mdi:grid' },
  { key: 'empty_table', tid: 'defaultModule.emptyTable', icon: 'mdi:grid' },
  { key: 'html', tid: 'defaultModule.html', icon: 'tabler:letter-h' },
  { key: 'custom', tid: 'defaultModule.customText', icon: 'mdi:format-text' },
])

const auxiliaryMaterials: readonly BuiltInMaterial[] = Object.freeze([
  { key: 'hline', tid: 'defaultModule.hline', icon: 'mdi:arrow-left-right-bold' },
  { key: 'vline', tid: 'defaultModule.vline', icon: 'mdi:arrow-up-down-bold' },
  { key: 'rect', tid: 'defaultModule.rect', icon: 'mdi:checkbox-blank-outline' },
  { key: 'oval', tid: 'defaultModule.oval', icon: 'mdi:circle-outline' },
  { key: 'barcode', tid: 'defaultModule.barcode', icon: 'mdi:barcode' },
  { key: 'qrcode', tid: 'defaultModule.qrcode', icon: 'mdi:qrcode' },
])

// 目录版本号：后端目录异步加载完成后递增，驱动建议字段重算（注册表本身非响应式）；
// 拉取失败由设计器画布统一提示，这里只记录
const catalogVersion = ref(0)
onMounted(async () => {
  try {
    await ensureRemotePrintDataSourcesLoaded()
  }
  catch (error) {
    console.error(error)
  }
  finally {
    catalogVersion.value++
  }
})

const activeDataSource = computed(() => {
  void catalogVersion.value
  return getPrintDataSource(props.dataSourceCode)
})
const dataSourceMaterials = computed<readonly DataSourceMaterial[]>(() => activeDataSource.value?.fields.map(field => ({
  icon: getFieldIcon(field.kind ?? 'text'),
  key: field.key,
  label: field.label,
  tid: getPrintFieldTid(activeDataSource.value!.code, field.key),
})) ?? [])

// 数据源切换后默认展开有效字段；自由模板没有建议字段，保持紧凑状态条即可。
watch(
  () => activeDataSource.value?.code,
  code => dataSourceExpanded.value = Boolean(code),
  { immediate: true },
)

/** 展开或收起当前代码数据源的建议字段素材，不改变模板绑定关系。 */
function toggleDataSourceFields(): void {
  if (activeDataSource.value)
    dataSourceExpanded.value = !dataSourceExpanded.value
}

/** 将业务字段类型映射到与通用素材一致的图标语义。 */
function getFieldIcon(kind: PrintFieldKind): string {
  return {
    barcode: 'mdi:barcode',
    image: 'mdi:image',
    qrcode: 'mdi:qrcode',
    table: 'mdi:grid',
    text: 'mdi:format-text',
  }[kind]
}
</script>

<template>
  <div class="material-palette" data-testid="print-material-palette">
    <div class="palette-heading">
      <h2>{{ t('setting.print_template.palette_title') }}</h2>
      <div class="history-actions">
        <XhButton variant="ghost" circle size="sm" :title="t('setting.print_template.undo')" @click="emit('undo')">
          <span><Icon icon="tabler:arrow-back-up" /></span>
        </XhButton>
        <XhButton variant="ghost" circle size="sm" :title="t('setting.print_template.redo')" @click="emit('redo')">
          <span><Icon icon="tabler:arrow-forward-up" /></span>
        </XhButton>
        <XhButton variant="ghost" circle size="sm" tone="warning" :title="t('setting.print_template.clear')" @click="emit('clear')">
          <span><Icon icon="tabler:trash" /></span>
        </XhButton>
      </div>
    </div>

    <section
      class="material-section data-source-section"
      :class="{ 'data-source-section--free': !activeDataSource }"
      :aria-label="t('setting.print_template.palette_data_fields')"
    >
      <button
        type="button"
        class="data-source-summary"
        :class="{ 'data-source-summary--expandable': activeDataSource }"
        :disabled="!activeDataSource"
        :title="activeDataSource
          ? t(dataSourceExpanded
            ? 'setting.print_template.palette_collapse_fields'
            : 'setting.print_template.palette_expand_fields')
          : t('setting.print_template.palette_free_mode_hint')"
        :aria-expanded="activeDataSource ? dataSourceExpanded : undefined"
        :aria-controls="activeDataSource ? dataSourceFieldsId : undefined"
        @click="toggleDataSourceFields"
      >
        <span class="data-source-icon" aria-hidden="true">
          <Icon width="20" height="20" :icon="activeDataSource ? 'tabler:database' : 'tabler:database-off'" />
        </span>
        <span class="data-source-copy">
          <strong>{{ activeDataSource
            ? t('setting.print_template.palette_data_fields')
            : t('setting.print_template.free_template') }}</strong>
          <span class="data-source-caption">
            {{ activeDataSource?.name || t('setting.print_template.palette_free_mode_compact_hint') }}
          </span>
        </span>
        <XhBadge v-if="activeDataSource" variant="subtle" size="sm" tone="info">
          {{ t('setting.print_template.palette_field_count', { count: dataSourceMaterials.length }) }}
        </XhBadge>
        <span
          v-if="activeDataSource"
          class="data-source-chevron"
          :class="{ 'data-source-chevron--expanded': dataSourceExpanded }"
          aria-hidden="true" style="display: inline-flex; font-size: 18px"
        ><Icon icon="tabler:chevron-down" /></span>
      </button>

      <XhCollapsibleRoot :open="Boolean(activeDataSource) && dataSourceExpanded">
        <XhCollapsibleContent>
          <div :id="dataSourceFieldsId" class="material-grid data-source-grid">
            <button
              v-for="material in dataSourceMaterials"
              :key="material.tid"
              type="button"
              class="ep-draggable-item material-item data-source-item"
              :deltax="EXTERNAL_DRAG_POINTER_OFFSET_PX"
              :deltay="EXTERNAL_DRAG_POINTER_OFFSET_PX"
              :data-testid="`print-data-field-${material.key}`"
              :tid="material.tid"
              :aria-label="material.label"
            >
              <span class="material-icon" style="display: inline-flex; font-size: 30px"><Icon :icon="material.icon" /></span>
              <span>{{ material.label }}</span>
            </button>
          </div>
        </XhCollapsibleContent>
      </XhCollapsibleRoot>
    </section>

    <section class="material-section common-section" :aria-label="t('setting.print_template.palette_common')">
      <div class="material-grid">
        <button
          v-for="material in commonMaterials"
          :key="material.tid"
          type="button"
          class="ep-draggable-item material-item"
          :class="{ 'material-item--new-row': material.key === 'html' }"
          :deltax="EXTERNAL_DRAG_POINTER_OFFSET_PX"
          :deltay="EXTERNAL_DRAG_POINTER_OFFSET_PX"
          :data-testid="`print-material-${material.key}`"
          :tid="material.tid"
          :aria-label="t(`setting.print_template.material_${material.key}`)"
        >
          <span class="material-icon" style="display: inline-flex; font-size: 48px"><Icon :icon="material.icon" /></span>
          <span>{{ t(`setting.print_template.material_${material.key}`) }}</span>
        </button>
      </div>
    </section>

    <section class="material-section" aria-labelledby="auxiliary-material-heading">
      <h3 id="auxiliary-material-heading">
        {{ t('setting.print_template.palette_auxiliary') }}
      </h3>
      <div class="material-grid">
        <button
          v-for="material in auxiliaryMaterials"
          :key="material.tid"
          type="button"
          class="ep-draggable-item material-item"
          :deltax="EXTERNAL_DRAG_POINTER_OFFSET_PX"
          :deltay="EXTERNAL_DRAG_POINTER_OFFSET_PX"
          :data-testid="`print-material-${material.key}`"
          :tid="material.tid"
          :aria-label="t(`setting.print_template.material_${material.key}`)"
        >
          <span class="material-icon" style="display: inline-flex; font-size: 48px"><Icon :icon="material.icon" /></span>
          <span>{{ t(`setting.print_template.material_${material.key}`) }}</span>
        </button>
      </div>
    </section>
  </div>
</template>

<style scoped>
.material-palette {
  min-height: 100%;
  padding: 22px 22px 28px;
  color: var(--n-text-color, #1f2937);
}

.palette-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.palette-heading h2,
.material-section h3 {
  margin: 0;
  font-weight: 650;
  color: var(--n-text-color, #1f2937);
}

.palette-heading h2 {
  font-size: 20px;
  line-height: 28px;
}

.history-actions {
  display: flex;
  flex: none;
  margin-right: -6px;
}

.material-section {
  margin-top: 24px;
}

.data-source-section {
  margin-top: 14px;
  overflow: hidden;
  border: 1px solid rgba(32, 128, 240, 0.18);
  border-radius: 10px;
  background: rgba(32, 128, 240, 0.035);
}

.data-source-section--free {
  border-color: rgba(148, 163, 184, 0.24);
  background: rgba(248, 250, 252, 0.76);
}

.data-source-summary {
  display: flex;
  width: 100%;
  min-height: 52px;
  align-items: center;
  gap: 10px;
  padding: 7px 10px;
  appearance: none;
  border: 0;
  background: transparent;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: default;
}

.data-source-summary--expandable {
  cursor: pointer;
  transition: background-color 0.16s ease;
}

.data-source-summary--expandable:hover,
.data-source-summary--expandable:focus-visible {
  background: rgba(32, 128, 240, 0.07);
}

.data-source-summary:focus-visible {
  outline: 2px solid rgba(32, 128, 240, 0.45);
  outline-offset: -2px;
}

.data-source-icon {
  display: inline-flex;
  width: 32px;
  height: 32px;
  flex: none;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  background: rgba(32, 128, 240, 0.1);
  color: #2080f0;
}

.data-source-copy {
  display: flex;
  min-width: 0;
  flex: 1;
  flex-direction: column;
  gap: 1px;
}

.data-source-copy strong {
  overflow: hidden;
  font-size: 13px;
  line-height: 19px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.data-source-caption {
  overflow: hidden;
  color: #64748b;
  font-size: 12px;
  line-height: 18px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.data-source-chevron {
  flex: none;
  color: #64748b;
  transition: transform 0.18s ease;
}

.data-source-chevron--expanded {
  transform: rotate(180deg);
}

.material-section h3 {
  margin-bottom: 10px;
  font-size: 16px;
  line-height: 24px;
}

.material-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px 18px;
}

.material-item {
  display: flex;
  min-height: 140px;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 7px;
  appearance: none;
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: #2080f0;
  font: inherit;
  font-size: 16px;
  user-select: none;
  cursor: move;
  transition:
    background-color 0.16s ease,
    transform 0.16s ease;
}

.material-item:hover,
.material-item:focus-visible {
  background: rgba(32, 128, 240, 0.09);
}

.material-item:focus-visible {
  outline: 2px solid rgba(32, 128, 240, 0.5);
  outline-offset: 1px;
}

.material-item:active {
  transform: scale(0.97);
}

.material-item--new-row {
  grid-column: 1;
}

.data-source-grid {
  padding: 8px 10px 10px;
  border-top: 1px solid rgba(32, 128, 240, 0.12);
  gap: 8px;
}

.data-source-item {
  min-height: 78px;
  padding: 8px 6px;
  border: 1px solid rgba(148, 163, 184, 0.2);
  background: rgba(255, 255, 255, 0.72);
  font-size: 13px;
}

:global(.dark) .data-source-section--free {
  border-color: rgba(100, 116, 139, 0.38);
  background: rgba(30, 41, 59, 0.54);
}

:global(.dark) .data-source-caption,
:global(.dark) .data-source-chevron {
  color: #94a3b8;
}

.material-icon {
  line-height: 1;
}
</style>
