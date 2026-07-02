/**
 * 聊天表情集（Unicode emoji，纯字符插入文本，无外部依赖）。
 * 只收录单码位/VS16 变体的常用表情，避开 ZWJ 组合序列（部分平台渲染分裂）。
 * 以空格分隔后 split：不能用字符串展开（VS16 变体如 ✌️ 会被拆成两个码位）。
 */

// 笑脸与情绪
const SMILEYS = '😀 😁 😂 🤣 😃 😄 😅 😆 😉 😊 😋 😎 😍 😘 🥰 🙂 🤗 🤩 🤔 🤨 😐 😶 🙄 😏 😣 😥 😮 😯 😪 😫 🥱 😴 😌 😛 😜 😝 🤤 😒 😓 😔 😕 🙃 🤑 😲 🙁 😖 😞 😟 😤 😢 😭 😨 😩 🤯 😬 😰 😱 🥵 🥶 😳 🤪 😵 😡 😠 😷 🤒 🤕 🤢 🤮 🤧 😇 🥳 🥺 🤠 🤡 🤥 🤫 🤭 🧐 🤓 😈 💀 👻 👽 🤖 💩'

// 手势
const GESTURES = '👍 👎 👌 ✌️ 🤞 🤟 🤘 🤙 👈 👉 👆 👇 ✋ 🖐️ 👋 🤝 🙏 ✊ 👊 👏 🙌 👐 💪 👀'

// 爱心与符号
const SYMBOLS = '❤️ 🧡 💛 💚 💙 💜 🖤 🤍 💔 💕 💖 💯 💢 💥 💫 ⭐ ✨ ⚡ 🔥 🌈 ☀️ 🌙'

// 庆祝与杂项
const MISC = '🎉 🎊 🎁 🎈 🌹 🌸 💐 🍀 🎵 🎶 ☕ 🍺 🎂 🍰'

export const CHAT_EMOJIS: readonly string[]
  = `${SMILEYS} ${GESTURES} ${SYMBOLS} ${MISC}`.split(' ')
