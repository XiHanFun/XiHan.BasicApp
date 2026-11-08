// CSS 约定，与 XiHan.UI 的皮肤同一套：
// ① 逻辑属性，不写 margin-left / padding-right / left / right 这类物理方向
// ② 颜色只取 --xh-* 令牌，或由令牌调出的 color-mix()；不写字面色值，不读应用配色变量（--primary 一类），组件的局部变量也一样
// ③ 圆角、内距与间隙的长度只取令牌（--xh-shape-* / --xh-space-*），直角写 0、撑满写百分比
// ④ 过渡逐个列出要动的属性，不写 all
// ⑤ 原生滚动条只在 packages/design/scrollbar.css 里给
// ⑥ 字号取 --xh-font-size-* / --xh-text-*-size，不写 px、rem 这类绝对单位（em、百分比与容器单位照常）
// 令牌定义（variables.css、xihan-ui.css、Tailwind 主题）本身就在映射配色，不受颜色一条约束

/** 圆角、内距、间隙三族属性：长度只能来自令牌 */
const LENGTH_PROP = /^(?:border(?:-[a-z]+)*-radius|padding(?:-(?:block|inline)(?:-(?:start|end))?|-(?:top|bottom))?|(?:row-|column-)?gap)$/

/** 裸长度；前后不连字母数字与连字符，令牌名里的数字（--xh-space-1_5）不算 */
const RAW_LENGTH = /(?<![\w-])\.?\d[\d.]*(?:r?(?:em|ex|ch|cap|ic)|px|[sdl]?v(?:[whib]|min|max)|cm|mm|q|in|pt|pc)(?![\w-])/i

/** 长度属性的一段：0、auto、inherit、百分比，或 var() / calc() 一类函数（括号至多三层嵌套） */
const LENGTH_PART = String.raw`(?:0|auto|inherit|\d+(?:\.\d+)?%|(?:var|calc|min|max|clamp)\((?:[^()]|\((?:[^()]|\([^()]*\))*\))*\))`

/** 长度属性的取值：以空白分隔的若干段 */
const LENGTH_VALUE = new RegExp(String.raw`^${LENGTH_PART}(?:\s+${LENGTH_PART})*$`)

/** 颜色属性 */
const COLOR_PROP = /^(?:color|background-color|border-color|outline-color|fill|stroke)$/

/** 字号里的绝对单位 */
const ABSOLUTE_FONT_SIZE = /(?<![\w-])\.?\d[\d.]*(?:px|rem|pt|pc|cm|mm|q|in)(?![\w-])/i

/** 字面色值与应用配色变量（color-mix() 里同样不许） */
const RAW_COLOR = /#[\da-f]{3,8}\b|\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color)\(|var\(\s*--(?:background|foreground|card|popover|muted|primary|secondary|accent|heavy|border|input|ring|overlay|sidebar|header|destructive|success|warning|info)(?:-[a-z]+)*\s*[,)]/i

/** 过渡属性位上独立的 all */
const TRANSITION_ALL = /(?<![\w-])all(?![\w-])/

/** 强制色模式下只能写系统调色板关键字 */
const SYSTEM_COLOR = /^(?:Canvas|CanvasText|ButtonFace|ButtonText|ButtonBorder|Highlight|HighlightText|LinkText|GrayText)$/

const asKey = re => `/${re.source}/`

function allowedListMessage(property, value) {
  if (LENGTH_PROP.test(property))
    return `${property} 的取值 "${value}" 不是令牌：圆角写 var(--xh-shape-…)，内距与间隙写 var(--xh-space-…)；直角写 0，撑满写百分比`
  return `${property} 的取值 "${value}" 不是颜色令牌：写 var(--xh-…) 或由它调出的 color-mix()，或 transparent / currentColor / inherit / none`
}

function disallowedListMessage(property, value) {
  if (property.startsWith('transition'))
    return `${property} 的取值 "${value}" 里写了 all：逐个列出要动的属性`
  if (LENGTH_PROP.test(property))
    return `${property} 的取值 "${value}" 里有裸长度或应用圆角变量：换成 var(--xh-shape-…) / var(--xh-space-…)，calc() 里同样只放令牌`
  if (property.startsWith('--'))
    return `自定义属性 ${property} 的取值 "${value}" 里有字面色值或应用配色变量：改用 --xh-* 语义令牌`
  if (COLOR_PROP.test(property))
    return `${property} 的取值 "${value}" 里有字面色值或应用配色变量：改用 --xh-* 语义令牌`
  if (property === 'font-size')
    return `font-size 的取值 "${value}" 写了绝对单位：改用 var(--xh-font-size-…) 或 var(--xh-text-…-size)，随偏好里的字号走`
  if (property.startsWith('scrollbar'))
    return `${property} 的取值 "${value}"：原生滚动条只在 packages/design/scrollbar.css 里给，藏条写 scrollbar-width: none`
  return `${property} 是物理方向属性：改用逻辑属性`
}

const DISALLOWED = {
  '/^margin-(left|right)$/': [/.*/],
  '/^padding-(left|right)$/': [/.*/],
  '/^transition(-property)?$/': [TRANSITION_ALL],
  [asKey(LENGTH_PROP)]: [RAW_LENGTH, /var\(\s*--radius(?:-card)?\s*[,)]/],
  [asKey(COLOR_PROP)]: [RAW_COLOR],
  '/^--/': [RAW_COLOR],
  'font-size': [ABSOLUTE_FONT_SIZE],
}

const SCROLLBAR_DISALLOWED = {
  'scrollbar-width': [/^(?!none$).+/],
  'scrollbar-color': [/.*/],
}

/** @type {import('stylelint').Config} */
export default {
  ignoreFiles: ['**/dist/**', '**/node_modules/**'],
  rules: {
    'declaration-property-value-disallowed-list': [
      {
        ...DISALLOWED,
        ...SCROLLBAR_DISALLOWED,
      },
      { message: disallowedListMessage },
    ],
    'property-disallowed-list': [
      'margin-left',
      'margin-right',
      'padding-left',
      'padding-right',
      'left',
      'right',
    ],
    'color-no-invalid-hex': true,
    'declaration-property-value-allowed-list': [
      {
        [asKey(COLOR_PROP)]: [
          /^var\(--/,
          /^color-mix\(/,
          /^light-dark\(/,
          SYSTEM_COLOR,
          'transparent',
          'currentColor',
          'currentcolor',
          'inherit',
          'none',
        ],
        // 每一段都是令牌、0、auto、inherit、百分比或数学函数；函数里藏的裸长度由禁用清单挡
        [asKey(LENGTH_PROP)]: [LENGTH_VALUE],
      },
      { message: allowedListMessage },
    ],
  },
  overrides: [
    {
      // 单文件组件只查 <style> 块
      files: ['**/*.vue'],
      customSyntax: 'postcss-html',
    },
    {
      // 令牌定义：把应用配色映射成变量
      files: ['packages/design/variables.css', 'packages/design/xihan-ui.css', 'src/styles/index.css'],
      rules: {
        'declaration-property-value-disallowed-list': [
          {
            ...DISALLOWED,
            ...SCROLLBAR_DISALLOWED,
            '/^--/': [],
          },
          { message: disallowedListMessage },
        ],
      },
    },
    {
      files: ['packages/design/scrollbar.css'],
      rules: {
        'declaration-property-value-disallowed-list': [
          DISALLOWED,
          { message: disallowedListMessage },
        ],
      },
    },
  ],
}
