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
      // 模板里用了没导入的组件时 Vue 只会渲染一个零尺寸的未知元素，不报错；Xh* 由 vite.config.ts 的解析器自动解析，Router* 由 vue-router 全局注册
      'vue/no-undef-components': ['error', {
        ignorePatterns: ['Xh.*', 'RouterView', 'RouterLink'],
      }],
      'vue/multi-word-component-names': 'off',
      '@typescript-eslint/no-explicit-any': 'warn',
    },
    ignores: [
      'src/types/components.d.ts',
      'packages/contracts/types.ts',
      'packages/contracts/operations.ts',
      'packages/contracts/permissions.ts',
      'packages/contracts/enums.ts',
      'contracts/**',
      'dist/**',
      'node_modules/**',
    ],
  },
  {
    // 门禁与生成脚本是命令行工具，结果写到标准输出
    name: 'xihan/scripts-console',
    files: ['scripts/**/*.mjs'],
    rules: {
      'no-console': 'off',
    },
  },
  {
    // packages 是前端底层基建，禁止反向依赖 src（应用层）
    name: 'xihan/packages-no-reverse-dep',
    files: ['packages/**/*.?([cm])[jt]s?(x)', 'packages/**/*.vue'],
    rules: {
      'no-restricted-imports': ['error', {
        patterns: [
          {
            group: ['@/*', '@/**', '../../src/**', '../../../src/**'],
            message: 'packages 为前端底层基建，不得反向依赖 src（应用层）。',
          },
        ],
      }],
    },
  },
)
