import antfu from '@antfu/eslint-config'

export default antfu(
  {
    vue: true,
    typescript: true,
    formatters: {
      css: true,
      html: true,
    },
    rules: {
      'no-console': 'warn',
      'vue/component-name-in-template-casing': ['error', 'PascalCase'],
      'vue/multi-word-component-names': 'off',
      '@typescript-eslint/no-explicit-any': 'warn',
    },
    ignores: [
      'src/types/auto-imports.d.ts',
      'src/types/components.d.ts',
      'dist/**',
      'node_modules/**',
    ],
  },
  {
    // packages 是前端底层基建，禁止反向依赖 src（应用层）。契约类型放 packages/types，
    // 运行时依赖经 app-context 注入。见架构审查报告 D1/H8。
    name: 'xihan/packages-no-reverse-dep',
    files: ['packages/**/*.?([cm])[jt]s?(x)', 'packages/**/*.vue'],
    // 已知待迁移的业务内容（阶段 4：business 业务常量 + profile 业务视图 Tab），暂豁免，迁移后删除本例外
    ignores: [
      'packages/constants/business.ts',
      'packages/views/_core/profile/ProfileTabTenants.vue',
      'packages/views/_core/profile/ProfileTabInfo.vue',
      'packages/views/_core/control-center/index.vue',
    ],
    rules: {
      'no-restricted-imports': ['error', {
        patterns: [
          {
            group: ['@/*', '@/**', '../../src/**', '../../../src/**', '../../../../src/**'],
            message: 'packages 为前端底层基建，不得反向依赖 src（应用层）：契约类型放 packages/types，运行时依赖经 app-context 注入。见架构审查报告 D1/H8。',
          },
        ],
      }],
    },
  },
)
