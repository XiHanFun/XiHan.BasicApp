<script lang="ts" setup>
import { XhComboboxRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { defaultPhoneCountry, normalizePhone, parsePhone, phoneCountryOptions } from '~/utils'
import XInput from './XInput.vue'

/**
 * 手机号码输入：国家下拉（可搜索）+ 本地号码输入，对外只吐 E.164。
 * 号码是登录身份标识，存储与登录比对都用同一个字符串，故不把国码与号码分成两个字段上行。
 */
defineOptions({ name: 'PhoneInput' })

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
const parsed = parsePhone(props.value)
const country = ref(parsed?.country ?? defaultPhoneCountry())
const national = ref(parsed?.national ?? '')
const query = ref('')

const options = computed(() => phoneCountryOptions(locale.value))
const filteredOptions = computed(() => {
  const keyword = query.value.trim().toLowerCase()
  return keyword ? options.value.filter(option => option.label.toLowerCase().includes(keyword)) : options.value
})

/** 外部值变化时回填（编辑表单打开、语言切换不影响已选国家） */
watch(() => props.value, (next) => {
  const result = parsePhone(next)
  if (result) {
    country.value = result.country
    national.value = result.national
  }
  else if (!next) {
    national.value = ''
  }
})

function publish() {
  const e164 = normalizePhone(national.value, country.value)
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
  <div class="xh-input-group">
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
      :value="national"
      :size="props.size"
      :disabled="props.disabled"
      :placeholder="props.placeholder ?? t('component.phone_input.placeholder')"
      @update:value="setNational"
    />
  </div>
</template>
