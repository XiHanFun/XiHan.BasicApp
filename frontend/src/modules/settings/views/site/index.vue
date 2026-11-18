<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { SiteAssetField, SiteTextField } from './use-site'
import { XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot, XhButton, XhEmptyStateAction, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldRoot, XhFormRoot, XhSpinner } from '@xihan-ui/vue'
import { computed, onMounted, useId, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XFieldLabel, XHelpTip, XInput } from '~/components'
import { toast } from '~/composables'
import { useAuthStore } from '~/stores'
import { SITE_ASSET_ACCEPT, siteAssetUrl, useSiteSettings } from './use-site'

defineOptions({ name: 'SettingsSiteView' })

const { t } = useI18n()
const auth = useAuthStore()
const formId = useId()
const editor = useSiteSettings()
const canEdit = computed(() => auth.can('settings.site.update'))
const isPlatform = computed(() => editor.settings.value?.isPlatform ?? true)

onMounted(editor.load)

/** 文字字段：分组、标签键、说明键与长文本 */
const groups = computed<Array<{ key: string, title: string, fields: Array<{ field: SiteTextField, label: string, help: string, multiline?: boolean, type?: 'url' }> }>>(() => [
  {
    key: 'basic',
    title: t('settings.site.sections.basic'),
    fields: [
      {
        field: 'name',
        label: t('settings.site.fields.name'),
        help: t('settings.site.help.name'),
      },
      {
        field: 'subtitle',
        label: t('settings.site.fields.subtitle'),
        help: t('settings.site.help.subtitle'),
      },
      {
        field: 'description',
        label: t('settings.site.fields.description'),
        help: t('settings.site.help.description'),
        multiline: true,
      },
    ],
  },
  {
    key: 'footer',
    title: t('settings.site.sections.footer'),
    fields: [
      {
        field: 'copyrightOwner',
        label: t('settings.site.fields.copyright_owner'),
        help: t('settings.site.help.copyright_owner'),
      },
      {
        field: 'copyrightUrl',
        label: t('settings.site.fields.copyright_url'),
        help: t('settings.site.help.link'),
        type: 'url',
      },
      {
        field: 'copyrightSince',
        label: t('settings.site.fields.copyright_since'),
        help: t('settings.site.help.copyright_since'),
      },
      {
        field: 'icp',
        label: t('settings.site.fields.icp'),
        help: t('settings.site.help.icp'),
      },
      {
        field: 'icpUrl',
        label: t('settings.site.fields.icp_url'),
        help: t('settings.site.help.link'),
        type: 'url',
      },
    ],
  },
])

/** 图片槽位：当前图与沿用的平台图 */
const images = computed<Array<{ field: SiteAssetField, label: string, help: string, wide?: boolean }>>(() => [
  {
    field: 'logo',
    label: t('settings.site.fields.logo'),
    help: t('settings.site.help.logo'),
  },
  {
    field: 'favicon',
    label: t('settings.site.fields.favicon'),
    help: t('settings.site.help.favicon'),
  },
  {
    field: 'loginBackground',
    label: t('settings.site.fields.login_background'),
    help: t('settings.site.help.login_background'),
    wide: true,
  },
])

function imageOf(field: SiteAssetField): null | string {
  return siteAssetUrl(editor.settings.value?.own[field])
}

function inheritedImageOf(field: SiteAssetField): null | string {
  return siteAssetUrl(editor.settings.value?.inherited?.[field])
}

const fileInput = useTemplateRef<HTMLInputElement>('fileInput')
let picking: null | SiteAssetField = null

function pick(field: SiteAssetField): void {
  picking = field
  fileInput.value?.click()
}

async function onFileSelected(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || !picking)
    return
  if (await editor.upload(picking, file))
    toast.success(t('settings.site.uploaded'))
  else if (editor.submitError.value)
    toast.danger(editor.submitError.value)
}

async function onRemove(field: SiteAssetField): Promise<void> {
  if (await editor.remove(field))
    toast.success(t('settings.site.removed'))
  else if (editor.submitError.value)
    toast.danger(editor.submitError.value)
}

async function onSubmit(): Promise<void> {
  if (await editor.save())
    toast.success(t('settings.site.saved'))
}
</script>

<template>
  <div class="site-settings">
    <div class="xh-loading-stage" :class="{ 'is-loading': editor.loading.value }">
      <div class="xh-loading-stage__veil">
        <XhSpinner />
      </div>

      <XhEmptyStateRoot v-if="editor.loadError.value" class="site-settings__empty">
        <XhEmptyStateIndicator>
          <Icon icon="lucide:alert-circle" />
        </XhEmptyStateIndicator>
        <XhEmptyStateTitle>{{ t('settings.site.load_failed') }}</XhEmptyStateTitle>
        <XhEmptyStateDescription>{{ editor.loadError.value }}</XhEmptyStateDescription>
        <XhEmptyStateAction>
          <XhButton variant="subtle" size="sm" @click="editor.load">
            {{ t('settings.site.retry') }}
          </XhButton>
        </XhEmptyStateAction>
      </XhEmptyStateRoot>

      <div v-else-if="editor.settings.value" class="site-settings__body" data-testid="site-settings">
        <XhAlertRoot :tone="isPlatform ? 'info' : 'neutral'" variant="subtle">
          <XhAlertIndicator>
            <Icon :icon="isPlatform ? 'lucide:globe' : 'lucide:building-2'" />
          </XhAlertIndicator>
          <XhAlertContent>
            <XhAlertDescription>
              {{ isPlatform ? t('settings.site.platform_hint') : t('settings.site.tenant_hint') }}
            </XhAlertDescription>
          </XhAlertContent>
        </XhAlertRoot>

        <section class="site-settings__card">
          <h3 class="site-settings__title">
            {{ t('settings.site.sections.images') }}
          </h3>
          <input ref="fileInput" class="site-settings__file" type="file" :accept="SITE_ASSET_ACCEPT" :aria-label="t('settings.site.upload')" @change="onFileSelected">
          <div class="site-settings__images">
            <div v-for="image in images" :key="image.field" class="site-settings__image" :class="{ 'is-wide': image.wide }">
              <!-- 图片不是表单字段：字段名与问号提示并排写在这里，不用依赖字段上下文的标签 -->
              <span class="site-settings__label">
                <span>{{ image.label }}</span>
                <XHelpTip :content="image.help" />
              </span>
              <div class="site-settings__preview" :class="{ 'is-wide': image.wide }">
                <img v-if="imageOf(image.field)" :src="imageOf(image.field)!" :alt="image.label">
                <img v-else-if="inheritedImageOf(image.field)" :src="inheritedImageOf(image.field)!" :alt="image.label" class="is-inherited">
                <span v-else class="site-settings__placeholder">{{ t('settings.site.no_image') }}</span>
              </div>
              <span v-if="!imageOf(image.field) && inheritedImageOf(image.field)" class="site-settings__hint">{{ t('settings.site.inherited_image') }}</span>
              <div v-if="canEdit" class="site-settings__image-actions">
                <XhButton size="sm" variant="subtle" :loading="editor.uploading.value === image.field" :disabled="editor.uploading.value !== null" @click="pick(image.field)">
                  {{ imageOf(image.field) ? t('settings.site.replace') : t('settings.site.upload') }}
                </XhButton>
                <XhButton v-if="imageOf(image.field)" size="sm" variant="ghost" tone="danger" :disabled="editor.uploading.value !== null" @click="onRemove(image.field)">
                  {{ t('settings.site.remove') }}
                </XhButton>
              </div>
            </div>
          </div>
        </section>

        <XhFormRoot :id="formId" class="site-settings__form" novalidate @submit="onSubmit">
          <section v-for="group in groups" :key="group.key" class="site-settings__card">
            <h3 class="site-settings__title">
              {{ group.title }}
            </h3>
            <div class="xh-edit-form-grid">
              <XhFieldRoot v-for="item in group.fields" :key="item.field" :invalid="!!editor.fieldError(item.field)" :class="{ 'site-settings__wide': item.multiline }">
                <XFieldLabel :help="item.help">
                  {{ item.label }}
                </XFieldLabel>
                <XhFieldControl>
                  <XInput
                    v-model:value="editor.form[item.field]"
                    :name="item.field"
                    :type="item.multiline ? 'textarea' : item.type"
                    :rows="item.multiline ? 2 : undefined"
                    :placeholder="editor.inherited(item.field)"
                    :disabled="!canEdit"
                    clearable
                  />
                </XhFieldControl>
                <XhFieldErrorText v-if="editor.fieldError(item.field)">
                  {{ editor.fieldError(item.field) }}
                </XhFieldErrorText>
              </XhFieldRoot>
            </div>
          </section>

          <p v-if="editor.submitError.value" class="site-settings__error" role="alert">
            {{ editor.submitError.value }}
          </p>
          <div v-if="canEdit" class="site-settings__actions">
            <XhButton type="submit" :form="formId" :loading="editor.saving.value">
              {{ t('settings.site.save') }}
            </XhButton>
          </div>
        </XhFormRoot>
      </div>
    </div>
  </div>
</template>

<style scoped>
.site-settings {
  padding: var(--xh-space-3);
}

.site-settings__empty {
  padding-block: var(--xh-space-8);
}

.site-settings__body,
.site-settings__form {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

/* 卡片之间只隔外层的 gap：表单根给直接子项的项距在这里不要，否则表单里的两张卡片隔得比图片卡远 */
.site-settings__card {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
  margin: 0;
  padding: var(--xh-space-4);
  border: var(--xh-stroke-thin) solid var(--xh-border-subtle);
  border-radius: var(--xh-shape-surface);
  background: var(--xh-bg-surface);
}

.site-settings__title {
  margin: 0;
  font-size: var(--xh-text-body-size);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.site-settings__file {
  display: none;
}

.site-settings__label {
  display: flex;
  gap: var(--xh-space-1);
  align-items: center;
  font-size: var(--xh-text-secondary-size);
  font-weight: var(--xh-font-weight-medium);
  color: var(--xh-fg-default);
}

.site-settings__images {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 12rem), 1fr));
  gap: var(--xh-space-4);
}

.site-settings__image {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.site-settings__image.is-wide {
  grid-column: span 2;
}

.site-settings__preview {
  display: flex;
  align-items: center;
  justify-content: center;
  block-size: 6rem;
  border: var(--xh-stroke-thin) dashed var(--xh-border-default);
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-subtle);
  overflow: hidden;
}

.site-settings__preview img {
  max-block-size: 100%;
  max-inline-size: 100%;
  object-fit: contain;
}

.site-settings__preview.is-wide img {
  inline-size: 100%;
  block-size: 100%;
  object-fit: cover;
}

.site-settings__preview img.is-inherited {
  opacity: 0.6;
}

.site-settings__placeholder,
.site-settings__hint {
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-muted);
}

.site-settings__image-actions,
.site-settings__actions {
  display: flex;
  gap: var(--xh-space-2);
}

.site-settings__actions {
  justify-content: flex-end;
}

.site-settings__wide {
  grid-column: 1 / -1;
}

.site-settings__error {
  margin: 0;
  color: var(--xh-fg-danger);
}

@media (width <= 40rem) {
  .site-settings__image.is-wide {
    grid-column: auto;
  }
}
</style>
