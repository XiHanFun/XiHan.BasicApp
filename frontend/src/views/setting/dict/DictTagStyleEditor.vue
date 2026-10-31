<!-- 字典项标签样式编辑：复用取色器及菜单图标选择器，显示开关与人工覆盖保存在同一元数据中。 -->
<script setup lang="ts">
import type { DictTagStyle } from '~/components/common/dict-tag'
import { XhButton, XhSwitch } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { XColorPicker, XInput, XSelect } from '~/components'
import { renderDictTag } from '~/components/common/dict-tag'
import { VNodeRender } from '~/components/common/VNodeRender'
import { IconPicker } from '~/iconify'

defineProps<{ label: string, disabled?: boolean }>()
const { t } = useI18n()
const model = defineModel<DictTagStyle>({ required: true })
const presets = computed<Array<{ value: NonNullable<DictTagStyle['tone']> | 'auto', label: string }>>(() => [
  { value: 'auto', label: t('setting.dict.tag_style.auto') },
  { value: 'success', label: t('setting.dict.tag_style.success') },
  { value: 'info', label: t('setting.dict.tag_style.info') },
  { value: 'brand', label: t('setting.dict.tag_style.brand') },
  { value: 'warning', label: t('setting.dict.tag_style.warning') },
  { value: 'danger', label: t('setting.dict.tag_style.danger') },
  { value: 'neutral', label: t('setting.dict.tag_style.neutral') },
])
const tone = computed({
  get: () => model.value.tone ?? 'auto',
  set: (value) => {
    model.value = { ...model.value, tone: value === 'auto' ? null : value as DictTagStyle['tone'] }
  },
})
const showIcon = computed({
  get: () => model.value.showIcon !== false,
  set: (value: boolean) => { model.value = { ...model.value, showIcon: value ? null : false } },
})
const selectedIcon = computed({
  get: () => model.value.icon ?? '',
  set: (value: string) => { model.value = { ...model.value, icon: value || null } },
})
/** @param key 背景或文字字段。 @param value 颜色输入。 @returns 只更新当前编辑模型，不发送请求。 */
function setColor(key: 'backgroundColor' | 'textColor', value: unknown): void {
  model.value = { ...model.value, [key]: typeof value === 'string' && value.length ? value : null }
}
</script>

<template>
  <section class="xh-span-2 min-w-0 rounded border border-border p-3" :aria-label="t('setting.dict.tag_style.title')">
    <div class="mb-3 flex flex-wrap items-center justify-between gap-2">
      <strong class="text-sm">{{ t('setting.dict.tag_style.title') }}</strong>
      <XhButton type="button" size="sm" variant="ghost" :disabled="disabled" @click="model = {}">
        {{ t('setting.dict.tag_style.reset') }}
      </XhButton>
    </div>
    <div class="grid gap-3 sm:grid-cols-2">
      <label class="grid min-w-0 gap-1 text-sm">
        {{ t('setting.dict.tag_style.preset') }}
        <XSelect v-model:value="tone" :options="presets" :disabled="disabled" :aria-label="t('setting.dict.tag_style.preset')" />
      </label>
      <div class="flex min-w-0 flex-wrap items-center gap-3">
        <span class="text-sm">{{ t('setting.dict.tag_style.preview') }}</span><VNodeRender :content="renderDictTag(label || t('setting.dict.tag_style.sample'), model)" />
      </div>
      <div class="flex items-center gap-3">
        <span class="text-sm">{{ t('setting.dict.tag_style.show_icon') }}</span>
        <XhSwitch v-model:checked="showIcon" :disabled="disabled" :aria-label="t('setting.dict.tag_style.show_icon')" />
      </div>
      <div class="grid min-w-0 gap-1">
        <span class="text-sm">{{ t('setting.dict.tag_style.custom_icon') }}</span>
        <div class="flex min-w-0 items-center gap-2">
          <IconPicker v-model="selectedIcon" class="min-w-0 flex-1" :disabled="disabled || !showIcon" :placeholder="t('setting.dict.tag_style.pick_icon')" />
          <XhButton type="button" size="sm" variant="ghost" :disabled="disabled || !showIcon || !selectedIcon" @click="selectedIcon = ''">
            {{ t('setting.dict.tag_style.reset_icon') }}
          </XhButton>
        </div>
        <span class="break-all text-xs text-muted-foreground">{{ selectedIcon || t('setting.dict.tag_style.default_icon') }}</span>
      </div>
      <div v-for="field in [{ key: 'backgroundColor' as const, label: t('setting.dict.tag_style.background') }, { key: 'textColor' as const, label: t('setting.dict.tag_style.foreground') }]" :key="field.key" class="grid min-w-0 gap-1 sm:col-span-2">
        <span class="text-sm">{{ field.label }}</span>
        <!-- 窄屏让取色器独占一行，避免其默认宽度挤掉十六进制输入框；桌面继续横向排列。 -->
        <div class="grid min-w-0 grid-cols-[minmax(0,1fr)_auto] items-center gap-2 sm:flex">
          <XColorPicker class="col-span-2 sm:shrink-0" :value="model[field.key] ?? null" :disabled="disabled" :aria-label="t('setting.dict.tag_style.choose_color', { color: field.label })" @update:value="setColor(field.key, $event)" />
          <XInput class="min-w-0 flex-1" :value="model[field.key] ?? ''" :disabled="disabled" :aria-label="field.label" :placeholder="t('setting.dict.tag_style.hex_placeholder')" :max-length="7" @update:value="setColor(field.key, $event)" />
          <XhButton type="button" size="sm" variant="ghost" :disabled="disabled" :aria-label="t('setting.dict.tag_style.clear_color', { color: field.label })" @click="setColor(field.key, null)">
            {{ t('setting.dict.tag_style.clear') }}
          </XhButton>
        </div>
      </div>
    </div>
    <p class="mb-0 mt-2 text-xs text-muted-foreground">
      {{ t('setting.dict.tag_style.hint') }}
    </p>
  </section>
</template>
