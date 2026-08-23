<script setup lang="ts">
import type { Size } from '@xihan-ui/kernel'
import { XhTextFieldClearTrigger, XhTextFieldInput, XhTextFieldRoot } from '@xihan-ui/vue'
import { computed, ref, useSlots } from 'vue'
import { Icon } from '~/iconify'
import { useControlAttrs } from './control-attrs'

/**
 * 单行文本输入。
 *
 * 把「根 + 输入 + 清除钮」三个部件收成一个标签——清除钮在组件库里是要自己摆的部件，
 * 不是一个开关，全站几百处输入框没必要各摆一遍。回车提交也收在这里。
 *
 * 前缀图标与密码显隐同理：描边与底色画在 input 上而不是根节点上，图标要叠在输入框内，
 * 因而需要一层定位容器 + 给 input 让出内边距。这段布局收在这里，调用方只给插槽。
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

const revealed = ref(false)
const isMultiline = computed(() => props.type === 'textarea')
const inputType = computed(() => (props.type === 'password' && !revealed.value ? 'password' : 'text'))
const hasPrefix = computed(() => !!slots.prefix)

const boxRef = ref<HTMLElement | null>(null)

/** 底层的 input/textarea 元素：读光标位置、程序化聚焦这类事要用到 */
const el = computed<HTMLInputElement | HTMLTextAreaElement | null>(
  () => boxRef.value?.querySelector('input, textarea') ?? null,
)

function focus() {
  el.value?.focus()
}

function blur() {
  el.value?.blur()
}

defineExpose({ el, focus, blur })
</script>

<template>
  <XhTextFieldRoot
    class="x-input"
    :class="[{ 'x-input--has-prefix': hasPrefix, 'x-input--has-reveal': type === 'password' }, attrs.class]"
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
        :type="inputType"
        :autocomplete="autocomplete"
        v-bind="controlAttrs"
        @keyup.enter="emit('enter')"
      />
      <button
        v-if="type === 'password'"
        type="button"
        class="x-input__reveal"
        :aria-pressed="revealed"
        @click="revealed = !revealed"
      >
        <Icon :icon="revealed ? 'lucide:eye-off' : 'lucide:eye'" width="15" />
      </button>
      <XhTextFieldClearTrigger v-else-if="clearable" />
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

/* 密码档的显隐钮压在输入框右侧，文字要提前收住，否则长密文钻到图标底下 */
.x-input--has-reveal .x-input__box :deep([data-scope='text-field'][data-part='input']) {
  padding-inline-end: 30px;
}

.x-input__prefix {
  position: absolute;
  inset-inline-start: 9px;
  display: inline-flex;
  align-items: center;
  color: var(--xh-fg-muted);
  pointer-events: none;
}

.x-input__reveal {
  position: absolute;
  inset-inline-end: 6px;
  display: inline-flex;
  align-items: center;
  padding: 2px;
  border: 0;
  background: transparent;
  color: var(--xh-fg-muted);
  cursor: pointer;
}

.x-input__reveal:hover {
  color: var(--xh-fg-default);
}
</style>
