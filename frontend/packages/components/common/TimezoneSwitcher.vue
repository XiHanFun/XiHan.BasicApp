<script setup lang="ts">
import type { DropdownOption, DropdownProps } from 'naive-ui'
import { NDropdown, NSelect, useMessage } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppStore } from '~/stores'

/**
 * 时区切换组件（统一封装：选项 + 切换逻辑）。
 * - variant=select：行内下拉框（个人中心）。
 * - variant=dropdown：触发器 + 菜单（顶栏），触发器经默认插槽传入。
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

const TIMEZONES = [
  { value: 'UTC', labelKey: 'header.timezone.utc' },
  { value: 'Asia/Shanghai', labelKey: 'header.timezone.shanghai' },
  { value: 'Asia/Tokyo', labelKey: 'header.timezone.tokyo' },
  { value: 'Europe/London', labelKey: 'header.timezone.london' },
  { value: 'America/New_York', labelKey: 'header.timezone.new_york' },
  { value: 'America/Los_Angeles', labelKey: 'header.timezone.los_angeles' },
] as const

/** 当前选中：apply 取应用时区，否则取受控值（兜底应用时区） */
const current = computed(() => (props.apply ? appStore.appTimezone : (props.value ?? appStore.appTimezone)))

const selectOptions = computed(() => TIMEZONES.map(z => ({ value: z.value, label: t(z.labelKey) })))
const dropdownOptions = computed<DropdownOption[]>(() => TIMEZONES.map(z => ({ key: z.value, label: t(z.labelKey) })))

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
