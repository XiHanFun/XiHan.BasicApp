import { Buffer } from 'node:buffer'
import { execFileSync } from 'node:child_process'
import fs from 'node:fs'
import path from 'node:path'
import process from 'node:process'
import { fileURLToPath, URL } from 'node:url'
import zlib from 'node:zlib'

const sampleDir = fileURLToPath(new URL('../../backend/samples/CrmPlugin', import.meta.url))
const outputDir = fileURLToPath(new URL('../test-results/crm-plugin', import.meta.url))

/** 留在包根上的文件与目录，其余构建产物放进 runtimes/<运行时标识>/ */
const ROOT_ENTRIES = [
  'xihan.manifest.json',
  'basicapp.plugin.json',
  'locales/',
  'pages/',
]

/** 当前平台的运行时标识，与后端 RuntimeInformation.RuntimeIdentifier 同形 */
function runtimeIdentifier(): string {
  const os = process.platform === 'win32'
    ? 'win'
    : process.platform === 'darwin'
      ? 'osx'
      : 'linux'
  return `${os}-${process.arch === 'arm64'
    ? 'arm64'
    : 'x64'}`
}

function filesUnder(dir: string, prefix = ''): string[] {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => entry.isDirectory()
    ? filesUnder(path.join(dir, entry.name), `${prefix}${entry.name}/`)
    : [`${prefix}${entry.name}`])
}

/** 最小的 zip 写出：每个条目 deflate，文件名按 UTF-8 */
function zip(entries: ReadonlyArray<readonly [string, Buffer]>): Buffer {
  const locals: Buffer[] = []
  const centrals: Buffer[] = []
  let offset = 0
  for (const [name, data] of entries) {
    const fileName = Buffer.from(name, 'utf8')
    const packed = zlib.deflateRawSync(data)
    const crc = zlib.crc32(data)
    const local = Buffer.alloc(30)
    local.writeUInt32LE(0x04034B50, 0)
    local.writeUInt16LE(20, 4)
    local.writeUInt16LE(0x0800, 6)
    local.writeUInt16LE(8, 8)
    local.writeUInt16LE(0x0021, 12)
    local.writeUInt32LE(crc, 14)
    local.writeUInt32LE(packed.length, 18)
    local.writeUInt32LE(data.length, 22)
    local.writeUInt16LE(fileName.length, 26)
    const central = Buffer.alloc(46)
    central.writeUInt32LE(0x02014B50, 0)
    central.writeUInt16LE(20, 4)
    central.writeUInt16LE(20, 6)
    central.writeUInt16LE(0x0800, 8)
    central.writeUInt16LE(8, 10)
    central.writeUInt16LE(0x0021, 14)
    central.writeUInt32LE(crc, 16)
    central.writeUInt32LE(packed.length, 20)
    central.writeUInt32LE(data.length, 24)
    central.writeUInt16LE(fileName.length, 28)
    central.writeUInt32LE(offset, 42)
    locals.push(local, fileName, packed)
    centrals.push(central, fileName)
    offset += local.length + fileName.length + packed.length
  }
  const directory = Buffer.concat(centrals)
  const end = Buffer.alloc(22)
  end.writeUInt32LE(0x06054B50, 0)
  end.writeUInt16LE(entries.length, 8)
  end.writeUInt16LE(entries.length, 10)
  end.writeUInt32LE(directory.length, 12)
  end.writeUInt32LE(offset, 16)
  return Buffer.concat([...locals, directory, end])
}

/**
 * 构建示例插件 CrmPlugin 并装成 .xhplugin
 * 旁挂清单的 runtimes 换成当前运行时标识，与包里有的一致（集成测试的 CrmPluginPackage 同一做法）
 */
export function crmPluginPackage(): Buffer {
  fs.rmSync(outputDir, { recursive: true, force: true })
  execFileSync('dotnet', [
    'build',
    sampleDir,
    '-c',
    'Release',
    '-o',
    outputDir,
    '--nologo',
    '-v',
    'quiet',
  ], { stdio: 'inherit' })
  const runtime = runtimeIdentifier()
  const entries = filesUnder(outputDir).map((file): [string, Buffer] => {
    const atRoot = ROOT_ENTRIES.some(root => root.endsWith('/')
      ? file.startsWith(root)
      : file === root)
    let content = fs.readFileSync(path.join(outputDir, file))
    if (file === 'basicapp.plugin.json') {
      const descriptor = JSON.parse(content.toString('utf8')) as Record<string, unknown>
      descriptor.runtimes = [runtime]
      content = Buffer.from(JSON.stringify(descriptor), 'utf8')
    }
    return [
      atRoot
        ? file
        : `runtimes/${runtime}/${file}`,
      content,
    ]
  })
  return zip(entries)
}
