<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { PersonalView } from './useViewManager'
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
} from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'
import SyncStatusBadge from '../common/SyncStatusBadge.vue'
import XInput from '../common/XInput.vue'

/**
 * 个人视图浮层：列出视图（点名称即套用，星标设默认，可删除），底部起名保存当前的筛选、排序与每页条数
 * 同名的覆盖原来那份；默认视图在打开页面时自动套用，再点星标取消
 */
defineOptions({ name: 'SchemaViewMenu' })

const props = defineProps<{
  /** 已存的视图 */
  views: PersonalView[]
  /** 当前套用的视图 */
  activeCode?: string
}>()

const emit = defineEmits<{
  apply: [code: string]
  save: [name: string]
  setDefault: [code: null | string]
  remove: [code: string]
}>()

/** 视图名称的长度上限 */
const NAME_MAX = 40

const { t } = useI18n()
const appStore = useAppStore()
const open = ref(false)
const name = ref('')

const trimmed = computed(() => name.value.trim())
/** 与已有的同名时保存即覆盖它 */
const overwriting = computed(() => props.views.some(view => view.name === trimmed.value))

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
      <XhPopoverContent class="schema-views">
        <div class="schema-views__head">
          <!-- 标题部件给浮层起名（content 的 aria-labelledby 指向它） -->
          <XhPopoverTitle class="schema-views__title">
            {{ t('component.schema_views.title') }}
          </XhPopoverTitle>
          <SyncStatusBadge :synced="appStore.tableSyncEnabled" />
        </div>

        <ul v-if="views.length" class="schema-views__list">
          <li v-for="view in views" :key="view.code" class="schema-views__row">
            <XhButton
              variant="ghost"
              size="sm"
              class="schema-views__apply"
              :aria-current="view.code === activeCode ? 'true' : undefined"
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
        <p v-else class="schema-views__empty">
          {{ t('component.schema_views.empty') }}
        </p>

        <XhSeparator />

        <form class="schema-views__save" novalidate @submit.prevent="save">
          <XhFieldRoot class="schema-views__field">
            <XhFieldLabel>{{ t('component.schema_views.name_label') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="name" name="viewName" clearable :max-length="NAME_MAX" :placeholder="t('component.schema_views.name_placeholder')" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhButton type="submit" size="sm" variant="solid" :disabled="trimmed === ''">
            {{ overwriting ? t('component.schema_views.overwrite') : t('component.schema_views.save') }}
          </XhButton>
        </form>
      </XhPopoverContent>
    </XhPopoverPositioner>
  </XhPopoverRoot>
</template>

<style scoped>
.schema-views {
  /* 皮肤给浮层的宽上限是 rem 值，显式放宽到能放下视图名与两个图标钮 */
  inline-size: 22rem;
  max-inline-size: 22rem;
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.schema-views__head {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
}

.schema-views__title {
  font-size: var(--xh-text-body-size);
  font-weight: var(--xh-text-heading-3-weight);
}

.schema-views__list {
  display: flex;
  flex-direction: column;
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
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-subtle);
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
