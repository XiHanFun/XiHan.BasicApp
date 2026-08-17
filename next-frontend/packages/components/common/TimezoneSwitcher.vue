<script setup lang="ts">
import type { MenuNode } from '@xihan-ui/headless'
import type { Placement, Size } from '@xihan-ui/kernel'
import { XhComboboxRoot, XhMenuRoot } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast, useTimezoneOptions } from '~/composables'
import { useAppStore } from '~/stores'

/**
 * 时区切换组件（统一封装：选项 + 切换逻辑）。
 * - variant=select：行内可搜索下拉（个人中心）。四百多条目录必须能筛，故用 combobox 而非 select。
 * - variant=dropdown：触发器 + 菜单（顶栏），触发器经默认插槽传入。
 * 选项取自 useTimezoneOptions 的共享目录（顶栏 / 个人中心 / 编号规则同一份），不在此另行硬编码。
 * - apply=true：即时切换并同步应用时区（appStore.appTimezone，随请求头 X-Timezone 上行）并提示；否则受控，仅 emit（如个人中心的资料字段）。
 */
defineOptions({ name: 'TimezoneSwitcher' })

const props = withDefaults(defineProps<{
  /** 展示形态 */
  variant?: 'select' | 'dropdown'
  /** 受控值（v-model）；apply 为 true 时忽略，读写应用时区 */
  value?: string
  /** 为 true 时即时切换应用时区；否则受控仅 emit */
  apply?: boolean
  /** 下拉框尺寸 */
  size?: Size
  /** 下拉框宽度（数字按 px 处理） */
  selectWidth?: number | string
  /** 菜单弹出位置 */
  placement?: Placement
}>(), {
  variant: 'select',
  apply: false,
  size: 'md',
})

const emit = defineEmits<{
  'update:value': [string]
  'change': [string]
}>()

const appStore = useAppStore()
const { t } = useI18n()

const { commonOptions, loading, ensureLoaded, withCurrent } = useTimezoneOptions()

onMounted(() => {
  void ensureLoaded()
})

/** 当前选中：apply 取应用时区，否则取受控值（兜底应用时区） */
const current = computed(() => (props.apply ? appStore.appTimezone : (props.value ?? appStore.appTimezone)))

// select 形态给完整目录（可搜索，供编号规则等需要任意时区的场景）；
// dropdown 形态是顶栏点开即选，只给常用几条，四百多条没法用
const selectOptions = computed(() => withCurrent(current.value).map(zone => ({ value: zone.value, label: zone.label })))
const dropdownOptions = computed<MenuNode[]>(() =>
  commonOptions.value.map(zone => ({ value: zone.value, label: zone.label })),
)

/** 筛选串由本组件持有：combobox 只负责显示，筛哪些条目归调用方 */
const query = ref('')
const filteredOptions = computed(() => {
  const keyword = query.value.trim().toLowerCase()
  return keyword === ''
    ? selectOptions.value
    : selectOptions.value.filter(zone => zone.label.toLowerCase().includes(keyword))
})

const selected = computed(() => (current.value ? [current.value] : []))

const selectStyle = computed(() =>
  props.selectWidth == null
    ? undefined
    : { inlineSize: typeof props.selectWidth === 'number' ? `${props.selectWidth}px` : props.selectWidth })

function choose(timezone: string) {
  if (props.apply) {
    // 落库并跨端同步；请求拦截器据此发送 X-Timezone，后端按该时区换算返回时间
    appStore.setAppTimezone(timezone)
    toast.success(t('header.timezone.switch_success', { timezone }))
  }
  else {
    emit('update:value', timezone)
  }
  emit('change', timezone)
}
</script>

<template>
  <XhComboboxRoot
    v-if="variant === 'select'"
    v-model:input-value="query"
    :collection="filteredOptions"
    :value="selected"
    :disabled="loading"
    :size="size"
    :style="selectStyle"
    open-on-click
    @update:value="(v: string[]) => v[0] && choose(v[0])"
  />
  <XhMenuRoot
    v-else
    :collection="dropdownOptions"
    :placement="placement"
    @select="(details: { value: string }) => choose(details.value)"
  >
    <template #trigger>
      <slot />
    </template>
    <template #item="node">
      <span :class="{ 'timezone-item--active': node.value === current }">{{ node.label }}</span>
    </template>
  </XhMenuRoot>
</template>

<style scoped>
/* 当前项高亮：主色 + 加粗 */
.timezone-item--active {
  color: var(--xh-fg-brand);
  font-weight: 600;
}
</style>
