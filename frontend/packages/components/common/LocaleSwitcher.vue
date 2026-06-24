<script setup lang="ts">
import type { DropdownOption, DropdownProps, SelectGroupOption, SelectOption } from 'naive-ui'
import { NDropdown, NSelect } from 'naive-ui'
import { computed, h } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocale } from '~/hooks'
import { useAppStore } from '~/stores'
import LocaleFlag from './LocaleFlag.vue'

/**
 * 语言切换组件（统一封装：选项 + 国旗 + 切换逻辑）。
 * - variant=select：行内下拉框（偏好设置、个人中心）。
 * - variant=dropdown：触发器 + 菜单（顶栏、登录页），触发器经默认插槽传入。
 * - apply=true：直接切换并同步应用语言（appStore.locale）；否则受控，仅 emit（如个人中心的资料字段）。
 */
defineOptions({ name: 'LocaleSwitcher' })

const props = withDefaults(defineProps<{
  /** 展示形态 */
  variant?: 'select' | 'dropdown'
  /** 受控值（v-model）；apply 为 true 时忽略，读写应用语言 */
  value?: string
  /** 为 true 时即时切换应用语言；否则受控仅 emit */
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
const { setLocale } = useLocale()
const { t } = useI18n()

const LOCALES = [
  { value: 'zh-CN', labelKey: 'header.locale.zh_cn' },
  { value: 'en-US', labelKey: 'header.locale.en_us' },
] as const

/** 当前选中：apply 取应用语言，否则取受控值（兜底应用语言） */
const current = computed(() => (props.apply ? appStore.locale : (props.value ?? appStore.locale)))

const selectOptions = computed(() => LOCALES.map(l => ({ value: l.value, label: t(l.labelKey) })))
const dropdownOptions = computed<DropdownOption[]>(() =>
  LOCALES.map((l) => {
    const active = l.value === current.value
    return {
      key: l.value,
      icon: () => h(LocaleFlag, { locale: l.value }),
      // 当前选中项高亮：主色 + 加粗（内联样式，确保 teleport 弹层生效）
      label: () => h('span', {
        style: active ? { color: 'hsl(var(--primary))', fontWeight: 600 } : undefined,
      }, t(l.labelKey)),
    }
  }))

const selectStyle = computed(() =>
  props.selectWidth == null
    ? undefined
    : { width: typeof props.selectWidth === 'number' ? `${props.selectWidth}px` : props.selectWidth })

function choose(key: string) {
  if (props.apply)
    setLocale(key)
  else
    emit('update:value', key)
  emit('change', key)
}

/** 下拉项：国旗 + 文案 */
function renderLabel(option: SelectOption | SelectGroupOption) {
  return h('div', { class: 'flex items-center gap-2' }, [
    h(LocaleFlag, { locale: String((option as SelectOption).value ?? '') }),
    h('span', String(option.label ?? '')),
  ])
}
/** 选中态：国旗 + 文案 */
function renderTag({ option }: { option: SelectOption, handleClose: () => void }) {
  return h('div', { class: 'flex items-center gap-1.5' }, [
    h(LocaleFlag, { locale: String(option.value ?? ''), size: 16 }),
    h('span', String(option.label ?? '')),
  ])
}
</script>

<template>
  <NSelect
    v-if="variant === 'select'"
    :value="current"
    :options="selectOptions"
    :render-label="renderLabel"
    :render-tag="renderTag"
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
