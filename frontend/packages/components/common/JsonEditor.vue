<script setup lang="ts">
import JsonEditorVue from 'vue3-ts-jsoneditor'
import { useAppStore } from '~/stores'

defineOptions({ name: 'XJsonEditor' })

const props = withDefaults(defineProps<{
  /** 编辑器高度，默认 400px */
  height?: string | number
  /** 显示模式 */
  mode?: 'tree' | 'text' | 'table'
  /** 只读 */
  readOnly?: boolean
  /** 暗色主题，不传则跟随系统 */
  darkTheme?: boolean
  /** 显示主菜单栏 */
  mainMenuBar?: boolean
  /** 显示导航栏 */
  navigationBar?: boolean
  /** 显示状态栏 */
  statusBar?: boolean
}>(), {
  height: 400,
  mode: 'tree',
  readOnly: false,
  darkTheme: undefined,
  mainMenuBar: true,
  navigationBar: true,
  statusBar: true,
})

const emit = defineEmits<{
  error: [error: Error]
  focus: []
  blur: []
}>()

/** 绑定 JSON 对象类型数据 */
const jsonModel = defineModel<Record<string, unknown> | unknown[] | null>('json', { default: undefined })
/** 绑定 JSON 字符串类型数据 */
const textModel = defineModel<string>('text', { default: undefined })

const appStore = useAppStore()

/** 跟随系统暗色模式，外部可通过 darkTheme prop 强制覆盖 */
const resolvedDark = computed(() => props.darkTheme ?? appStore.isDark)

const heightStyle = computed(() => {
  return typeof props.height === 'number' ? `${props.height}px` : props.height
})
</script>

<template>
  <div :style="{ height: heightStyle }" class="x-json-editor">
    <JsonEditorVue
      v-model:json="jsonModel"
      v-model:text="textModel"
      :mode="props.mode"
      :read-only="props.readOnly"
      :dark-theme="resolvedDark"
      :main-menu-bar="props.mainMenuBar"
      :navigation-bar="props.navigationBar"
      :status-bar="props.statusBar"
      @error="(e: Error) => emit('error', e)"
      @focus="() => emit('focus')"
      @blur="() => emit('blur')"
    />
  </div>
</template>

<style scoped>
.x-json-editor {
  width: 100%;
}

.x-json-editor :deep(.jse-main) {
  height: 100%;
}
</style>
