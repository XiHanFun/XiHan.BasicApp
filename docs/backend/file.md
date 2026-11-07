# 文件与存储

本页说明存储配置、文件与副本、上传下载、导出中心与存储配额，适用于后端开发者与运维人员。

Files 包（Tier 2）在框架对象存储（`IBlobStore`）之上管理存储配置、文件与多副本、秒传、签名地址、分片上传，以及导出中心与导入历史。

## 存储配置

`SysStorageConfig`（平台库实体，`IOwnedByTenant`：平台与租户各有自己的配置；访问密钥经 `ISecretProtector` 加密、不回显）。

| 类型 | 实现 |
| --- | --- |
| `Local` | 框架 `ObjectStorage.FileSystem`，根目录 `BasicApp:Files:LocalRootPath`（缺省 `App_Data/files`）；所有本地配置共用命名实例 `local` |
| `S3` / `Oss` / `Cos` / `MinIO` | 框架 `ObjectStorage.S3`，按 `ServiceUrl` 接 S3 兼容服务；每个配置一个命名实例 `storage-<配置 ID>` |

存储配置在运行时译成框架对象存储注册表的命名实例（框架 ADR-0050），健康项 `objectstorage.store.<名>` 随之出现在 `/health/ready`。

S3 兼容配置有一个「条件写入」开关（`conditionalWrites`，缺省开；本地存储不看这一项，恒为开）。实例在第一次用到（或配置改了之后）时建出：

- **开着**：建之前框架先在这条配置的桶里探测条件写入（框架 ADR-0089）：带 `If-None-Match: *` 写两次探测键 `.xihan/probe/conditional-write`、第二次必须被拒，再探一次分片上传的条件完成；上传以不覆盖写入落盘，由存储保证原子。
  - 服务要认 `If-None-Match: *`：RustFS、MinIO 可用；Garage、Zenko CloudServer 不认，返回 `OBJECTSTORAGE_UNAVAILABLE_003`；阿里云 OSS、腾讯云 COS、Cloudflare R2 还没有验证。
  - 访问密钥要能写桶里的 `.xihan/probe/*` 并能放弃分片上传，被拒返回 `OBJECTSTORAGE_DENIED_002`；桶不存在返回 `OBJECTSTORAGE_NOTFOUND_001`。
  - 不认条件写入、凭据被拒、桶不存在时实例不登记，用到这条配置的读写都返回同一个错误；改好配置或服务之后，下次用到时重新探测。
  - 连不上时实例照样建出、在后台与每次用到时重试，探明之前上传返回 503 `OBJECTSTORAGE_UNAVAILABLE_004`，服务恢复后自动可用（框架 ADR-0090）。
  - 探测键是一个空对象，留在桶里不删；`.xihan/` 是框架的保留前缀，应用的对象键不能以它开头（BasicApp 的键按文件 ID 生成，不受影响）。
- **关掉**：不探测，上传与分片上传直接写入。对象键带新文件的 ID、从不重复，不会盖掉已有对象；给不认条件写入的服务用（上传报 `OBJECTSTORAGE_UNAVAILABLE_003` 时关掉这一项）。

开发库里已有的存储配置加这一列后是关的（布尔新列按 `false` 回填），要开就在存储配置页打开。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `storage-configs.list` / `get` | `files.storage-configs.read` | 缺省配置在前 |
| `storage-configs.create` / `update` / `delete` | `files.storage-configs.create` / `update` / `delete` | 缺省配置与还有副本的配置不能删 |
| `storage-configs.set-default` | `files.storage-configs.update` | 之后的上传落在它上面 |
| `storage-configs.set-status` | `files.storage-configs.update` | 停用后不再接收上传，已有副本照常读取；缺省配置不能停用 |

按 ID 取存储配置时认「本侧或平台的」：租户没有自己的配置时用平台的。种子 `files.storage-configs` 写一个平台本地配置。

## 文件与副本

`SysFile` / `SysFileStorage` 是租户库实体（平台一侧的租户为 `platform`）：一个文件可以有多个存储副本，其中一个是主副本，内容按 SHA-256 去重。

| 访问级别（`FileAccessLevel`） | 谁能换取下载地址 |
| --- | --- |
| `Authenticated` | 同一侧已登录即可 |
| `Authorized` | 要持有 `files.files.read` |
| `Public` | 任何已登录主体（不分侧），匿名不行 |

签名地址本身在有效期内不再校验身份。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `files.list` / `get` / `storages` | `files.files.read` | |
| `files.update` / `set-status` | `files.files.update` | 元数据；`Normal` / `Archived` 归档恢复，`Corrupted` 由校验产生 |
| `files.delete` | `files.files.delete` | 不再被别的文件用到的内容随之从存储删除 |
| `files.fast-upload` | `files.files.create` | 秒传：同一侧已有相同内容的正常文件即直接建记录 |
| `files.set-primary-storage` / `verify-storage` / `set-storage-status` | `files.files.update` | 换主副本、按 SHA-256 校验、停用副本 |
| `files.presign` | 按访问级别 | 换限时签名地址 |

## 上传与下载

| 端点 | 说明 |
| --- | --- |
| `POST /api/files/upload` | 宿主映射的普通端点，`multipart/form-data`；上限 `BasicApp:Files:MaxUploadBytes`（100 MB）；受租户存储配额约束 |
| `GET /api/files/{id}/content` | 按身份读取内容，支持 `Range`（音视频拖动） |
| `POST /api/profile/avatar` | 头像：只要求登录，按文件头认 JPEG / PNG，不超过 2 MB；平台侧的公开文件，跟着用户走、不计入租户配额 |

签名地址由存储签出（框架 ADR-0049）：S3 兼容存储直连存储，本地存储走宿主的 `/blobs` 签名端点（密钥 `XiHan:ObjectStorage:SignedUrls:Secret`）。缺省 30 分钟有效，最长 `MaxLinkLifetime`（7 天）。 内容类型是上传方给的，所以只有图片（不含 SVG）、音视频、PDF 与纯文本能内联打开，别的类型（HTML、SVG、XML 等）即使不要 `download` 也以附件下载；`/blobs` 的响应不让浏览器猜类型（`X-Content-Type-Options: nosniff`），除 PDF 外都在沙箱里（`Content-Security-Policy: sandbox`），上传的脚本拿不到前端的源。

### 分片上传（大文件）

```text
files.start-upload        发起，得到 uploadId 与分片大小（BasicApp:Files:UploadPartBytes，缺省 8 MB）
files.sign-upload-parts   一次至多 100 片，签出直传存储的地址
（客户端逐片 PUT 到存储）
files.upload-session      断点续传：查已上传的分片编号
files.complete-upload     按存储里已上传的分片拼成对象，回读算 SHA-256
files.abort-upload        放弃
```

上传会话 `BasicApp:Files:UploadLifetime`（1 天）后过期，作业 `files.uploads.expire` 每小时清理。

普通端点经宿主的端点过滤器登记审计身份，访问日志按 `files.upload` 这类名字记录。

## 导出中心

| 操作 | 说明 |
| --- | --- |
| `export-tasks.submit` | 提交导出：资源（列表操作）、请求、范围（`CurrentPage` / `SearchResult` / `All`）、格式（`Csv` / `Xlsx`） |
| `export-tasks.list` / `get` | 我的导出 |
| `export-tasks.cancel` | 执行中的不能取消 |
| `export-tasks.delete` | 结果文件一并删除 |

都只要求登录：后台以发起人的凭据逐页调用对应的列表操作，权限、租户、数据范围与字段安全照常判定。结果存成 7 天的临时文件。进度经 `TaskProgress` 推给发起人（领到任务、每写一千行或到 99%、完成或失败，见[任务进度](./realtime#任务进度)），文案与失败原因按提交时的区域翻译。前端的用户页与六种日志页接了导出中心，其余声明了导出的列表页是浏览器里的本地 CSV（见[列表页引擎](../frontend/schema-page#导出与导入)）。 执行时抛异常，任务的失败原因只有异常类型，完整异常看应用日志。CSV 单元格以 `=`、`+`、`-`、`@`、制表符或换行开头时前面加单引号，防止电子表格软件当公式执行；单个字符与普通数字保持原样。XLSX 写的是内联字符串，不会被当公式。

临时文件过了到期时间由作业 `files.temporary.purge` 每小时清理：平台与各租户逐批删掉（文件与副本软删除，同删除操作），存储里不再被别的副本引用的对象一起删；一批 100 条、一侧一次至多 20 批，积压的留给下一次。没标为临时的文件即使写了到期时间也不删；读取侧本来就按到期拒绝，不等作业。

别的包产出的临时文件经 Contracts 的 `ITemporaryFiles` 存取（如代码生成的产物包）：存在调用方这一侧，标为临时、带到期时间，访问级别「需要授权」；读取不再核对通用的文件权限，由业务按自己的权限先核对，到期的按不存在处理。

## 导入历史

`import-histories.create` / `recent`（只要求登录）：导入本身由前端逐行调用资源的新建操作完成，后端只记历史。

## 配额

上传、秒传与分片完成都经 `Contracts` 的 `ITenantQuota` 检查租户的存储上限（套餐或租户自己的覆盖值）；用量显示在「我的订阅」与租户配额页。

## 页面

`src/modules/files`：文件库（上传、预览、副本）、存储配置、导出中心。

## 下一步

- [缓存与异步](./caching) · [多租户 SaaS](./multi-tenancy) · [配置参考](../configuration)
