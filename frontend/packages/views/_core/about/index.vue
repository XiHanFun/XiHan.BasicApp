<script lang="ts" setup>
import type { ResponseOf } from '~/contracts'
import { XhCardContent, XhCardHeader, XhCardRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'
import { server as serverApi } from '~/contracts'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { useAuthStore } from '~/stores'
import PackageJson from '../../../../package.json'

defineOptions({ name: 'AboutPage' })

const { t } = useI18n()
const auth = useAuthStore()

// package.json 里的依赖值是 pnpm 的 catalog: 协议串，真实版本由构建期解析后注入
const dependencies = __APP_DEPENDENCIES__
const devDependencies = __APP_DEV_DEPENDENCIES__
const keywords = PackageJson.keyword

const appName = __APP_NAME__
const appLogo = import.meta.env.VITE_APP_LOGO || '/favicon.png'
const version = __APP_VERSION__
const lastBuildTime = __APP_BUILD_TIME__
const license = PackageJson.license
const repository = __APP_REPOSITORY__
const author = PackageJson.author.name

const DOCS_URL = 'https://docs.xihanfun.com'

/** 一句话介绍按语言取；package.json 的 description 只给包元数据 */
const description = computed(() => t('component.about.description'))
/** 后端依赖清单要服务监控的读权限（平台侧）：租户侧没有这一块，不发请求也不留空卡片 */
const canReadBackendDependencies = computed(() => auth.can('maintenance.server.read'))

const dependencyCount = Object.keys(dependencies).length
const devDependencyCount = Object.keys(devDependencies).length
const overviewItems = computed<Array<{ icon: string, label: string, value: string, subValue: string, href?: string }>>(() => [
  {
    icon: 'lucide:user-round',
    label: t('page.about.name_author'),
    value: appName,
    subValue: author,
  },
  {
    icon: 'lucide:package',
    label: `${t('page.about.version')} / ${t('page.about.license')}`,
    value: `v${version}`,
    subValue: license,
  },
  {
    icon: 'lucide:calendar-clock',
    label: t('page.about.release_time'),
    value: lastBuildTime,
    subValue: t('component.about.sub_build_time'),
  },
  {
    icon: 'lucide:book-open-text',
    label: t('page.about.docs'),
    value: DOCS_URL,
    subValue: t('component.about.sub_docs'),
    href: DOCS_URL,
  },
  {
    icon: 'lucide:github',
    label: t('page.about.repository'),
    value: repository,
    subValue: t('component.about.sub_source_repo'),
    href: repository,
  },
])

const coreCapabilities = computed(() => [
  {
    icon: 'lucide:shield-check',
    title: t('component.about.cap_identity_title'),
    desc: t('component.about.cap_identity_desc'),
  },
  {
    icon: 'lucide:file-code-2',
    title: t('component.about.cap_contract_title'),
    desc: t('component.about.cap_contract_desc'),
  },
  {
    icon: 'lucide:blocks',
    title: t('component.about.cap_packages_title'),
    desc: t('component.about.cap_packages_desc'),
  },
  {
    icon: 'lucide:radio',
    title: t('component.about.cap_realtime_title'),
    desc: t('component.about.cap_realtime_desc'),
  },
])

const governanceItems = computed(() => [
  {
    icon: 'lucide:cpu',
    label: t('component.about.feature_runtime_label'),
    value: t('component.about.feature_runtime_value'),
  },
  {
    icon: 'lucide:database',
    label: t('component.about.feature_data_label'),
    value: t('component.about.feature_data_value'),
  },
  {
    icon: 'lucide:layout-dashboard',
    label: t('component.about.feature_frontend_label'),
    value: t('component.about.feature_frontend_value'),
  },
  {
    icon: 'lucide:globe',
    label: t('component.about.feature_i18n_label'),
    value: t('component.about.feature_i18n_value'),
  },
  {
    icon: 'lucide:layers',
    label: t('component.about.feature_architecture_label'),
    value: t('component.about.feature_architecture_value'),
  },
])

const backendDependencies = ref<ResponseOf<'server.packages'>['items']>([])

/** 页面人人可进；有权限才取后端依赖清单，取失败才提示 */
async function fetchBackendDependencies() {
  if (!canReadBackendDependencies.value)
    return
  try {
    backendDependencies.value = (await serverApi.packages({})).items
  }
  catch (error) {
    toast.danger((error instanceof ApiError
      ? error.detail
      : (error as Error).message) || t('common.messages.load_failed'))
  }
}

onMounted(() => {
  void fetchBackendDependencies()
})
</script>

<template>
  <div class="ab-page">
    <div class="ab-banner">
      <div class="ab-banner-glow" />
      <div class="ab-banner-title">
        <Icon icon="lucide:shield-check" width="18" />
        <span>{{ t('page.about.title') }}</span>
      </div>
      <div class="ab-hero">
        <div class="ab-logo">
          <img :src="appLogo" :alt="appName" class="ab-logo-img">
        </div>
        <div class="ab-hero-main">
          <div class="ab-hero-line1" :title="`${appName} | ${description}`">
            <span class="ab-hero-name">{{ appName }}</span>
            <span class="ab-hero-sep">|</span>
            <span class="ab-hero-desc">{{ description }}</span>
          </div>
          <div class="ab-hero-line2">
            <XhTagRoot variant="subtle" size="sm" tone="brand">
              <XhTagLabel>
                {{ t('component.about.tag_multi_tenant') }}
              </XhTagLabel>
            </XhTagRoot>
            <XhTagRoot variant="subtle" size="sm" tone="success">
              <XhTagLabel>
                {{ t('component.about.tag_contract_driven') }}
              </XhTagLabel>
            </XhTagRoot>
            <XhTagRoot variant="subtle" size="sm" tone="info">
              <XhTagLabel>
                {{ t('component.about.tag_capability_packages') }}
              </XhTagLabel>
            </XhTagRoot>
          </div>
        </div>
      </div>
      <div class="ab-overview-grid">
        <div v-for="item in overviewItems" :key="item.label" class="ab-overview-item">
          <div class="ab-overview-icon">
            <Icon :icon="item.icon" width="18" />
          </div>
          <div class="ab-overview-body">
            <div class="ab-overview-label">
              {{ item.label }}
            </div>
            <div class="ab-overview-value" :title="item.value">
              <a
                v-if="item.href"
                :href="item.href"
                target="_blank"
                rel="noopener noreferrer"
                class="ab-overview-link"
              >
                {{ item.value }}
              </a>
              <template v-else>
                {{ item.value }}
              </template>
            </div>
            <div class="ab-overview-sub" :title="item.subValue">
              {{ item.subValue }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <XhCardRoot variant="ghost" class="ab-card">
      <XhCardHeader>
        <div class="ab-card-header">
          <Icon icon="lucide:sparkles" width="16" />
          <span>{{ t('component.about.core_capabilities') }}</span>
        </div>
      </XhCardHeader>
      <XhCardContent>
        <div class="ab-cap-grid">
          <div v-for="item in coreCapabilities" :key="item.title" class="ab-cap-item">
            <div class="ab-cap-icon">
              <Icon :icon="item.icon" width="16" />
            </div>
            <div class="ab-cap-content">
              <div class="ab-cap-title">
                {{ item.title }}
              </div>
              <div class="ab-cap-desc">
                {{ item.desc }}
              </div>
            </div>
          </div>
        </div>
      </XhCardContent>
    </XhCardRoot>

    <XhCardRoot variant="ghost" class="ab-card">
      <XhCardHeader>
        <div class="ab-card-header">
          <Icon icon="lucide:boxes" width="16" />
          <span>{{ t('component.about.platform_features') }}</span>
        </div>
      </XhCardHeader>
      <XhCardContent>
        <div class="ab-governance">
          <div v-for="item in governanceItems" :key="item.label" class="ab-gov-item">
            <div class="ab-gov-left">
              <div class="ab-gov-icon">
                <Icon :icon="item.icon" width="14" />
              </div>
              <span class="ab-gov-label">{{ item.label }}</span>
            </div>
            <span class="ab-gov-value">{{ item.value }}</span>
          </div>
        </div>
      </XhCardContent>
    </XhCardRoot>

    <XhCardRoot v-if="canReadBackendDependencies" variant="ghost" class="ab-card">
      <XhCardHeader>
        <div class="ab-card-header">
          <Icon icon="lucide:box" width="16" />
          <span>{{ t('component.about.backend_dependencies') }}</span>
          <XhTagRoot variant="subtle" size="sm" tone="info" class="ab-pkg-count">
            <XhTagLabel>
              {{ backendDependencies.length }}
            </XhTagLabel>
          </XhTagRoot>
        </div>
      </XhCardHeader>
      <XhCardContent>
        <div v-if="!backendDependencies.length" class="ab-empty">
          {{ t('common.no_data') }}
        </div>
        <div v-else class="ab-pkg-grid">
          <div v-for="pkg in backendDependencies" :key="pkg.packageName" class="ab-pkg-item">
            <Icon icon="lucide:package" width="14" class="ab-pkg-icon" />
            <span class="ab-pkg-name" :title="pkg.packageName">
              {{ pkg.packageName }}
            </span>
            <XhTagRoot variant="subtle" tone="success" size="sm" class="ab-pkg-ver">
              <XhTagLabel>
                {{ pkg.packageVersion }}
              </XhTagLabel>
            </XhTagRoot>
          </div>
        </div>
      </XhCardContent>
    </XhCardRoot>

    <XhCardRoot variant="ghost" class="ab-card">
      <XhCardHeader>
        <div class="ab-card-header">
          <Icon icon="lucide:box" width="16" />
          <span>{{ t('component.about.frontend_dependencies') }}</span>
          <XhTagRoot variant="subtle" size="sm" tone="info" class="ab-pkg-count">
            <XhTagLabel>
              {{ dependencyCount }}
            </XhTagLabel>
          </XhTagRoot>
        </div>
      </XhCardHeader>
      <XhCardContent>
        <div v-if="!dependencyCount" class="ab-empty">
          {{ t('common.no_data') }}
        </div>
        <div v-else class="ab-pkg-grid">
          <div v-for="(value, key) in dependencies" :key="String(key)" class="ab-pkg-item">
            <Icon icon="lucide:package" width="14" class="ab-pkg-icon" />
            <span class="ab-pkg-name" :title="String(key)">
              {{ key }}
            </span>
            <XhTagRoot variant="subtle" tone="success" size="sm" class="ab-pkg-ver">
              <XhTagLabel>
                {{ value }}
              </XhTagLabel>
            </XhTagRoot>
          </div>
        </div>
      </XhCardContent>
    </XhCardRoot>

    <XhCardRoot variant="ghost" class="ab-card">
      <XhCardHeader>
        <div class="ab-card-header">
          <Icon icon="lucide:wrench" width="16" />
          <span>{{ t('component.about.frontend_dev_dependencies') }}</span>
          <XhTagRoot variant="subtle" size="sm" tone="info" class="ab-pkg-count">
            <XhTagLabel>
              {{ devDependencyCount }}
            </XhTagLabel>
          </XhTagRoot>
        </div>
      </XhCardHeader>
      <XhCardContent>
        <div v-if="!devDependencyCount" class="ab-empty">
          {{ t('common.no_data') }}
        </div>
        <div v-else class="ab-pkg-grid">
          <div v-for="(value, key) in devDependencies" :key="String(key)" class="ab-pkg-item">
            <Icon icon="lucide:package" width="14" class="ab-pkg-icon" />
            <span class="ab-pkg-name" :title="String(key)">
              {{ key }}
            </span>
            <XhTagRoot variant="subtle" tone="danger" size="sm" class="ab-pkg-ver">
              <XhTagLabel>
                {{ value }}
              </XhTagLabel>
            </XhTagRoot>
          </div>
        </div>
      </XhCardContent>
    </XhCardRoot>

    <XhCardRoot variant="ghost" class="ab-card">
      <XhCardHeader>
        <div class="ab-card-header">
          <Icon icon="lucide:tag" width="16" />
          <span>{{ t('page.about.keywords') }}</span>
        </div>
      </XhCardHeader>
      <XhCardContent>
        <div class="ab-keywords">
          <XhTagRoot
            v-for="(value, index) in keywords"
            :key="String(value)"
            variant="subtle"
            size="sm"
            tone="brand"
          >
            <XhTagLabel>
              #{{ index + 1 }} {{ value }}
            </XhTagLabel>
          </XhTagRoot>
          <span v-if="keywords.length === 0" class="about-empty-hint">
            {{ t('page.about.empty_keywords') }}
          </span>
        </div>
      </XhCardContent>
    </XhCardRoot>
  </div>
</template>

<style scoped>
.about-empty-hint {
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-sm);
}

.ab-page {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.ab-banner {
  position: relative;
  overflow: hidden;
  padding: var(--xh-space-4) var(--xh-space-5);
  border-radius: var(--xh-shape-surface);
  background:
    radial-gradient(circle at 85% 0%, color-mix(in oklab, var(--xh-bg-brand) 16%, transparent), transparent 45%),
    linear-gradient(135deg, var(--xh-bg-subtle), var(--xh-bg-muted));
  border: 1px solid var(--xh-border-default);
}

.ab-banner-glow {
  position: absolute;
  inset-inline-end: calc(var(--xh-space-8) * -2);
  top: calc(var(--xh-space-8) * -2);
  width: calc(var(--xh-space-8) * 6);
  height: calc(var(--xh-space-8) * 6);
  border-radius: var(--xh-shape-circle);
  background: color-mix(in oklab, var(--xh-bg-brand) 12%, transparent);
  filter: blur(var(--xh-space-5));
  pointer-events: none;
}

.ab-banner-title {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  margin-bottom: var(--xh-space-3);
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.ab-hero {
  display: flex;
  align-items: center;
  gap: var(--xh-space-3);
  margin-bottom: var(--xh-space-3);
}

.ab-logo {
  width: calc(var(--xh-space-8) + var(--xh-space-6));
  height: calc(var(--xh-space-8) + var(--xh-space-6));
  border-radius: var(--xh-shape-surface);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background: var(--xh-bg-brand-subtle);
  border: 1px solid color-mix(in oklab, var(--xh-bg-brand) 25%, transparent);
  overflow: hidden;
}

.ab-logo-img {
  width: calc(var(--xh-space-8) + var(--xh-space-2));
  height: calc(var(--xh-space-8) + var(--xh-space-2));
  object-fit: contain;
}

.ab-hero-main {
  min-width: 0;
}

.ab-hero-line1 {
  display: flex;
  align-items: baseline;
  gap: var(--xh-space-2);
  min-width: 0;
}

.ab-hero-name {
  font-size: var(--xh-font-size-xl);
  font-weight: var(--xh-font-weight-bold);
  color: var(--xh-fg-default);
  line-height: 1.2;
  flex-shrink: 0;
}

.ab-hero-sep {
  color: var(--xh-fg-disabled);
  font-size: var(--xh-font-size-xs);
}

.ab-hero-desc {
  font-size: var(--xh-font-size-sm);
  color: var(--xh-fg-muted);
  line-height: 1.5;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ab-hero-line2 {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2);
  margin-top: var(--xh-space-2);
}

.ab-overview-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: var(--xh-space-2);
}

.ab-overview-item {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  padding: var(--xh-space-3);
  border-radius: var(--xh-shape-surface);
  background: var(--xh-bg-surface);
  border: 1px solid var(--xh-border-default);
  min-width: 0;
  transition: border-color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.ab-overview-item:hover {
  border-color: color-mix(in oklab, var(--xh-bg-brand) 30%, transparent);
}

.ab-overview-icon {
  width: calc(var(--xh-space-8) + var(--xh-space-1));
  height: calc(var(--xh-space-8) + var(--xh-space-1));
  border-radius: var(--xh-shape-control);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background: var(--xh-bg-brand-subtle);
  color: var(--xh-fg-brand);
}

.ab-overview-body {
  flex: 1;
  min-width: 0;
}

.ab-overview-label {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
  line-height: 1.4;
}

.ab-overview-value {
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
  margin-top: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ab-overview-link {
  color: var(--xh-fg-brand);
  text-decoration: none;
}

.ab-overview-link:hover {
  text-decoration: underline;
}

.ab-overview-sub {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-subtle);
  margin-top: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ab-card-header {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.ab-keywords {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2);
}

.ab-cap-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--xh-space-2);
}

.ab-cap-item {
  display: flex;
  align-items: flex-start;
  gap: var(--xh-space-2);
  padding: var(--xh-space-2);
  border-radius: var(--xh-shape-surface);
  border: 1px solid var(--xh-border-default);
  background: var(--xh-bg-surface);
}

.ab-cap-icon {
  width: var(--xh-space-8);
  height: var(--xh-space-8);
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-brand-subtle);
  color: var(--xh-fg-brand);
  flex-shrink: 0;
}

.ab-cap-title {
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.ab-cap-desc {
  margin-top: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  line-height: 1.55;
  color: var(--xh-fg-muted);
}

.ab-governance {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(calc(var(--xh-space-8) * 7), 1fr));
  gap: var(--xh-space-2);
}

.ab-gov-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--xh-space-2);
  padding: var(--xh-space-2);
  border-radius: var(--xh-shape-surface);
  border: 1px solid var(--xh-border-default);
  background: var(--xh-bg-surface);
}

.ab-gov-left {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  min-width: 0;
}

.ab-gov-icon {
  width: var(--xh-space-7);
  height: var(--xh-space-7);
  border-radius: var(--xh-shape-control);
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--xh-bg-brand-subtle);
  color: var(--xh-fg-brand);
  flex-shrink: 0;
}

.ab-gov-label {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

.ab-gov-value {
  font-size: var(--xh-font-size-xs);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.ab-empty {
  padding: var(--xh-space-5) 0;
  text-align: center;
  font-size: var(--xh-font-size-sm);
  color: var(--xh-fg-subtle);
}

.ab-pkg-count {
  margin-inline-start: auto;
  font-variant-numeric: tabular-nums;
}

.ab-pkg-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--xh-space-2);
  max-height: calc(var(--xh-space-8) * 13);
  overflow-y: auto;
}

.ab-pkg-item {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  padding: var(--xh-space-1) var(--xh-space-2);
  border-radius: var(--xh-shape-surface);
  border: 1px solid var(--xh-border-default);
  background: var(--xh-bg-surface);
  min-width: 0;
  transition: border-color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.ab-pkg-item:hover {
  border-color: color-mix(in oklab, var(--xh-bg-brand) 30%, transparent);
}

.ab-pkg-icon {
  flex-shrink: 0;
  color: color-mix(in oklab, var(--xh-fg-brand) 60%, transparent);
}

.ab-pkg-name {
  font-size: var(--xh-font-size-xs);
  font-weight: var(--xh-font-weight-medium);
  color: var(--xh-fg-default);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
  min-width: 0;
}

.ab-pkg-ver {
  flex-shrink: 0;
  font-variant-numeric: tabular-nums;
  font-size: var(--xh-font-size-xs);
}

@media (max-width: 64rem) {
  .ab-overview-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .ab-pkg-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .ab-cap-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 40rem) {
  .ab-banner {
    padding: var(--xh-space-3);
  }

  .ab-overview-grid {
    grid-template-columns: 1fr;
  }

  .ab-hero-name {
    font-size: var(--xh-font-size-lg);
  }

  .ab-hero-line2 {
    gap: var(--xh-space-1);
  }

  .ab-pkg-grid {
    grid-template-columns: 1fr;
  }
}
</style>
