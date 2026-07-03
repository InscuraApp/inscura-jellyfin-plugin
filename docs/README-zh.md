# Inscura 的 Jellyfin 元数据插件

语言： [English](../README.md) | **简体中文** | [日本語](README-ja.md) | [한국어](README-ko.md)

Inscura 是一个本地媒体库应用，可以整理影片信息、演员、分类、封面、背景图和预告片等资料。Jellyfin 负责播放和管理媒体库，但它自身并不知道 Inscura 已经整理好的这些数据。

这个插件用于把 Inscura 的本地接口服务接入 Jellyfin。Jellyfin 刷新电影元数据时，插件会把标题、简介、发布日期、评分、类型、制作方、演员、封面、背景图和缩略图等数据写入 Jellyfin。

插件只读取 Inscura 的媒体库数据和库内生成的图片资源，不会下载、移动、重命名、删除或修改原始媒体文件。

## 当前能力

- 按 Jellyfin 传入的真实文件路径和文件名优先匹配，减少因 Jellyfin 标题被手动修改导致的误匹配。
- 找不到路径匹配时，会继续尝试文件名中的识别码和 Jellyfin 标题。
- 支持写入 Jellyfin 可接收的电影元数据字段，包括标题、原始标题、简介、发布日期、年份、评分、类型、制作方、国家、标签、演员、导演、编剧、制片人、海报、背景图和缩略图。
- 支持从 Inscura 导入本地图片资源，作为 Jellyfin 的 Primary、Backdrop、Thumb、Banner、Logo、Art 和 Disc 图片候选。
- 远程预告片当前只导入 YouTube 地址。**非 YouTube 本地或在线预告片请通过 Inscura App 的 NFO 功能导出后交给 Jellyfin 扫描。**

## 开启 Inscura 本地接口服务

1. 打开 Inscura，并打开要同步到 Jellyfin 的媒体库。
2. 进入设置中的 API 接口服务设置，开启本地接口服务。
3. 如果接口服务使用令牌鉴权，请保存设置页显示的接口令牌。
4. 在 Jellyfin 服务器所在设备上访问健康检查地址，确认服务可达。

示例：

```bash
curl "http://[ip]:28687/api/v1/health"
```

如果 Jellyfin 和 Inscura 不在同一台机器上，插件里的服务地址不能填写 `127.0.0.1`，要填写运行 Inscura 的那台电脑在局域网中的地址，例如：

```text
http://[ip]:28687
```

本地接口服务会跟随当前媒体库生命周期运行：媒体库打开且服务已启用时监听端口，**媒体库锁定、关闭或应用退出时停止服务。**

## 推荐安装方式：插件仓库

这种方式由 Jellyfin 读取插件仓库的 `manifest.json`，安装和后续升级都更方便。中国大陆网络环境建议使用 jsDelivr 地址。

仓库名称：

```text
Inscura
```

仓库 URL：

```text
https://cdn.jsdelivr.net/gh/InscuraApp/inscura-jellyfin-plugin@release/manifest.json
```

安装步骤：

1. 进入 Jellyfin 控制台。
2. 打开插件目录或插件设置页。
3. 进入存储库或 Repositories。
4. 点击添加。
5. 名称填写 `Inscura`。
6. URL 填写上面的 jsDelivr manifest 地址。
7. 保存后进入插件目录或 Catalog。
8. 找到 `Inscura` 并安装。
9. 重启 Jellyfin。

如果你的网络可以稳定访问 GitHub，也可以使用 release 分支中的 manifest 文件；但面向中国大陆环境，优先使用上面的 jsDelivr 地址。

## 备用安装方式：直接下载 zip

如果 Jellyfin 无法通过插件仓库安装，可以手动下载插件 zip 并放入 Jellyfin 插件目录。

### 获取下载地址

打开 manifest 文件：

```text
https://cdn.jsdelivr.net/gh/InscuraApp/inscura-jellyfin-plugin@release/manifest.json
```

复制其中最新版本的 `sourceUrl`，下载对应的 zip 文件。

当前发布文件的地址格式类似：

```text
https://cdn.jsdelivr.net/gh/InscuraApp/inscura-jellyfin-plugin@release/releases/Inscura_[版本号].zip
```

### 放置插件文件

找到 Jellyfin 的数据目录，并进入其中的 `plugins` 目录。常见位置如下；如果你的系统不一致，以 Jellyfin 控制台显示的数据目录或部署时的实际映射路径为准。

| 环境 | 常见插件目录 |
| --- | --- |
| Linux 套件安装 | `/var/lib/jellyfin/plugins/` |
| Docker | 宿主机映射到容器 `/config/plugins/` 的目录 |
| Windows 服务安装 | `%ProgramData%\Jellyfin\Server\plugins\` |
| Windows 便携或用户模式 | Jellyfin 数据目录下的 `plugins\` |

手动安装步骤：

1. 停止 Jellyfin，或准备在复制完成后重启 Jellyfin。
2. 在 `plugins` 目录下创建版本目录，例如：

```text
plugins/Inscura_0.1.0.0/
```

3. 解压 zip，把其中的文件放到该目录下。
4. 目录中只需要包含插件 DLL：

```text
Jellyfin.Plugin.Inscura.dll
```

5. 确认 Jellyfin 进程对该文件有读取权限。
6. 重启 Jellyfin。
7. 进入控制台的插件页面，确认 `Inscura` 已加载。

## 在 Jellyfin 中启用插件

安装插件后，还需要在电影库中启用 Inscura 元数据源。

1. 进入 Jellyfin 控制台。
2. 打开媒体库，编辑你的电影库。
3. 在元数据下载器中启用 `Inscura`。
4. 建议把 `Inscura` 排在其他电影元数据源前面。
5. 在图片下载器中启用 `Inscura`。
6. 保存媒体库设置。
7. 进入插件设置页，填写 Inscura API 地址和接口令牌。
8. 对影片执行刷新元数据或识别。

首次使用时，建议先选择少量影片刷新元数据，确认标题、演员、封面和背景图符合预期后，再批量刷新资料库。

## 插件设置说明

| 设置 | 说明 |
| --- | --- |
| Inscura API URL | Inscura 本地接口服务地址。Jellyfin 和 Inscura 不在同一台机器时必须填写局域网地址 |
| API Token | 本地接口服务使用令牌鉴权时填写；如果 Inscura 设置为无鉴权，可以留空 |
| Search result limit | 每次匹配时从 Inscura 请求的候选数量 |
| Request timeout | 请求 Inscura 本地接口的超时时间 |
| Enable movie metadata provider | 启用或关闭电影元数据刮削 |
| Enable image provider | 启用或关闭图片导入 |
| Import YouTube trailers | 启用后只导入 YouTube 远程预告片 |
| Use Inscura preview images as Thumb candidates | 将 Inscura 预览图作为 Jellyfin Thumb 图片候选 |
| Use gallery images as Backdrop candidates | 将 Inscura 图库照片作为 Jellyfin Backdrop 图片候选 |

## 使用建议

- 首次使用时，先刷新少量影片，确认匹配结果符合预期后再批量刷新。
- 如果 Jellyfin 中影片标题曾被手动改过，插件仍会优先使用真实文件路径和文件名匹配，不依赖标题作为唯一依据。
- 如果 Inscura 服务地址或令牌修改过，需要在 Jellyfin 插件设置中同步更新，然后刷新元数据。
- 如果 Inscura 媒体库被锁定或关闭，Jellyfin 将无法读取元数据。
- 非 YouTube 预告片不要依赖插件导入，请通过 Inscura App 的 NFO 功能导出给 Jellyfin 扫描。

## 排查问题

### Jellyfin 看不到 Inscura 插件

1. 如果使用仓库安装，确认仓库 URL 可以从 Jellyfin 服务器访问。
2. 如果手动安装，确认插件文件位于 Jellyfin 数据目录下的 `plugins/Inscura_[版本号]/`。
3. 确认目录中存在 `Jellyfin.Plugin.Inscura.dll`。
4. 确认 Jellyfin 进程有读取该文件的权限。
5. 重启 Jellyfin。
6. 重新打开 Jellyfin 网页端，进入控制台插件页面检查。

### Jellyfin 能看到插件，但没有元数据

1. 在 Jellyfin 服务器所在设备上访问 Inscura 健康检查地址。
2. 确认 Inscura 媒体库已经打开，且本地接口服务处于开启状态。
3. 确认插件里的 Inscura API URL 不是错误的 `127.0.0.1`。
4. 如果接口使用令牌鉴权，确认插件里填写了正确令牌。
5. 确认电影库的元数据下载器中已经启用 `Inscura`。
6. 对影片执行刷新元数据，刷新模式选择覆盖所有元数据。

### 封面、背景图或演员头像不显示

1. 确认 Jellyfin 服务器能访问 Inscura API URL。
2. 如果接口使用令牌鉴权，确认插件令牌正确。
3. 确认 Inscura 中对应媒体或演员确实有可用图片资源。
4. 确认电影库的图片下载器中已经启用 `Inscura`。
5. 刷新该影片元数据，并在需要时选择替换现有图片。

### 预告片没有导入

1. 确认插件设置中启用了 `Import YouTube trailers`。
2. 确认 Inscura 中对应预告片地址是 YouTube 地址。
3. 当前插件不会导入非 YouTube 本地或在线预告片。
4. 如需导入非 YouTube 预告片，请通过 Inscura App 的 NFO 功能导出后交给 Jellyfin 扫描。

## 升级插件

### 通过插件仓库升级

1. 确认插件仓库 URL 仍然可访问。
2. 进入 Jellyfin 控制台的插件页面。
3. 检查 `Inscura` 是否有可用更新。
4. 安装更新。
5. 重启 Jellyfin。

### 手动升级

1. 停止 Jellyfin。
2. 下载新版本 zip。
3. 在 `plugins` 目录下创建新的版本目录，例如 `Inscura_0.1.1.0`。
4. 解压新版本 zip 到该目录。
5. 保留旧版本目录，直到确认新版本可以正常加载。
6. 启动 Jellyfin。
7. 确认新版本正常后，再删除旧版本目录。

升级后如果 Jellyfin 网页端仍显示旧设置或旧元数据，先强制刷新浏览器页面，再重新打开插件设置或刷新影片元数据。
