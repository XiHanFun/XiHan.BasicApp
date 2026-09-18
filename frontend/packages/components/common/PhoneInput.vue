<script lang="ts" setup>
import { XhComboboxRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { defaultPhoneCountry, normalizePhone, parsePhone, phoneCountryOptions } from '~/utils'
import { useControlAttrs } from './control-attrs'
import XInput from './XInput.vue'

/**
 * 手机号码输入：国家下拉（可搜索）+ 本地号码输入，对外只吐 E.164。
 * 号码是登录身份标识，存储与登录比对都用同一个字符串，故不把国码与号码分成两个字段上行。
 *
 * 落在标签上的 id 与 aria-*：本组件是薄封装，根节点是个 div，不接住这些属性
 * （label 的 for 只对可标注元素生效，落在 div 上悄无声息地失效），故声明
 * inheritAttrs: false，改经 useControlAttrs() 转交给里面真正可聚焦的号码输入框。
 */
defineOptions({ name: 'PhoneInput', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** E.164 手机号码 */
  value?: string
  size?: 'sm' | 'md' | 'lg'
  disabled?: boolean
  placeholder?: string
}>(), { value: '', size: 'md', disabled: false, placeholder: undefined })

const emit = defineEmits<{
  'update:value': [value: string]
  'valid': [valid: boolean]
}>()

const { t, locale } = useI18n()
const { attrs, controlAttrs } = useControlAttrs()

const parsed = parsePhone(props.value)
const country = ref(parsed?.country ?? defaultPhoneCountry())
const national = ref(parsed?.national ?? '')
const query = ref('')

/**
 * 上一次自己吐出去的 update:value。
 * 父层若经 v-model 原样把它传回来，属于回声而非外部改值——否则编辑到号码暂时
 * 不成立、吐出空字符串的那一刻，父层回传的同一个空字符串会被当成外部改值，
 * 把正在输入但还没打完的号码冲掉。
 */
const lastEmitted = ref(props.value)

const options = computed(() => phoneCountryOptions(locale.value))
const filteredOptions = computed(() => {
  const keyword = query.value.trim().toLowerCase()
  return keyword ? options.value.filter(option => option.label.toLowerCase().includes(keyword)) : options.value
})

// 挂载时就把当前值的有效性吐出去：表单回填一个已存在的合法号码时，使用方也要能立即拿到校验结果，
// 不必等用户碰一下输入框。
emit('valid', parsed !== null || !props.value)

/** 外部值变化时回填（编辑表单打开、切换到另一个用户），并同步吐出新值的有效性 */
watch(() => props.value, (next) => {
  if (next === lastEmitted.value) {
    return
  }

  const result = parsePhone(next)
  if (result) {
    country.value = result.country
    national.value = result.national
  }
  else {
    national.value = ''
  }
  emit('valid', result !== null || !next)
})

function publish() {
  const e164 = normalizePhone(national.value, country.value)
  lastEmitted.value = e164 ?? ''
  emit('update:value', e164 ?? '')
  emit('valid', e164 !== null || national.value.trim() === '')
}

function setCountry(next: string) {
  country.value = next
  if (national.value) {
    publish()
  }
}

function setNational(next: string) {
  national.value = next
  publish()
}

defineExpose({ country, national, setCountry, setNational })
</script>

<template>
  <div class="xh-input-group" :class="attrs.class" :style="attrs.style">
    <XhComboboxRoot
      v-model:input-value="query"
      :collection="filteredOptions"
      :value="[country]"
      :disabled="props.disabled"
      :size="props.size"
      :aria-label="t('component.phone_input.country')"
      style="inline-size: 140px"
      @update:value="(v: string[]) => v[0] && setCountry(v[0])"
    />
    <XInput
      v-bind="controlAttrs"
      :value="national"
      :size="props.size"
      :disabled="props.disabled"
      :placeholder="props.placeholder ?? t('component.phone_input.placeholder')"
      @update:value="setNational"
    />
  </div>
</template>
