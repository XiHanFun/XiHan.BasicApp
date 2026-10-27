<script setup lang="ts">
import type { Size } from '@xihan-ui/core'
import type { TextFieldType } from '@xihan-ui/headless'
import { isComposingEvent } from '@xihan-ui/core'
import {
  XhPasswordInputCapsLockIndicator,
  XhPasswordInputControl,
  XhPasswordInputInput,
  XhPasswordInputRoot,
  XhPasswordInputVisibilityTrigger,
  XhTextFieldClearTrigger,
  XhTextFieldControl,
  XhTextFieldCount,
  XhTextFieldInput,
  XhTextFieldPrefix,
  XhTextFieldRoot,
  XhTextFieldSuffix,
} from '@xihan-ui/vue'
import { computed, ref, useSlots } from 'vue'
import { useControlAttrs } from './control-attrs'

/**
 * 单行文本输入。
 *
 * 把「根 + 视觉盒 + 输入 + 清除钮 + 字数」几个部件收成一个标签——清除钮与字数在组件库里是要自己摆的部件，
 * 不是一个开关，全站几百处输入框没必要各摆一遍。回车提交也收在这里。
 *
 * 描边、底色、高度与聚焦环画在 control 部件上（Field Chrome），input 本身是透明的；
 * 不套 control 的 input 没有盒。前后缀是组件库的 prefix / suffix 部件，与输入框同排在盒内，
 * 间距由盒的 gap 给，调用方只给插槽；字数部件排在盒下方右侧。
 *
 * 密码档由组件库的 password-input 拼出：显隐钮、大写锁定提示与切换明暗后的光标复位都归它，
 * 盒尾由显隐钮占着，不出 suffix 与字数。
 *
 * 落在这个标签上的属性转交给里面那个 input：放在 XhFieldControl 里时，字段把 id 与 aria-*
 * 挂到唯一子节点上，那组属性要落在真正的控件上 label 的 for 才接得住，见 control-attrs.ts。
 */
defineOptions({ name: 'XInput', inheritAttrs: false })

const props = withDefaults(defineProps<{
  value?: string | null
  placeholder?: string
  /** 单行档按原生 type 换软键盘（email / tel / url / search）；password 档出显隐钮；textarea 档换成多行宿主 */
  type?: TextFieldType | 'textarea'
  clearable?: boolean
  /** 三态不写时随外层 Field / Form 走（字段校验出错、表单整体禁用都能落到这个盒上）；写了以本处为准 */
  disabled?: boolean
  readOnly?: boolean
  invalid?: boolean
  /** 字数上限，按字素计（一个组合 emoji 算一个字） */
  maxLength?: number
  /** 盒下方显示字数，有 maxLength 时为「已用 / 上限」，到上限换色；密码档不出 */
  showCount?: boolean
  autocomplete?: string
  /** 移动端软键盘类型；type 已表明语义（email / tel / url）时不必再写 */
  inputmode?: 'none' | 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url'
  size?: Size
  /** 多行档的固定行数；写了 autosize 时高度随内容走，以 autosize 的行数区间为准 */
  rows?: number
  /** 多行档的自动高度，给行数区间或直接给 true */
  autosize?: boolean | { minRows?: number, maxRows?: number }
}>(), {
  value: '',
  placeholder: undefined,
  type: 'text',
  clearable: false,
  disabled: undefined,
  readOnly: undefined,
  invalid: undefined,
  maxLength: undefined,
  showCount: false,
  autocomplete: undefined,
  inputmode: undefined,
  size: 'sm',
  rows: undefined,
  autosize: undefined,
})

const emit = defineEmits<{
  'update:value': [value: string]
  /** 单行输入框内回车；输入法组合中按回车是选词，不算 */
  'enter': []
  /** 经清除钮或 Escape 清空之后 */
  'clear': []
}>()

const slots = useSlots()
const { attrs, controlAttrs } = useControlAttrs()

const isMultiline = computed(() => props.type === 'textarea')

/** 交给根部件的原生 type：多行宿主不发 */
const fieldType = computed<TextFieldType | undefined>(() => (props.type === 'textarea' ? undefined : props.type))

/** 多行宿主的行数：没写就不发这个键，免得盖掉组件库按 autosize 区间给的起始行数 */
const textareaAttrs = computed(() => (props.rows === undefined ? {} : { rows: props.rows }))

const controlRef = ref<InstanceType<typeof XhPasswordInputControl> | InstanceType<typeof XhTextFieldControl> | null>(null)

/** 装着输入框的那层盒子：两档都是组件库的 control 部件 */
const hostEl = computed<HTMLElement | null>(() => (controlRef.value?.$el as HTMLElement | null) ?? null)

/** 底层的 input/textarea 元素：读光标位置、程序化聚焦这类事要用到 */
const el = computed<HTMLInputElement | HTMLTextAreaElement | null>(
  () => hostEl.value?.querySelector('input, textarea') ?? null,
)

function focus() {
  el.value?.focus()
}

function blur() {
  el.value?.blur()
}

/** 回车按下即报，长按的连发不重复报；组合中的回车是在候选框里选词 */
function isEnterSubmit(event: KeyboardEvent): boolean {
  return event.key === 'Enter' && !event.repeat && !isComposingEvent(event)
}

/** 与组件库接管 Escape 的口径一致：组合中不接、带修饰键不接 */
function isEscapeClear(event: KeyboardEvent): boolean {
  return event.key === 'Escape' && !event.ctrlKey && !event.metaKey && !event.altKey && !isComposingEvent(event)
}

/**
 * 文本档：Escape 清空由组件库接管（它的监听排在前面），这里只在它确实清了时补报 clear；
 * canClear 取自本次按键前那一帧的根状态，已含字段继承来的禁用与只读。
 */
function onTextKeydown(event: KeyboardEvent, canClear: boolean) {
  if (isEscapeClear(event)) {
    if (canClear)
      emit('clear')
    return
  }
  if (!isMultiline.value && isEnterSubmit(event))
    emit('enter')
}

/** 密码档没有清除钮部件，Escape 清空这条自己补，与文本档一致；禁用与只读看输入框上已落定的状态 */
function onPasswordKeydown(event: KeyboardEvent) {
  if (isEnterSubmit(event)) {
    emit('enter')
    return
  }
  if (!isEscapeClear(event) || !props.clearable)
    return
  const input = event.currentTarget as HTMLInputElement
  if (input.disabled || input.readOnly || input.value === '')
    return
  event.preventDefault()
  emit('update:value', '')
  emit('clear')
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
      <span v-if="slots.prefix" class="x-input__control-prefix" aria-hidden="true">
        <slot name="prefix" />
      </span>
      <XhPasswordInputInput
        :maxlength="maxLength"
        :inputmode="inputmode"
        v-bind="controlAttrs"
        @keydown="onPasswordKeydown"
      />
      <XhPasswordInputCapsLockIndicator />
      <XhPasswordInputVisibilityTrigger />
    </XhPasswordInputControl>
  </XhPasswordInputRoot>
  <XhTextFieldRoot
    v-else
    v-slot="{ canClear }"
    class="x-input"
    :class="attrs.class"
    :style="attrs.style"
    :value="value ?? ''"
    :type="fieldType"
    :placeholder="placeholder"
    :clearable="clearable"
    :disabled="disabled"
    :read-only="readOnly"
    :invalid="invalid"
    :max-length="maxLength"
    :show-count="showCount"
    :size="size"
    :auto-size="autosize"
    @update:value="(next: string) => emit('update:value', next)"
    @clear="emit('clear')"
  >
    <!-- 盒里除输入框外还排着前后缀与清除钮，点在空处时把焦点交回输入框 -->
    <XhTextFieldControl ref="controlRef" class="x-input__control" @mousedown.self.prevent="focus">
      <XhTextFieldPrefix v-if="slots.prefix">
        <slot name="prefix" />
      </XhTextFieldPrefix>
      <XhTextFieldInput
        v-if="isMultiline"
        as="textarea"
        :inputmode="inputmode"
        v-bind="{ ...textareaAttrs, ...controlAttrs }"
        @keydown="(event: KeyboardEvent) => onTextKeydown(event, canClear)"
      />
      <XhTextFieldInput
        v-else
        :autocomplete="autocomplete"
        :inputmode="inputmode"
        v-bind="controlAttrs"
        @keydown="(event: KeyboardEvent) => onTextKeydown(event, canClear)"
      />
      <XhTextFieldSuffix v-if="slots.suffix">
        <slot name="suffix" />
      </XhTextFieldSuffix>
      <!-- 按清除钮清掉值后组件库派发 clear（值变化之后、原本就空时不派发），根上原样转出 -->
      <XhTextFieldClearTrigger v-if="clearable" />
    </XhTextFieldControl>
    <XhTextFieldCount v-if="showCount" />
  </XhTextFieldRoot>
</template>

<style scoped>
.x-input {
  inline-size: 100%;
}

/* 视觉盒由组件库的 control 部件承担，这里只把它铺满整行 */
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
</style>
