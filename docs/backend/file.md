# 文件与存储

文件能力分三块：**文件库**（元数据与多副本）、**存储配置**（后端在哪儿、怎么连）、**导入导出中心**（大批量数据的异步进出）。对应菜单目录 `/file` 与 `/setting` 下的存储配置页。

## 数据模型：元数据与存储位置分离

这是理解整块能力的关键——**一个文件（`SysFile`）可以有多个存储位置（`SysFileStorage`）**。

```text
SysFile（文件元数据，业务只认它）
  ├─ SysFileStorage  ← 主副本（IsPrimary = true）：本地磁盘
  └─ SysFileStorage  ← 备副本：对象存储 / CDN
```

### `SysFile`：文件是什么

| 字段 | 说明 |
| --- | --- |
| `FileName` / `OriginalName` / `FileExtension` | 存储名 / 用户上传时的原名 / 扩展名 |
| `FileType` / `MimeType` | 分类与 MIME |
| `FileSize` | 字节数（**JSON 里是字符串**，`long` 统一序列化为字符串） |
| `FileHash` / `HashAlgorithm` | 内容哈希——**秒传探测与去重的依据** |
| `Width` / `Height` / `Duration` | 图片尺寸 / 媒体时长 |
| `ThumbnailFileId` | 缩略图指向另一条 `SysFile` |
| `UploadIp` / `UploadSource` | 上传来源审计 |

### `SysFileStorage`：文件在哪儿

| 字段 | 说明 |
| --- | --- |
| `FileId` | 归属文件 |
| `StorageType` / `StorageProvider` / `StorageConfigId` | 后端类型、提供方、用的哪份存储配置 |
| `BucketName` / `StorageRegion` / `StoragePath` / `FullPath` | 定位信息 |
| `InternalUrl` / `ExternalUrl` / `CdnUrl` | 内网 / 外网 / CDN 访问地址 |
| `IsPrimary` | **主副本标识**，读取默认走它 |

::: tip 为什么要拆两张表
迁移存储后端时（本地 → OSS）只需给同一个 `SysFile` 追加一条 `SysFileStorage` 并切换 `IsPrimary`，**业务表里保存的 `FileId` 不用动**。这也是下面「业务只存 fileId」这条约定成立的前提。
:::

## 存储配置（`/file/storage`）

`SysStorageConfig` 是**落库的**存储后端配置，可以按租户隔离、运行期切换：

| 字段 | 说明 |
| --- | --- |
| `ConfigCode` / `ConfigName` | 配置编码与名称 |
| `StorageType` | `Local`(0) / `S3`(1) / `OSS`(2) / `COS`(3) / `MinIO`(4) |
| `Endpoint` / `Region` / `BucketName` | 连接信息 |
| `AccessKeyId` / `SecretAccessKey` | 凭据（**加密存储**） |
| `IsDefault` | 默认配置 |
| `IsEnabled` / `Sort` / `Remark` | 常规 |

本地存储另有一份基础设施级配置在 `appsettings` 的 `XiHan:ObjectStorage:Local`（`RootPath` 默认 `wwwroot/uploads`、`UrlPrefix` 默认 `/uploads`）。

::: warning 本地存储的 URL 是根相对路径
本地存储返回 `/uploads/...` 这样的**根相对路径**，静态文件中间件挂在鉴权之前所以能匿名直链。

前后端**同源**时直接可用；**线上不同源时必须拼上 `VITE_API_BASE_URL` 的 origin**，否则浏览器会拿前端域名去请求而 404。仓库里的 `toAbsoluteFileUrl` / `useAvatarUrl` 已经处理，自己拼 URL 时别忘了。
:::

## 文件管理（`/file/library`）

上传、查看、下载、删除文件。几条约定：

::: danger 业务表存 fileId，不存 URL
把 URL 写进业务表会在两件事上出问题：换存储后端时全表 URL 失效；带签名的私有对象 URL 会过期。

**正确做法是业务表只存 `FileId`**，读取时由服务端解析出当前有效的访问地址。
:::

- **秒传**：上传前先按内容哈希探测，命中就直接复用已有 `SysFile`，不重复传输与落盘。注意探测未命中时响应的 `data` 字段会被 `WhenWritingNull` 整个省略——客户端要按 `isSuccess` 判定，不要判断 `data` 是否存在。
- **上传走 `multipart/form-data`**：前端不要手工设 `Content-Type`，让浏览器自己带 boundary（`RequestClient` 检测到 `FormData` 会主动删掉该头）。
- **缩略图是独立的 `SysFile`**，通过 `ThumbnailFileId` 关联，不是同一条记录的附属字段。

## 导出中心（`/file/export-center`）

大数据量导出不能占请求线程，走**异步任务 + 队列消费**：

```text
页面点「导出」
   │ 提交 SysExportTask（Status=Pending，落查询快照与字段快照）
   │ 事务提交后入 IRedisDelayQueue<ExportTaskMessage>
   ▼
ExportTaskHostedService 拉取 → 执行导出 → 写文件 → 回写 FileId/进度/终态
   ▼
页面轮询任务状态 → 完成后下载
```

`SysExportTask` 的关键字段：

| 字段 | 说明 |
| --- | --- |
| `BusinessType` | 业务类型（如 `log.access`），决定用哪个导出 Provider |
| `Scope` | 导出范围（当前搜索结果 / 全量等） |
| `Format` | 导出格式（CSV 等） |
| `Status` / `Progress` / `TotalCount` / `ProcessedCount` | 进度反馈 |
| **`QuerySnapshot`** | **提交时的查询条件快照** |
| **`FieldsSnapshot`** | **提交时的字段与列设置快照** |
| `FileId` / `FileName` / `FileSize` | 产物 |
| `ErrorMessage` / `StartedTime` / `FinishedTime` | 执行结果 |

::: tip 为什么要存两份快照
导出是异步的——用户提交后可能立刻改了搜索条件、调了列设置甚至关掉页面。存快照保证**导出的内容与点击那一刻看到的一致**，而不是与执行那一刻的界面状态一致。

这也意味着：修复导出内容的 bug 要看快照里存了什么，而不是看现在页面上是什么。
:::

前端 Schema 页只要在 `PageSchema.resource.export` 里声明 `{ businessType, buildQuery }`，导出按钮就会走这条异步链路；未声明时退化为本地 CSV 导出。**导出按钮的显隐由 `exportPermission` 精准门控**，未声明该字段则该页不显示导出。

### 崩溃恢复

后台服务启动时会复位崩溃残留的「执行中」任务并重投，否则一次异常重启会让一批任务永久卡在中间态。新增类似链路时别漏这一步，见 [缓存与异步](./caching#崩溃恢复)。

## 导入

`SysImportHistory` 记录导入批次。前端 Schema 页内置 `SchemaImportDialog`：下载模板 → 选文件 → CSV 解析 → **预校验** → 批量创建。

模板字段由页面的 `ListFieldSchema` 中 `importable` 为真的字段派生——和表格、导出共用同一份字段声明，不需要单独维护模板。

## 相关页面

- [Schema 驱动页面](../frontend/schema-page)：导入导出按钮怎么声明
- [缓存与异步](./caching)：队列与后台消费机制
- [数据模型](./data-model#文件与任务)：相关表结构
- [配置参考](../configuration#xihan-objectstorage)：本地存储路径与 URL 前缀
