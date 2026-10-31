# 文件与存储

Files 包（Tier 2）在框架对象存储（`IBlobStore`）之上管理存储配置、文件与多副本、秒传、签名地址、分片上传，以及导出中心与导入历史。

## 存储配置

`SysStorageConfig`（平台库实体，`IOwnedByTenant`：平台与租户各有自己的配置；访问密钥经 `ISecretProtector` 加密、不回显）。

| 类型 | 实现 |
| --- | --- |
| `Local` | 框架 `ObjectStorage.FileSystem`，根目录 `BasicApp:Files:LocalRootPath`（缺省 `App_Data/files`）；所有本地配置共用命名实例 `local` |
| `S3` / `Oss` / `Cos` / `MinIO` | 框架 `ObjectStorage.S3`，按 `ServiceUrl` 接 S3 兼容服务；每个配置一个命名实例 `storage-<配置 ID>` |

存储配置在运行时译成框架对象存储注册表的命名实例（框架 ADR-0050），健康项 `objectstorage.store.<名>` 随之出现在 `/health/ready`。

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

签名地址由存储签出（框架 ADR-0049）：S3 兼容存储直连存储，本地存储走宿主的 `/blobs` 签名端点（密钥 `XiHan:ObjectStorage:SignedUrls:Secret`）。缺省 30 分钟有效，最长 `MaxLinkLifetime`（7 天）。

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

都只要求登录：后台以**发起人的凭据**逐页调用对应的列表操作，权限、租户、数据范围与字段安全照常判定。结果存成 7 天的临时文件，进度经 `TaskProgress` 推送。前端所有 SchemaPage 列表页都接了导出中心。

## 导入历史

`import-histories.create` / `recent`（只要求登录）：导入本身由前端逐行调用资源的新建操作完成，后端只记历史。

## 配额

上传、秒传与分片完成都经 `Contracts` 的 `ITenantQuota` 检查租户的存储上限（套餐或租户自己的覆盖值）；用量显示在「我的订阅」与租户配额页。

## 页面

`src/modules/files`：文件库（上传、预览、副本）、存储配置、导出中心。

## 下一步

- [缓存与异步](./caching) · [多租户 SaaS](./multi-tenancy) · [配置参考](../configuration)
