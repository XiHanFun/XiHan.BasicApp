<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ListFieldSchema } from './types'
import type { PersonalView, ViewSnapshot } from './useViewManager'
import {
  XhButton,
  XhFieldControl,
  XhFieldLabel,
  XhFieldRoot,
  XhPopoverContent,
  XhPopoverPositioner,
  XhPopoverRoot,
  XhPopoverTitle,
  XhPopoverTrigger,
  XhSeparator,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'
import SyncStatusBadge from '../common/SyncStatusBadge.vue'
import XInput from '../common/XInput.vue'
import { conditionSentence, describeConditions } from './condition-summary'

/**
 * 条件预设浮层：列出预设（点名称即套用，悬停看它存的条件，星标设默认，可删除），底部列出当前的筛选、排序与每页条数，起名即存成预设
 * 同名的覆盖原来那份；默认预设在打开页面时自动套用，再点星标取消
 */
defineOptions({ name: 'SchemaViewMenu' })

const props = defineProps<{
  /** 已存的预设 */
  views: PersonalView[]
  /** 当前套用的预设 */
  activeCode?: string
  /** 页面字段：把筛选与排序说成字段名与选项名 */
  fields: readonly ListFieldSchema[]
  /** 当前的筛选、排序与每页条数，即保存时存下的那份 */
  current: ViewSnapshot
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

const trimmed = computed(() => name.value.trim())
/** 与已有的同名时保存即覆盖它 */
const overwriting = computed(() => props.views.some(view => view.name === trimmed.value))
const currentSummary = computed(() => describeConditions(props.fields, props.current))

/** 当前条件按筛选、排序、每页分三组，每组一列标签 */
const conditionGroups = computed(() => [
  {
    key: 'filters',
    label: t('component.schema_views.filters'),
    items: currentSummary.value.filters,
    tone: 'brand' as const,
  },
  {
    key: 'sorts',
    label: t('component.schema_views.sorts'),
    items: currentSummary.value.sorts,
    tone: 'brand' as const,
  },
  ...(currentSummary.value.pageSize
    ? [
        {
          key: 'page-size',
          label: t('component.schema_views.page_size'),
          items: [
            t('component.schema_views.page_size_value', { n: currentSummary.value.pageSize }),
          ],
          tone: 'neutral' as const,
        },
      ]
    : []),
])

function presetHint(view: PersonalView): string {
  return conditionSentence(describeConditions(props.fields, view.snapshot))
}

function apply(code: string) {
  emit('apply', code)
  open.value = false
}

function save() {
  if (trimmed.value === '')
    return
  emit('save', trimmed.value)
  name.value = ''
  open.value = false
}

function toggleDefault(view: PersonalView) {
  emit('setDefault', view.isDefault
    ? null
    : view.code)
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
        :aria-label="t('component.schema_views.title')"
      >
        <Icon :icon="activeCode ? 'lucide:bookmark-check' : 'lucide:bookmark'" />
      </XhButton>
    </XhPopoverTrigger>
    <XhPopoverPositioner>
      <!-- 版式随表格设置：标题行带同步标记，分隔线分段，左边小字名称、右边内容 -->
      <XhPopoverContent class="schema-views">
        <div class="schema-views__body flex flex-col gap-2">
          <div class="flex items-center gap-2">
            <!-- 标题部件给浮层起名（content 的 aria-labelledby 指向它） -->
            <XhPopoverTitle class="text-base font-semibold text-foreground">
              {{ t('component.schema_views.title') }}
            </XhPopoverTitle>
            <SyncStatusBadge :synced="appStore.presetSyncEnabled" />
          </div>

          <XhSeparator class="my-1" />

          <ul v-if="views.length" class="schema-views__list">
            <li v-for="view in views" :key="view.code" class="schema-views__row">
              <XhButton
                variant="ghost"
                size="sm"
                class="schema-views__apply"
                :aria-current="view.code === activeCode ? 'true' : undefined"
                :title="presetHint(view)"
                @click="apply(view.code)"
              >
                <Icon class="schema-views__check" :class="{ 'is-active': view.code === activeCode }" icon="lucide:check" aria-hidden="true" />
                <span class="schema-views__name">{{ view.name }}</span>
              </XhButton>
              <XhButton
                variant="ghost"
                size="sm"
                icon-only
                :aria-pressed="view.isDefault ? 'true' : 'false'"
                :aria-label="view.isDefault ? t('component.schema_views.unset_default', { name: view.name }) : t('component.schema_views.set_default', { name: view.name })"
                :title="view.isDefault ? t('component.schema_views.unset_default', { name: view.name }) : t('component.schema_views.set_default', { name: view.name })"
                @click="toggleDefault(view)"
              >
                <Icon :class="{ 'schema-views__star': view.isDefault }" :icon="view.isDefault ? 'lucide:star' : 'lucide:star-off'" />
              </XhButton>
              <XhButton
                variant="ghost"
                size="sm"
                icon-only
                :aria-label="t('component.schema_views.remove', { name: view.name })"
                :title="t('component.schema_views.remove', { name: view.name })"
                @click="emit('remove', view.code)"
              >
                <Icon icon="lucide:trash-2" />
              </XhButton>
            </li>
          </ul>
          <p v-else class="schema-views__empty text-xs text-foreground/60">
            {{ t('component.schema_views.empty') }}
          </p>

          <XhSeparator class="my-1" />

          <!-- 保存的就是这几项：左边名称，右边一个条件一个标签、一行一个 -->
          <section class="flex flex-col gap-2" :aria-labelledby="currentTitleId">
            <h3 :id="currentTitleId" class="schema-views__section">
              {{ t('component.schema_views.current') }}
            </h3>
            <dl class="schema-views__conditions">
              <template v-for="group in conditionGroups" :key="group.key">
                <dt class="text-xs text-foreground/60">
                  {{ group.label }}
                </dt>
                <dd class="schema-views__tags">
                  <XhTagRoot
                    v-for="item in group.items"
                    :key="item"
                    variant="subtle"
                    size="sm"
                    :tone="group.tone"
                    class="schema-views__tag"
                    :title="item"
                  >
                    <XhTagLabel>{{ item }}</XhTagLabel>
                  </XhTagRoot>
                  <span v-if="!group.items.length" class="text-xs text-foreground/60">
                    {{ t('component.schema_views.none') }}
                  </span>
                </dd>
              </template>
            </dl>
          </section>

          <XhSeparator class="my-1" />

          <form class="schema-views__save" novalidate @submit.prevent="save">
            <XhFieldRoot class="schema-views__field">
              <XhFieldLabel class="text-xs text-foreground/60">
                {{ t('component.schema_views.name_label') }}
              </XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="name" name="viewName" clearable :max-length="NAME_MAX" :placeholder="t('component.schema_views.name_placeholder')" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhButton type="submit" size="sm" variant="solid" :disabled="trimmed === ''">
              {{ overwriting ? t('component.schema_views.overwrite') : t('component.schema_views.save') }}
            </XhButton>
          </form>
        </div>
      </XhPopoverContent>
    </XhPopoverPositioner>
  </XhPopoverRoot>
</template>

<style scoped>
.schema-views {
  /* 皮肤给浮层的宽高上限是 rem 值，本应用根字号 14px 会把高度压到 224px、保存表单挤到要滚动才看得到；
     与表格设置一样放宽，宽度能放下预设名与两个图标钮 */
  inline-size: 24rem;
  max-inline-size: 24rem;
  --xh-popover-max-h: 40rem;
}

/* 浮层给高度上限（窗口矮时再收）：预设清单吃剩下的高度、自己滚动，当前条件与保存表单始终露出 */
.schema-views__body {
  flex: 1 1 auto;
  min-block-size: 0;
}

.schema-views__list {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  min-block-size: calc(var(--xh-control-h-sm) * 2);
  max-block-size: 16rem;
  margin: 0;
  padding: 0;
  overflow: auto;
  list-style: none;
}

.schema-views__row {
  display: flex;
  gap: var(--xh-space-1);
  align-items: center;
}

.schema-views__apply {
  flex: 1;
  min-inline-size: 0;
  justify-content: flex-start;
}

.schema-views__check {
  flex-shrink: 0;
  visibility: hidden;
  color: var(--xh-fg-brand);
}

.schema-views__check.is-active {
  visibility: visible;
}

.schema-views__name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.schema-views__star {
  color: var(--xh-fg-warning);
}

.schema-views__empty {
  margin: 0;
}

/* 分段小标题：与表格设置的列表表头同一档 */
.schema-views__section {
  margin: 0;
  font-size: var(--xh-font-size-xs);
  font-weight: var(--xh-font-weight-regular);
  color: var(--xh-fg-subtle);
}

/* 左边名称、右边标签：一个条件一行，标签靠右，长的截断、悬停看全文 */
.schema-views__conditions {
  display: grid;
  grid-template-columns: max-content minmax(0, 1fr);
  gap: var(--xh-space-2) var(--xh-space-3);
  align-items: start;
  margin: 0;
}

.schema-views__conditions dt {
  padding-block: var(--xh-space-0_5);
}

.schema-views__tags {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
  align-items: flex-end;
  min-inline-size: 0;
  margin: 0;
}

.schema-views__tag {
  max-inline-size: 100%;
}

.schema-views__save {
  display: flex;
  gap: var(--xh-space-2);
  align-items: flex-end;
}

.schema-views__field {
  flex: 1;
  min-inline-size: 0;
}
</style>
