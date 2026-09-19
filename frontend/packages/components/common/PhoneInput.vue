<script lang="ts" setup>
import { XhComboboxRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { defaultPhoneCountry, normalizePhone, parsePhone, phoneCountryOptions, rememberPhoneCountry } from '~/utils'
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

/** 非空但解析不了的存量写法：把原始数字摘出来回填，让管理员能看见并手工修正 */
function rawNationalDigits(value: string | null | undefined): string {
  return value ? value.replace(/\D/g, '') : ''
}

const parsed = parsePhone(props.value)
const country = ref(parsed?.country ?? defaultPhoneCountry())
const national = ref(parsed?.national ?? rawNationalDigits(props.value))
/**
 * 国家下拉输入框里的文字。
 * 组件库只在没接 input-value 时才在挂载时填入选中项文字，这里接了（要拿到键入内容来过滤），
 * 所以得自己填；外部改值换国家、切换语言时也由下方 watch 同步，不然输入框会是空的或停在旧国家。
 */
const query = ref('')

/**
 * 上一次自己吐出去的 update:value。
 * 父层若经 v-model 原样把它传回来，属于回声而非外部改值——否则编辑到号码暂时
 * 不成立、吐出空字符串的那一刻，父层回传的同一个空字符串会被当成外部改值，
 * 把正在输入但还没打完的号码冲掉。
 */
const lastEmitted = ref(props.value)

const options = computed(() => phoneCountryOptions(locale.value))
const selectedLabel = computed(() => options.value.find(option => option.value === country.value)?.label ?? '')
const filteredOptions = computed(() => {
  const keyword = query.value.trim().toLowerCase()
  // 输入框里就是已选国家的名称（刚选完、失焦复原、挂载回填）时不算在搜索，展开下拉要能看到全部国家
  if (!keyword || query.value === selectedLabel.value) {
    return options.value
  }
  return options.value.filter(option => option.label.toLowerCase().includes(keyword))
})

watch(selectedLabel, (label) => {
  query.value = label
}, { immediate: true })

/**
 * 点国家输入框时整段选取已选国家名称：输入框平时显示着名称，不选取的话键入会插进名称中间
 * （「台灣 日本+886」）而搜不到任何国家。已在搜索中（文字不是已选名称）就不动，留着让用户移动光标。
 * 键盘 Tab 进来时浏览器本来就会整段选取，这里只补鼠标点击。
 */
function selectCountryText(event: MouseEvent) {
  const target = event.target
  if (target instanceof HTMLInputElement && target.getAttribute('role') === 'combobox' && target.value === selectedLabel.value) {
    target.select()
  }
}

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
    // 非空但解析不了（存量非 E.164 写法）：保留默认国家，回填原始数字供管理员查看/修正
    national.value = rawNationalDigits(next)
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
  rememberPhoneCountry(next)
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
  <div class="phone-input" :data-size="props.size" :class="attrs.class" :style="attrs.style" @click="selectCountryText">
    <!-- 下拉的输入行自带 12rem 最小宽度，比这里给的 140px 宽，不归零会溢出压到右侧号码框（聚焦环也跟着压上去） -->
    <XhComboboxRoot
      v-model:input-value="query"
      :collection="filteredOptions"
      :value="[country]"
      :disabled="props.disabled"
      :size="props.size"
      :aria-label="t('component.phone_input.country')"
      open-on-click
      style="inline-size: 140px; --xh-combobox-control-min-w: 0"
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

<style scoped>
/* 国家与号码分开摆、留控件间距（与认证页「验证码 + 取得验证码」一致），不拼成输入组；间距随尺寸档取同档令牌 */
.phone-input {
  /* 号码框在窄屏只分到百来像素，放开组件库输入类的 12rem 最小宽，否则会顶出容器右缘 */
  --xh-text-field-control-min-w: 0;
  --xh-text-field-input-min-w: 0;

  display: flex;
  align-items: stretch;
  gap: var(--xh-control-gap-md);
  min-width: 0;
}

.phone-input[data-size='sm'] {
  gap: var(--xh-control-gap-sm);
}

.phone-input[data-size='lg'] {
  gap: var(--xh-control-gap-lg);
}

.phone-input > :last-child {
  flex: 1;
  min-width: 0;
}
</style>
