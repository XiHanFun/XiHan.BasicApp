<script setup lang="ts">
import type { MenuNode } from '@xihan-ui/headless'
import type { Placement, Size } from '@xihan-ui/kernel'
import {
  XhMenuRoot,
  XhSelectContent,
  XhSelectControl,
  XhSelectIndicator,
  XhSelectItem,
  XhSelectItemIndicator,
  XhSelectItemText,
  XhSelectList,
  XhSelectPositioner,
  XhSelectRoot,
  XhSelectTrigger,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocale } from '~/hooks'
import { useAppStore } from '~/stores'
import LocaleFlag from './LocaleFlag.vue'

/**
 * 语言切换组件（统一封装：选项 + 国旗 + 切换逻辑）。
 * - variant=select：行内下拉框（偏好设置、个人中心）。
 * - variant=dropdown：触发器 + 菜单（顶栏、登录页），触发器经默认插槽传入。
 * - apply=true：直接切换并同步应用语言（appStore.locale）；否则受控，仅 emit（如个人中心的资料字段）。
 *
 * 选项里要带国旗，所以这里不走 XSelect，直接摆部件：条目与选中态各自出一面旗。
 */
defineOptions({ name: 'LocaleSwitcher' })

const props = withDefaults(defineProps<{
  /** 展示形态 */
  variant?: 'select' | 'dropdown'
  /** 受控值（v-model）；apply 为 true 时忽略，读写应用语言 */
  value?: string
  /** 为 true 时即时切换应用语言；否则受控仅 emit */
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
const { setLocale } = useLocale()
const { t } = useI18n()

const LOCALES = [
  { value: 'zh-CN', labelKey: 'header.locale.zh_cn' },
  { value: 'en-US', labelKey: 'header.locale.en_us' },
] as const

/** 当前选中：apply 取应用语言，否则取受控值（兜底应用语言） */
const current = computed(() => (props.apply ? appStore.locale : (props.value ?? appStore.locale)))

const options = computed<MenuNode[]>(() => LOCALES.map(l => ({ value: l.value, label: t(l.labelKey) })))

/** 选中值：组件库的选中值恒为数组，单选也是长度 1 */
const selected = computed(() => [current.value])

const selectStyle = computed(() =>
  props.selectWidth == null
    ? undefined
    : { inlineSize: typeof props.selectWidth === 'number' ? `${props.selectWidth}px` : props.selectWidth })

function choose(key: string) {
  if (props.apply)
    setLocale(key)
  else
    emit('update:value', key)
  emit('change', key)
}
</script>

<template>
  <XhSelectRoot
    v-if="variant === 'select'"
    :collection="options"
    :value="selected"
    :size="size"
    :style="selectStyle"
    @update:value="(v: string[]) => v[0] && choose(v[0])"
  >
    <XhSelectControl>
      <XhSelectTrigger>
        <!-- 选中态自绘：旗 + 文案；不用 ValueText，它只出纯文本 -->
        <span class="locale-item">
          <LocaleFlag :locale="current" :size="16" />
          <span>{{ options.find(o => o.value === current)?.label }}</span>
        </span>
        <XhSelectIndicator />
      </XhSelectTrigger>
    </XhSelectControl>
    <XhSelectPositioner>
      <XhSelectContent>
        <XhSelectList>
          <XhSelectItem v-for="node in options" :key="node.value" :value="node.value">
            <XhSelectItemText>
              <span class="locale-item">
                <LocaleFlag :locale="node.value" />
                <span>{{ node.label }}</span>
              </span>
            </XhSelectItemText>
            <XhSelectItemIndicator />
          </XhSelectItem>
        </XhSelectList>
      </XhSelectContent>
    </XhSelectPositioner>
  </XhSelectRoot>

  <XhMenuRoot
    v-else
    trigger-as-child
    :collection="options"
    :placement="placement"
    @select="(details: { value: string }) => choose(details.value)"
  >
    <template #trigger>
      <slot />
    </template>
    <template #item="node">
      <span class="locale-item" :class="{ 'locale-item--active': node.value === current }">
        <LocaleFlag :locale="node.value" />
        <span>{{ node.label }}</span>
      </span>
    </template>
  </XhMenuRoot>
</template>

<style scoped>
.locale-item {
  display: inline-flex;
  gap: 8px;
  align-items: center;
}

/* 当前项高亮：主色 + 加粗 */
.locale-item--active {
  color: var(--xh-fg-brand);
  font-weight: 600;
}
</style>
