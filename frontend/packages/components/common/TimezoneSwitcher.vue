<script setup lang="ts">
import type { DropdownOption, DropdownProps } from 'naive-ui'
import { NDropdown, NSelect, useMessage } from 'naive-ui'
import { computed, h, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTimezoneOptions } from '~/composables'
import { useAppStore } from '~/stores'

/**
 * 时区切换组件（统一封装：选项 + 切换逻辑）。
 * - variant=select：行内下拉框（个人中心）。
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
  /** NSelect 尺寸 */
  size?: 'tiny' | 'small' | 'medium' | 'large'
  /** NSelect 宽度（数字按 px 处理） */
  selectWidth?: number | string
  /** NDropdown 弹出位置 */
  placement?: DropdownProps['placement']
}>(), {
  variant: 'select',
  apply: false,
  size: 'medium',
})

const emit = defineEmits<{
  'update:value': [string]
  'change': [string]
}>()

const appStore = useAppStore()
const message = useMessage()
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
const dropdownOptions = computed<DropdownOption[]>(() =>
  commonOptions.value.map((zone) => {
    const active = zone.value === current.value
    return {
      key: zone.value,
      // 当前选中项高亮：主色 + 加粗（内联样式，确保 teleport 弹层生效）
      label: () => h('span', {
        style: active ? { color: 'hsl(var(--primary))', fontWeight: 600 } : undefined,
      }, zone.label),
    }
  }))

const selectStyle = computed(() =>
  props.selectWidth == null
    ? undefined
    : { width: typeof props.selectWidth === 'number' ? `${props.selectWidth}px` : props.selectWidth })

function choose(timezone: string) {
  if (props.apply) {
    // 落库并跨端同步；请求拦截器据此发送 X-Timezone，后端按该时区换算返回时间
    appStore.setAppTimezone(timezone)
    message.success(t('header.timezone.switch_success', { timezone }))
  }
  else {
    emit('update:value', timezone)
  }
  emit('change', timezone)
}
</script>

<template>
  <NSelect
    v-if="variant === 'select'"
    :value="current || null"
    :options="selectOptions"
    :loading="loading"
    filterable
    :size="size"
    :style="selectStyle"
    @update:value="(v) => choose(String(v))"
  />
  <NDropdown
    v-else
    :options="dropdownOptions"
    :placement="placement"
    @select="(key) => choose(String(key))"
  >
    <slot />
  </NDropdown>
</template>
