<script lang="ts" setup>
import type { SysNuGetPackage } from '@/api'
import { NCard, NTag, NText } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { serverApi } from '@/api'
import { Icon } from '~/iconify'
import PackageJson from '../../../../package.json'

defineOptions({ name: 'AboutPage' })

const { t } = useI18n()

const dependencies = PackageJson.dependencies ?? {}
const devDependencies = PackageJson.devDependencies ?? {}
const keywords = Array.isArray(PackageJson.keyword) ? PackageJson.keyword : []

const appName = PackageJson.name ?? 'XiHan.BasicApp'
const appLogo = import.meta.env.VITE_APP_LOGO || '/favicon.png'
const version = PackageJson.version ?? '0.0.0'
const lastBuildTime = PackageJson.lastBuildTime ?? '-'
const description = PackageJson.description ?? '-'
const license = PackageJson.license ?? '-'
const homepage = PackageJson.homepage ?? ''
const repository
  = typeof PackageJson.repository === 'string'
    ? PackageJson.repository
    : (PackageJson.repository?.url ?? '')
const author
  = typeof PackageJson.author === 'string' ? PackageJson.author : (PackageJson.author?.name ?? '-')

const dependencyCount = Object.keys(dependencies).length
const devDependencyCount = Object.keys(devDependencies).length
const overviewItems = [
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
    subValue: 'Build Time',
  },
  {
    icon: 'lucide:globe',
    label: t('page.about.homepage'),
    value: homepage || '-',
    subValue: 'Official Site',
  },
  {
    icon: 'lucide:github',
    label: t('page.about.repository'),
    value: repository || '-',
    subValue: 'Source Repository',
  },
]

const coreCapabilities = [
  {
    icon: 'lucide:shield',
    title: '安全与权限',
    desc: '内置 RBAC、会话与审计体系，满足企业级安全治理要求。',
  },
  {
    icon: 'lucide:zap',
    title: '高效开发',
    desc: '模块化架构与统一约定，显著降低业务落地与维护成本。',
  },
  {
    icon: 'lucide:workflow',
    title: '灵活扩展',
    desc: '前后端解耦并支持动态菜单与能力扩展，适应复杂场景演进。',
  },
]

const governanceItems = [
  { icon: 'lucide:activity', label: '稳定性', value: '生产可用' },
  { icon: 'lucide:database', label: '数据层', value: '.NET + SqlSugar' },
  { icon: 'lucide:globe', label: '国际化', value: '中英双语' },
  { icon: 'lucide:layers', label: '架构模式', value: '模块化分层' },
]

const backendDependencies = ref<SysNuGetPackage[]>([])

async function fetchBackendDependencies() {
  try {
    backendDependencies.value = (await serverApi.getNuGetPackages()) ?? []
  }
  catch {
    backendDependencies.value = []
  }
}

onMounted(() => {
  fetchBackendDependencies()
})
</script>

<template>
  <div class="ab-page">
    <div class="ab-banner">
      <div class="ab-banner-glow" />
      <div class="ab-banner-head">
        <div class="ab-banner-title">
          <Icon icon="lucide:shield-check" width="18" />
          <span>{{ t('page.about.title') }}</span>
        </div>
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
            <NTag size="small" :bordered="false" type="primary">
              Enterprise Ready
            </NTag>
            <NTag size="small" :bordered="false" type="success">
              Composable Architecture
            </NTag>
            <NTag size="small" :bordered="false" type="info">
              Modern Stack
            </NTag>
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
              {{ item.value }}
            </div>
            <div class="ab-overview-sub" :title="item.subValue">
              {{ item.subValue }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="ab-section-grid">
      <NCard :bordered="false" size="small" class="ab-card">
        <template #header>
          <div class="ab-card-header">
            <Icon icon="lucide:sparkles" width="16" />
            <span>核心能力</span>
          </div>
        </template>
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
      </NCard>

      <NCard :bordered="false" size="small" class="ab-card">
        <template #header>
          <div class="ab-card-header">
            <Icon icon="lucide:gem" width="16" />
            <span>平台特性</span>
          </div>
        </template>
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
      </NCard>
    </div>

    <NCard :bordered="false" size="small" class="ab-card">
      <template #header>
        <div class="ab-card-header">
          <Icon icon="lucide:box" width="16" />
          <span>后端生产环境依赖 (NuGet)</span>
          <NTag size="small" :bordered="false" type="info" class="ab-pkg-count">
            {{ backendDependencies.length }}
          </NTag>
        </div>
      </template>
      <div v-if="!backendDependencies.length" class="ab-empty">
        暂无数据
      </div>
      <div v-else class="ab-pkg-grid">
        <div v-for="pkg in backendDependencies" :key="pkg.packageName" class="ab-pkg-item">
          <Icon icon="lucide:package" width="14" class="ab-pkg-icon" />
          <span class="ab-pkg-name" :title="pkg.packageName">
            {{ pkg.packageName }}
          </span>
          <NTag type="success" size="tiny" :bordered="false" class="ab-pkg-ver">
            {{ pkg.packageVersion }}
          </NTag>
        </div>
      </div>
    </NCard>

    <NCard :bordered="false" size="small" class="ab-card">
      <template #header>
        <div class="ab-card-header">
          <Icon icon="lucide:box" width="16" />
          <span>前端生产环境依赖 (NPM)</span>
          <NTag size="small" :bordered="false" type="info" class="ab-pkg-count">
            {{ dependencyCount }}
          </NTag>
        </div>
      </template>
      <div v-if="!dependencyCount" class="ab-empty">
        暂无数据
      </div>
      <div v-else class="ab-pkg-grid">
        <div v-for="(value, key) in dependencies" :key="String(key)" class="ab-pkg-item">
          <Icon icon="lucide:package" width="14" class="ab-pkg-icon" />
          <span class="ab-pkg-name" :title="String(key)">
            {{ key }}
          </span>
          <NTag type="success" size="tiny" :bordered="false" class="ab-pkg-ver">
            {{ value }}
          </NTag>
        </div>
      </div>
    </NCard>

    <NCard :bordered="false" size="small" class="ab-card">
      <template #header>
        <div class="ab-card-header">
          <Icon icon="lucide:wrench" width="16" />
          <span>前端开发环境依赖 (NPM)</span>
          <NTag size="small" :bordered="false" type="info" class="ab-pkg-count">
            {{ devDependencyCount }}
          </NTag>
        </div>
      </template>
      <div v-if="!devDependencyCount" class="ab-empty">
        暂无数据
      </div>
      <div v-else class="ab-pkg-grid">
        <div v-for="(value, key) in devDependencies" :key="String(key)" class="ab-pkg-item">
          <Icon icon="lucide:package" width="14" class="ab-pkg-icon" />
          <span class="ab-pkg-name" :title="String(key)">
            {{ key }}
          </span>
          <NTag type="error" size="tiny" :bordered="false" class="ab-pkg-ver">
            {{ value }}
          </NTag>
        </div>
      </div>
    </NCard>

    <NCard :bordered="false" size="small" class="ab-card">
      <template #header>
        <div class="ab-card-header">
          <Icon icon="lucide:tag" width="16" />
          <span>{{ t('page.about.keywords') }}</span>
        </div>
      </template>
      <div class="ab-keywords">
        <NTag
          v-for="(value, index) in keywords"
          :key="String(value)"
          size="small"
          :bordered="false"
          type="primary"
        >
          #{{ index + 1 }} {{ value }}
        </NTag>
        <NText v-if="keywords.length === 0" depth="3">
          {{ t('page.about.empty_keywords') }}
        </NText>
      </div>
    </NCard>
  </div>
</template>

<style scoped>
.ab-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.ab-banner {
  position: relative;
  overflow: hidden;
  padding: 16px 20px;
  border-radius: var(--radius);
  background:
    radial-gradient(circle at 85% 0%, hsl(var(--primary) / 16%), transparent 45%),
    linear-gradient(135deg, hsl(var(--accent)), hsl(var(--muted)));
  border: 1px solid var(--border-color);
}

.ab-banner-glow {
  position: absolute;
  right: -60px;
  top: -60px;
  width: 180px;
  height: 180px;
  border-radius: 50%;
  background: hsl(var(--primary) / 12%);
  filter: blur(20px);
  pointer-events: none;
}

.ab-banner-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}

.ab-banner-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.ab-hero {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.ab-logo {
  width: 56px;
  height: 56px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background: hsl(var(--primary) / 8%);
  border: 1px solid hsl(var(--primary) / 25%);
  overflow: hidden;
}

.ab-logo-img {
  width: 40px;
  height: 40px;
  object-fit: contain;
}

.ab-hero-main {
  min-width: 0;
}

.ab-hero-line1 {
  display: flex;
  align-items: baseline;
  gap: 8px;
  min-width: 0;
}

.ab-hero-name {
  font-size: 20px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.2;
  flex-shrink: 0;
}

.ab-hero-sep {
  color: var(--text-disabled);
  font-size: 12px;
}

.ab-hero-desc {
  font-size: 13px;
  color: var(--text-secondary);
  line-height: 1.5;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ab-hero-line2 {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 8px;
}

.ab-overview-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 10px;
}

.ab-overview-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px;
  border-radius: var(--radius);
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  min-width: 0;
  transition: border-color 0.2s;
}

.ab-overview-item:hover {
  border-color: hsl(var(--primary) / 30%);
}

.ab-overview-icon {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
}

.ab-overview-body {
  flex: 1;
  min-width: 0;
}

.ab-overview-label {
  font-size: 12px;
  color: var(--text-secondary);
  line-height: 1.4;
}

.ab-overview-value {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  margin-top: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ab-overview-sub {
  font-size: 11px;
  color: var(--text-disabled);
  margin-top: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ab-card {
  background: var(--bg-card);
}

.ab-section-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}

.ab-card-header {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.ab-keywords {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.ab-cap-grid {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.ab-cap-item {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 10px;
  border-radius: var(--radius);
  border: 1px solid var(--border-color);
  background: var(--bg-surface);
}

.ab-cap-icon {
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
  flex-shrink: 0;
}

.ab-cap-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
}

.ab-cap-desc {
  margin-top: 3px;
  font-size: 12px;
  line-height: 1.55;
  color: var(--text-secondary);
}

.ab-governance {
  display: grid;
  gap: 8px;
}

.ab-gov-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 10px;
  border-radius: var(--radius);
  border: 1px solid var(--border-color);
  background: var(--bg-surface);
}

.ab-gov-left {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.ab-gov-icon {
  width: 26px;
  height: 26px;
  border-radius: 7px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
  flex-shrink: 0;
}

.ab-gov-label {
  font-size: 12px;
  color: var(--text-secondary);
}

.ab-gov-value {
  font-size: 12px;
  font-weight: 600;
  color: var(--text-primary);
}

.ab-empty {
  padding: 20px 0;
  text-align: center;
  font-size: 13px;
  color: var(--text-tertiary);
}

.ab-pkg-count {
  margin-left: auto;
  font-variant-numeric: tabular-nums;
}

.ab-pkg-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 6px;
  max-height: 420px;
  overflow-y: auto;
}

.ab-pkg-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  border-radius: var(--radius);
  border: 1px solid var(--border-color);
  background: var(--bg-surface);
  min-width: 0;
  transition: border-color 0.2s;
}

.ab-pkg-item:hover {
  border-color: hsl(var(--primary) / 30%);
}

.ab-pkg-icon {
  flex-shrink: 0;
  color: hsl(var(--primary) / 60%);
}

.ab-pkg-name {
  font-size: 12px;
  font-weight: 500;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
  min-width: 0;
}

.ab-pkg-ver {
  flex-shrink: 0;
  font-variant-numeric: tabular-nums;
  font-size: 11px;
}

@media (max-width: 1024px) {
  .ab-overview-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .ab-section-grid {
    grid-template-columns: 1fr;
  }

  .ab-pkg-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 640px) {
  .ab-banner {
    padding: 14px;
  }

  .ab-overview-grid {
    grid-template-columns: 1fr;
  }

  .ab-hero-name {
    font-size: 18px;
  }

  .ab-hero-line2 {
    gap: 6px;
  }

  .ab-pkg-grid {
    grid-template-columns: 1fr;
  }
}
</style>
