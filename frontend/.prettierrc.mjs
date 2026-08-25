/** @type {import('prettier').Config} */
export default {
  semi: false,
  singleQuote: true,
  // 与 eslint 里 format/prettier 解出的那份保持一致：两处不一样时，
  // pnpm run fix 会让 prettier 把 eslint 刚排好的重新折回去，lint 永远清不干净
  printWidth: 120,
  trailingComma: 'all',
  endOfLine: 'lf',
  tabWidth: 2,
  useTabs: false,
  vueIndentScriptAndStyle: false,
  htmlWhitespaceSensitivity: 'ignore',
}
