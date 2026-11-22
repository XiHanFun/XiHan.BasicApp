<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ListFieldSchema } from './types'
import type { ConditionPreset, PresetSnapshot } from './usePresetManager'
import {
  XhButton,
  XhPopoverContent,
  XhPopoverPositioner,
  XhPopoverRoot,
  XhPopoverTitle,
  XhPopoverTrigger,
  XhSeparator,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'
import SyncStatusBadge from '../common/SyncStatusBadge.vue'
import XInput from '../common/XInput.vue'
import { conditionSentence, describeConditions } from './condition-summary'

/**
 * 条件预设浮层：列出预设（点名称即套用，悬停看它存的条件，眼睛预览，星标设默认，可删除）；「当前条件」先起名、再列出当前的筛选、排序与每页条数，右上角保存（回车也行）
 * 同名的覆盖原来那份；默认预设在打开页面时自动套用，再点星标取消
 * 预览只换下面那块的显示：列出这个预设存的条件，名称行收起、保存置灰（看的是预设，存的是当前），再点一次或关掉浮层即还原
 */
defineOptions({ name: 'SchemaPresetMenu' })

const props = defineProps<{
  /** 已存的预设 */
  presets: ConditionPreset[]
  /** 当前套用的预设 */
  activeCode?: string
  /** 页面字段：把筛选与排序说成字段名与选项名 */
  fields: readonly ListFieldSchema[]
  /** 当前的筛选、排序与每页条数，即保存时存下的那份 */
  current: PresetSnapshot
}>()

const emit = defineEmits<{
  apply: [code: string]
  save: [name: string]
  setDefault: [code: null | string]
  remove: [code: string]
}>()

/** 预设名称的长度上限 */
const NAME_MAX = 40

const { t } = useI18n()
const appStore = useAppStore()
const open = ref(false)
const name = ref('')
const currentTitleId = useId()
const savedTitleId = useId()

/** 正在预览的预设：没有或已被删除时显示当前条件 */
const previewCode = ref<null | string>(null)
const previewing = computed(() => props.presets.find(preset => preset.code === previewCode.value))

const trimmed = computed(() => name.value.trim())
/** 与已有的同名时保存即覆盖它 */
const overwriting = computed(() => props.presets.some(preset => preset.name === trimmed.value))
const currentSummary = computed(() => describeConditions(props.fields, previewing.value?.snapshot ?? props.current))

// 关掉浮层即还原：下次打开看到的是当前条件
watch(open, (value) => {
  if (!value)
    previewCode.value = null
})

/** 当前条件按筛选、排序、每页分三组，每组一列标签 */
const conditionGroups = computed(() => [
  {
    key: 'filters',
    label: t('component.schema_presets.filters'),
    items: currentSummary.value.filters,
    tone: 'brand' as const,
  },
  {
    key: 'sorts',
    label: t('component.schema_presets.sorts'),
    items: currentSummary.value.sorts,
    tone: 'brand' as const,
  },
  ...(currentSummary.value.pageSize
    ? [
        {
          key: 'page-size',
          label: t('component.schema_presets.page_size'),
          items: [
            t('component.schema_presets.page_size_value', { n: currentSummary.value.pageSize }),
          ],
          tone: 'neutral' as const,
        },
      ]
    : []),
])

function presetHint(preset: ConditionPreset): string {
  return conditionSentence(describeConditions(props.fields, preset.snapshot))
}

function togglePreview(preset: ConditionPreset) {
  previewCode.value = previewCode.value === preset.code
    ? null
    : preset.code
}

function apply(code: string) {
  emit('apply', code)
  open.value = false
}

function save() {
  if (trimmed.value === '' || previewing.value)
    return
  emit('save', trimmed.value)
  name.value = ''
  open.value = false
}

function toggleDefault(preset: ConditionPreset) {
  emit('setDefault', preset.isDefault
    ? null
    : preset.code)
}
</script>

<template>
  <XhPopoverRoot v-model:open="open" placement="bottom-end">
    <!-- 浮层触发器合到组件库的纯图标按钮上，与同排的 XIconButton 同为 sm 档、同高 -->
    <XhPopoverTrigger as-child>
      <XhButton
        class="x-icon-btn"
        variant="ghost"
        size="sm"
        icon-only
        data-guide="schema.presets"
        :aria-label="t('component.schema_presets.title')"
      >
        <Icon :icon="activeCode ? 'lucide:bookmark-check' : 'lucide:bookmark'" />
      </XhButton>
    </XhPopoverTrigger>
    <XhPopoverPositioner>
      <!-- 版式随表格设置：标题行带同步标记，分隔线分段，左边小字名称、右边内容 -->
      <XhPopoverContent class="schema-presets">
        <div class="schema-presets__body flex flex-col gap-2">
          <!-- 标题行：左边标题与同步标记，右边保存（与表格设置同一位置） -->
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <!-- 标题部件给浮层起名（content 的 aria-labelledby 指向它） -->
              <XhPopoverTitle class="text-base font-semibold text-foreground">
                {{ t('component.schema_presets.title') }}
              </XhPopoverTitle>
              <SyncStatusBadge :synced="appStore.presetSyncEnabled" />
            </div>
            <XhButton size="sm" variant="solid" :disabled="trimmed === '' || !!previewing" @click="save">
              {{ overwriting ? t('component.schema_presets.overwrite') : t('component.schema_presets.save') }}
            </XhButton>
          </div>

          <XhSeparator class="my-1" />

          <!-- 已存的预设：与下面的当前条件同款小标题 -->
          <section class="schema-presets__saved flex flex-col gap-2" :aria-labelledby="savedTitleId">
            <h3 :id="savedTitleId" class="schema-presets__section">
              {{ t('component.schema_presets.saved') }}
            </h3>
            <ul v-if="presets.length" class="schema-presets__list">
              <li v-for="preset in presets" :key="preset.code" class="schema-presets__row">
                <XhButton
                  variant="ghost"
                  size="sm"
                  class="schema-presets__apply"
                  :aria-current="preset.code === activeCode ? 'true' : undefined"
                  :title="presetHint(preset)"
                  @click="apply(preset.code)"
                >
                  <span class="schema-presets__name">{{ preset.name }}</span>
                  <Icon v-if="preset.code === activeCode" class="schema-presets__check" icon="lucide:check" aria-hidden="true" />
                </XhButton>
                <XhButton
                  variant="ghost"
                  size="sm"
                  icon-only
                  :class="{ 'schema-presets__previewing': preset.code === previewCode }"
                  :aria-pressed="preset.code === previewCode ? 'true' : 'false'"
                  :aria-label="preset.code === previewCode ? t('component.schema_presets.preview_back') : t('component.schema_presets.preview', { name: preset.name })"
                  :title="preset.code === previewCode ? t('component.schema_presets.preview_back') : t('component.schema_presets.preview', { name: preset.name })"
                  @click="togglePreview(preset)"
                >
                  <Icon :icon="preset.code === previewCode ? 'lucide:eye-off' : 'lucide:eye'" />
                </XhButton>
                <XhButton
                  variant="ghost"
                  size="sm"
                  icon-only
                  :aria-pressed="preset.isDefault ? 'true' : 'false'"
                  :aria-label="preset.isDefault ? t('component.schema_presets.unset_default', { name: preset.name }) : t('component.schema_presets.set_default', { name: preset.name })"
                  :title="preset.isDefault ? t('component.schema_presets.unset_default', { name: preset.name }) : t('component.schema_presets.set_default', { name: preset.name })"
                  @click="toggleDefault(preset)"
                >
                  <Icon :class="{ 'schema-presets__star': preset.isDefault }" :icon="preset.isDefault ? 'lucide:star' : 'lucide:star-off'" />
                </XhButton>
                <XhButton
                  variant="ghost"
                  size="sm"
                  icon-only
                  :aria-label="t('component.schema_presets.remove', { name: preset.name })"
                  :title="t('component.schema_presets.remove', { name: preset.name })"
                  @click="emit('remove', preset.code)"
                >
                  <Icon icon="lucide:trash-2" />
                </XhButton>
              </li>
            </ul>
            <p v-else class="schema-presets__empty text-xs text-foreground/60">
              {{ t('component.schema_presets.none') }}
            </p>
          </section>

          <XhSeparator class="my-1" />

          <!-- 保存的就是这几项：先起名，再看筛选、排序与每页条数（右边一个条件一个标签、一行一个） -->
          <section class="flex flex-col gap-2" :aria-labelledby="currentTitleId">
            <h3 :id="currentTitleId" class="schema-presets__section">
              {{ previewing ? t('component.schema_presets.preview_title', { name: previewing.name }) : t('component.schema_presets.current') }}
            </h3>
            <dl class="schema-presets__conditions">
              <dt v-if="!previewing" class="schema-presets__name-label text-xs text-foreground/60">
                {{ t('component.schema_presets.name_label') }}
              </dt>
              <dd v-if="!previewing" class="schema-presets__name-input">
                <XInput
                  v-model:value="name"
                  name="presetName"
                  clearable
                  :max-length="NAME_MAX"
                  :aria-label="t('component.schema_presets.name_label')"
                  :placeholder="t('component.schema_presets.name_placeholder')"
                  @enter="save"
                />
              </dd>
              <template v-for="group in conditionGroups" :key="group.key">
                <dt class="text-xs text-foreground/60">
                  {{ group.label }}
                </dt>
                <dd class="schema-presets__tags">
                  <XhTagRoot
                    v-for="item in group.items"
                    :key="item"
                    variant="subtle"
                    size="sm"
                    :tone="group.tone"
                    class="schema-presets__tag"
                    :title="item"
                  >
                    <XhTagLabel>{{ item }}</XhTagLabel>
                  </XhTagRoot>
                  <span v-if="!group.items.length" class="text-xs text-foreground/60">
                    {{ t('component.schema_presets.none') }}
                  </span>
                </dd>
              </template>
            </dl>
          </section>

          <XhSeparator class="my-1" />
          <p class="schema-presets__hint text-xs text-foreground/40">
            {{ t('component.schema_presets.hint') }}
          </p>
        </div>
      </XhPopoverContent>
    </XhPopoverPositioner>
  </XhPopoverRoot>
</template>

<style scoped>
.schema-presets {
  /* 皮肤给浮层的宽高上限是 rem 值，本应用根字号 14px 会把高度压到 224px、当前条件挤到要滚动才看得到；
     与表格设置一样放宽，宽度能放下预设名与两个图标钮 */
  inline-size: 24rem;
  max-inline-size: 24rem;
  --xh-popover-max-h: 40rem;
}

/* 浮层给高度上限（窗口矮时再收）：预设清单吃剩下的高度、自己滚动，当前条件始终露出 */
.schema-presets__body {
  flex: 1 1 auto;
  min-block-size: 0;
}

.schema-presets__saved {
  flex: 1 1 auto;
  min-block-size: 0;
}

/* 名称与上面的小标题左对齐：整条清单往左挪出按钮的左内边距，悬停底色照常伸到左边（挪的是清单，按钮不会被它的滚动裁掉） */
.schema-presets__list {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  min-block-size: calc(var(--xh-control-h-sm) * 2);
  max-block-size: 16rem;
  margin-block: 0;
  margin-inline: calc(-1 * var(--xh-space-2)) 0;
  padding: 0;
  overflow: auto;
  list-style: none;
}

.schema-presets__row {
  display: flex;
  gap: var(--xh-space-1);
  align-items: center;
}

.schema-presets__apply {
  --xh-button-px: var(--xh-space-2);
  flex: 1;
  min-inline-size: 0;
  justify-content: flex-start;
}

/* 正在用的预设：名称换品牌色，后面跟一个勾 */
.schema-presets__apply[aria-current='true'] {
  --xh-button-fg: var(--xh-fg-brand);
}

.schema-presets__check {
  flex-shrink: 0;
}

.schema-presets__name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.schema-presets__star {
  color: var(--xh-fg-warning);
}

/* 正在预览的那一行：眼睛钮换品牌色 */
.schema-presets__previewing {
  --xh-button-fg: var(--xh-fg-brand);
}

.schema-presets__hint {
  margin: 0;
}

.schema-presets__empty {
  margin: 0;
}

/* 分段小标题：与表格设置的列表表头同一档 */
.schema-presets__section {
  margin: 0;
  font-size: var(--xh-font-size-xs);
  font-weight: var(--xh-font-weight-regular);
  color: var(--xh-fg-subtle);
}

/* 左边名称、右边标签：一个条件一行，标签靠右，长的截断、悬停看全文 */
.schema-presets__conditions {
  display: grid;
  grid-template-columns: max-content minmax(0, 1fr);
  gap: var(--xh-space-2) var(--xh-space-3);
  align-items: start;
  margin: 0;
}

.schema-presets__conditions dt {
  padding-block: var(--xh-space-0_5);
}

.schema-presets__tags {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
  align-items: flex-end;
  min-inline-size: 0;
  margin: 0;
}

.schema-presets__tag {
  max-inline-size: 100%;
}

/* 名称一行：标签与输入框竖直居中，输入框占满右栏 */
.schema-presets__name-label {
  align-self: center;
}

.schema-presets__name-input {
  /* 组件库的输入框缺省是控件标准宽，这里铺满右栏、与下面的标签右边对齐 */
  --xh-text-field-control-w: 100%;
  min-inline-size: 0;
  margin: 0;
}
</style>
