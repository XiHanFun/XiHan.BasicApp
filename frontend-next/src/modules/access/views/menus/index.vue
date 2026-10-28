<script lang="ts" setup>
import type { MenuKind, MenuRow } from './use-menus'
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import { useMenus } from './use-menus'

defineOptions({ name: 'AccessMenusView' })

const { t, te } = useI18n()
const auth = useAuthStore()
const page = useMenus()

const columns = [
  { id: 'title', label: 'access.menus.fields.title' },
  { id: 'kind', label: 'access.menus.fields.kind' },
  { id: 'target', label: 'access.menus.fields.target' },
  { id: 'permission', label: 'access.menus.fields.permission' },
  { id: 'order', label: 'access.menus.fields.order' },
  { id: 'state', label: 'access.menus.fields.state' },
] as const

onMounted(() => {
  void page.load()
})

/** 节点标题：运营节点与改过标题的页面是原文，其余按文案键翻译 */
function titleOf(node: MenuRow): string {
  if (node.title)
    return node.title
  return te(node.titleKey) ? t(node.titleKey) : node.titleKey
}

// 种类是契约枚举（字符串），文案键静态写出以便 i18n 门禁核对
function kindLabel(kind: MenuKind): string {
  if (kind === 'Directory')
    return t('access.menus.kinds.directory')
  return kind === 'Link' ? t('access.menus.kinds.link') : t('access.menus.kinds.page')
}

function registeredTitle(node: MenuRow): string {
  return te(node.titleKey) ? t(node.titleKey) : node.titleKey
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('access.menus.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('access.menus.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 新建目录或外链：页面菜单来自页面登记，不在这里新建；校验以后端为准，字段级 errors 落到对应字段 -->
    <form v-if="auth.can('access.menus.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-4" data-testid="create-menu" novalidate @submit.prevent="page.submit()">
      <label class="flex flex-col gap-1 text-caption text-fg-muted" for="create-menu-kind">
        {{ t('access.menus.fields.kind') }}
        <select id="create-menu-kind" v-model="page.form.kind" name="kind" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
          <option value="Directory">
            {{ t('access.menus.kinds.directory') }}
          </option>
          <option value="Link">
            {{ t('access.menus.kinds.link') }}
          </option>
        </select>
      </label>
      <XhFieldRoot :label="t('access.menus.fields.code')" :error-text="page.fieldError('code')" :invalid="!!page.fieldError('code')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.code" name="code" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('access.menus.fields.title')" :error-text="page.fieldError('title')" :invalid="!!page.fieldError('title')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.title" name="title" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('access.menus.fields.icon')" :error-text="page.fieldError('icon')" :invalid="!!page.fieldError('icon')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.icon" name="icon" placeholder="lucide:folder" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('access.menus.fields.order')" :error-text="page.fieldError('order')" :invalid="!!page.fieldError('order')">
        <template #control>
          <XhNumberFieldRoot v-model:value="page.form.order" name="order" :step="1" />
        </template>
      </XhFieldRoot>
      <template v-if="page.form.kind === 'Link'">
        <label class="flex flex-col gap-1 text-caption text-fg-muted" for="create-menu-parent">
          {{ t('access.menus.fields.parent') }}
          <select id="create-menu-parent" v-model="page.form.parentId" name="parentId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
            <option value="">
              {{ t('access.menus.top_level') }}
            </option>
            <option v-for="directory in page.directories.value" :key="directory.id" :value="directory.id">
              {{ titleOf(directory) }}
            </option>
          </select>
        </label>
        <XhFieldRoot :label="t('access.menus.fields.url')" :error-text="page.fieldError('externalUrl')" :invalid="!!page.fieldError('externalUrl')" required>
          <template #control>
            <XhTextFieldRoot v-model:value="page.form.externalUrl" name="externalUrl" type="url" placeholder="https://" />
          </template>
        </XhFieldRoot>
      </template>
      <XhFieldRoot :label="t('access.menus.fields.permission')" :error-text="page.fieldError('permission')" :invalid="!!page.fieldError('permission')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.permission" name="permission" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <div class="flex items-end gap-2">
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('access.menus.actions.create') }}
        </XhButton>
        <XhButton type="button" :disabled="page.loading.value" @click="page.load()">
          {{ t('common.actions.refresh') }}
        </XhButton>
      </div>
      <p v-if="page.submitError.value" class="text-caption text-fg-danger md:col-span-4" role="alert">
        {{ page.submitError.value }}
      </p>
    </form>

    <p v-if="page.listError.value" class="text-caption text-fg-danger" role="alert">
      {{ page.listError.value }}
    </p>

    <div class="grid gap-4 lg:grid-cols-2">
      <XhTableRoot
        :columns="columns.map(c => ({ id: c.id, label: t(c.label) }))"
        :rows="page.flat.value.map(row => ({ id: row.node.id }))"
        :loading="page.loading.value"
        size="sm"
        ruled
      >
        <XhTableHeader>
          <XhTableRow>
            <XhTableColumnHeader v-for="column in columns" :key="column.id" :value="column.id">
              <XhTableColumnLabel>{{ t(column.label) }}</XhTableColumnLabel>
            </XhTableColumnHeader>
          </XhTableRow>
        </XhTableHeader>
        <XhTableBody>
          <XhTableRow v-for="row in page.flat.value" :key="row.node.id" :value="row.node.id" :class="page.selected.value?.id === row.node.id ? 'bg-brand-subtle' : 'cursor-pointer'" @click="page.select(row.node.id)">
            <XhTableCell value="title">
              <span class="text-fg-muted">{{ '· '.repeat(row.depth) }}</span>
              <button type="button" class="text-fg-brand underline-offset-2 hover:underline" :aria-pressed="page.selected.value?.id === row.node.id" @click.stop="page.select(row.node.id)">
                {{ titleOf(row.node) }}
              </button>
            </XhTableCell>
            <XhTableCell value="kind">
              {{ kindLabel(row.node.kind) }}
            </XhTableCell>
            <XhTableCell value="target">
              <span class="text-caption">{{ row.node.kind === 'Link' ? row.node.externalUrl : row.node.route || '-' }}</span>
            </XhTableCell>
            <XhTableCell value="permission">
              <span class="text-caption">{{ row.node.permission ?? '-' }}</span>
            </XhTableCell>
            <XhTableCell value="order">
              {{ row.node.order }}
            </XhTableCell>
            <XhTableCell value="state">
              <span v-if="!row.node.isVisible" class="mr-1 rounded-control bg-subtle px-1 text-caption text-fg-muted">{{ t('access.menus.hidden') }}</span>
              <span v-if="row.node.isCustomized" class="rounded-control bg-subtle px-1 text-caption text-fg-muted">{{ t('access.menus.customized') }}</span>
            </XhTableCell>
          </XhTableRow>
          <XhTableEmpty v-if="!page.loading.value && page.flat.value.length === 0">
            {{ t('access.menus.empty') }}
          </XhTableEmpty>
        </XhTableBody>
      </XhTableRoot>

      <!-- 详情：页面节点只改展示（可恢复默认），运营节点另可改地址与权限、可删除；每个动作按自己的权限码门控 -->
      <section v-if="page.selected.value" class="flex flex-col gap-4 rounded-surface border border-border bg-surface p-4" data-testid="menu-detail">
        <header class="flex flex-wrap items-center gap-2">
          <h2 class="text-label font-semibold">
            {{ titleOf(page.selected.value) }}
          </h2>
          <span class="text-caption text-fg-muted">{{ page.selected.value.code }} · {{ kindLabel(page.selected.value.kind) }}</span>
          <div class="ml-auto flex gap-2">
            <XhButton v-if="auth.can('access.menus.update') && page.selected.value.isCustomized" size="sm" :disabled="page.working.value" @click="page.reset()">
              {{ t('access.menus.actions.reset') }}
            </XhButton>
            <XhButton v-if="auth.can('access.menus.delete') && page.selected.value.kind !== 'Page'" size="sm" :disabled="page.working.value" @click="page.remove()">
              {{ t('access.menus.actions.delete') }}
            </XhButton>
          </div>
        </header>
        <p v-if="page.detailError.value" class="text-caption text-fg-danger" role="alert">
          {{ page.detailError.value }}
        </p>
        <p v-if="page.notice.value === 'saved'" class="text-caption text-fg-success" role="status">
          {{ t('access.menus.saved') }}
        </p>
        <p v-if="page.notice.value === 'reset'" class="text-caption text-fg-success" role="status">
          {{ t('access.menus.reset_done') }}
        </p>
        <p v-if="page.selected.value.kind === 'Page'" class="text-caption text-fg-muted">
          {{ t('access.menus.page_hint') }}
          <span v-if="page.selected.value.route"> {{ page.selected.value.route }} · {{ page.selected.value.component }} · {{ page.selected.value.permission ?? '-' }}</span>
        </p>

        <form class="flex flex-col gap-3" data-testid="edit-menu" novalidate @submit.prevent="page.save()">
          <div class="grid gap-3 md:grid-cols-3">
            <XhFieldRoot :label="t('access.menus.fields.title')" :error-text="page.editError('title')" :invalid="!!page.editError('title')" :required="page.selected.value.kind !== 'Page'">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.title" name="title" :placeholder="page.selected.value.kind === 'Page' ? registeredTitle(page.selected.value) : undefined" :disabled="!auth.can('access.menus.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('access.menus.fields.icon')" :error-text="page.editError('icon')" :invalid="!!page.editError('icon')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.icon" name="icon" :disabled="!auth.can('access.menus.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('access.menus.fields.order')" :error-text="page.editError('order')" :invalid="!!page.editError('order')">
              <template #control>
                <XhNumberFieldRoot v-model:value="page.edit.order" name="order" :step="1" :disabled="!auth.can('access.menus.update')" />
              </template>
            </XhFieldRoot>
          </div>
          <div v-if="page.selected.value.kind !== 'Directory'" class="grid gap-3 md:grid-cols-3">
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="edit-menu-parent">
              {{ t('access.menus.fields.parent') }}
              <select id="edit-menu-parent" v-model="page.edit.parentId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!auth.can('access.menus.update') || page.isChildPage(page.selected.value)">
                <option value="">
                  {{ t('access.menus.top_level') }}
                </option>
                <option v-for="directory in page.directories.value" :key="directory.id" :value="directory.id">
                  {{ titleOf(directory) }}
                </option>
                <option v-if="page.isChildPage(page.selected.value)" :value="page.selected.value.parentId ?? ''">
                  {{ t('access.menus.registered_parent') }}
                </option>
              </select>
            </label>
            <template v-if="page.selected.value.kind === 'Link'">
              <XhFieldRoot :label="t('access.menus.fields.url')" :error-text="page.editError('externalUrl')" :invalid="!!page.editError('externalUrl')" required>
                <template #control>
                  <XhTextFieldRoot v-model:value="page.edit.externalUrl" name="externalUrl" type="url" :disabled="!auth.can('access.menus.update')" />
                </template>
              </XhFieldRoot>
            </template>
          </div>
          <XhFieldRoot v-if="page.selected.value.kind !== 'Page'" :label="t('access.menus.fields.permission')" :error-text="page.editError('permission')" :invalid="!!page.editError('permission')">
            <template #control>
              <XhTextFieldRoot v-model:value="page.edit.permission" name="permission" autocomplete="off" :disabled="!auth.can('access.menus.update')" />
            </template>
          </XhFieldRoot>
          <div class="flex flex-wrap gap-4 text-body">
            <label class="flex items-center gap-2">
              <input v-model="page.edit.isVisible" type="checkbox" name="isVisible" :disabled="!auth.can('access.menus.update')">
              {{ t('access.menus.fields.visible') }}
            </label>
            <template v-if="page.selected.value.kind === 'Page'">
              <label class="flex items-center gap-2">
                <input v-model="page.edit.keepAlive" type="checkbox" name="keepAlive" :disabled="!auth.can('access.menus.update')">
                {{ t('access.menus.fields.keep_alive') }}
              </label>
              <label class="flex items-center gap-2">
                <input v-model="page.edit.isAffix" type="checkbox" name="isAffix" :disabled="!auth.can('access.menus.update')">
                {{ t('access.menus.fields.affix') }}
              </label>
            </template>
          </div>
          <div v-if="auth.can('access.menus.update')">
            <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value" :loading="page.working.value">
              {{ t('access.menus.actions.save') }}
            </XhButton>
          </div>
        </form>
      </section>
    </div>
  </section>
</template>
