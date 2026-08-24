<script setup lang="ts">
import type { Size } from '@xihan-ui/kernel'
import {
  XhPasswordInputCapsLockIndicator,
  XhPasswordInputControl,
  XhPasswordInputInput,
  XhPasswordInputRoot,
  XhPasswordInputVisibilityTrigger,
  XhTextFieldClearTrigger,
  XhTextFieldInput,
  XhTextFieldRoot,
} from '@xihan-ui/vue'
import { computed, ref, useSlots } from 'vue'
import { useControlAttrs } from './control-attrs'

/**
 * 单行文本输入。
 *
 * 把「根 + 输入 + 清除钮」三个部件收成一个标签——清除钮在组件库里是要自己摆的部件，
 * 不是一个开关，全站几百处输入框没必要各摆一遍。回车提交也收在这里。
 *
 * 前缀图标叠在输入框内：描边与底色画在 input 上而不是根节点上，因而需要一层定位容器 +
 * 给 input 让出内边距。这段布局收在这里，调用方只给插槽。
 *
 * 密码档由组件库的 password-input 拼出：显隐钮、大写锁定提示与切换明暗后的光标复位都归它。
 *
 * 落在这个标签上的属性转交给里面那个 input：放在 XhFieldControl 里时，字段把 id 与 aria-*
 * 挂到唯一子节点上，那组属性要落在真正的控件上 label 的 for 才接得住。只有 data-scope /
 * data-part 例外——它们会盖掉部件自己的角色标记，让皮肤整条选不中。
 */
defineOptions({ name: 'XInput', inheritAttrs: false })

const props = withDefaults(defineProps<{
  value?: string | null
  placeholder?: string
  /** 密码档会在右侧出一个显隐钮；textarea 档换成多行宿主 */
  type?: 'text' | 'password' | 'textarea'
  clearable?: boolean
  disabled?: boolean
  readOnly?: boolean
  invalid?: boolean
  maxLength?: number
  autocomplete?: string
  size?: Size
  /** 多行档的自动高度，给行数区间或直接给 true */
  autosize?: boolean | { minRows?: number, maxRows?: number }
}>(), {
  value: '',
  placeholder: undefined,
  type: 'text',
  clearable: false,
  disabled: false,
  readOnly: false,
  invalid: false,
  maxLength: undefined,
  autocomplete: undefined,
  size: 'sm',
  autosize: undefined,
})

const emit = defineEmits<{
  'update:value': [value: string]
  /** 输入框内回车 */
  'enter': []
}>()

const slots = useSlots()
const { attrs, controlAttrs } = useControlAttrs()

const isMultiline = computed(() => props.type === 'textarea')
const hasPrefix = computed(() => !!slots.prefix)

const boxRef = ref<HTMLElement | null>(null)
const controlRef = ref<InstanceType<typeof XhPasswordInputControl> | null>(null)

/** 装着输入框的那层盒子：文本档是自绘的容器，密码档是组件库的 control 部件 */
const hostEl = computed<HTMLElement | null>(() => boxRef.value ?? (controlRef.value?.$el as HTMLElement | null) ?? null)

/** 底层的 input/textarea 元素：读光标位置、程序化聚焦这类事要用到 */
const el = computed<HTMLInputElement | HTMLTextAreaElement | null>(
  () => hostEl.value?.querySelector('input, textarea') ?? null,
)

function focus() {
  el.value?.focus()
}

/** 密码档没有清除钮部件，Escape 清空这条自己补，与文本档一致 */
function clearOnEscape() {
  if (props.clearable && !props.disabled && !props.readOnly && (props.value ?? '') !== '')
    emit('update:value', '')
}

function blur() {
  el.value?.blur()
}

defineExpose({ el, focus, blur })
</script>

<template>
  <XhPasswordInputRoot
    v-if="type === 'password'"
    class="x-input"
    :class="attrs.class"
    :style="attrs.style"
    :value="value ?? ''"
    :placeholder="placeholder"
    :disabled="disabled"
    :read-only="readOnly"
    :invalid="invalid"
    :size="size"
    :auto-complete="autocomplete"
    @update:value="(next: string) => emit('update:value', next)"
  >
    <!-- 盒里除输入框外还排着提示与显隐钮，点在空处时把焦点交回输入框 -->
    <XhPasswordInputControl ref="controlRef" class="x-input__control" @mousedown.self.prevent="focus">
      <span v-if="hasPrefix" class="x-input__control-prefix" aria-hidden="true">
        <slot name="prefix" />
      </span>
      <XhPasswordInputInput
        :maxlength="maxLength"
        v-bind="controlAttrs"
        @keyup.enter="emit('enter')"
        @keydown.esc="clearOnEscape"
      />
      <XhPasswordInputCapsLockIndicator />
      <XhPasswordInputVisibilityTrigger />
    </XhPasswordInputControl>
  </XhPasswordInputRoot>
  <XhTextFieldRoot
    v-else
    class="x-input"
    :class="[{ 'x-input--has-prefix': hasPrefix }, attrs.class]"
    :style="attrs.style"
    :value="value ?? ''"
    :placeholder="placeholder"
    :clearable="clearable"
    :disabled="disabled"
    :read-only="readOnly"
    :invalid="invalid"
    :max-length="maxLength"
    :size="size"
    :auto-size="autosize"
    @update:value="(next: string) => emit('update:value', next)"
  >
    <div ref="boxRef" class="x-input__box">
      <span v-if="hasPrefix" class="x-input__prefix" aria-hidden="true">
        <slot name="prefix" />
      </span>
      <XhTextFieldInput
        v-if="isMultiline"
        as="textarea"
        v-bind="controlAttrs"
      />
      <XhTextFieldInput
        v-else
        type="text"
        :autocomplete="autocomplete"
        v-bind="controlAttrs"
        @keyup.enter="emit('enter')"
      />
      <XhTextFieldClearTrigger v-if="clearable" />
    </div>
  </XhTextFieldRoot>
</template>

<style scoped>
.x-input {
  inline-size: 100%;
}

/* 输入框与叠在其上的前缀/后缀共处一个定位上下文 */
.x-input__box {
  position: relative;
  display: flex;
  align-items: center;
  inline-size: 100%;
}

.x-input__box :deep([data-scope='text-field'][data-part='input']) {
  inline-size: 100%;
}

/* 有前缀时给输入框让出左侧位置 */
.x-input--has-prefix .x-input__box :deep([data-scope='text-field'][data-part='input']) {
  padding-inline-start: 30px;
}

/* 密码档的视觉盒由组件库的 control 部件承担，这里只把它铺满整行 */
.x-input__control {
  inline-size: 100%;
}

/* 密码档的前缀图标排在盒内，与输入框、显隐钮同为一行上的分段 */
.x-input__control-prefix {
  display: inline-flex;
  flex: none;
  align-items: center;
  color: var(--xh-fg-muted);
  pointer-events: none;
}

.x-input__prefix {
  position: absolute;
  inset-inline-start: 9px;
  display: inline-flex;
  align-items: center;
  color: var(--xh-fg-muted);
  pointer-events: none;
}
</style>
