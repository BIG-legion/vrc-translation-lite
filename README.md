# VRC Translation Lite

适用于 VRCT 的轻量安装器、图形启动器和本地 New API 兼容中转。

**作者：little-legion**

> 仅供个人非商业用途及免费分享，禁止商用、收费、售卖或捆绑收费。

## 功能

- 图形化安装与启动，无需手动打开多个命令行窗口。
- 自动备份并配置 VRCT 的 LM Studio 接口地址。
- 支持在 VRCT 中选择最多 3 个 New API 模型或模型别名。
- 中转仅监听 `127.0.0.1`，默认连接本机 New API `http://127.0.0.1:3000`。
- 强制温度为 0，只允许返回译文。
- 保留最多 15 条短上下文，辅助判断语气、指代与高置信度语音识别错误。
- New API 与兼容中转可隐藏运行；关闭 VRCT 后自动结束本次启动的后台服务。
- New API 令牌使用 Windows DPAPI 加密保存。

## 获取安装包

本 GitHub 仓库只保存源码和文档，不上传安装 ZIP。安装包由作者 little-legion 另行免费分享。

安装包不包含 VRCT、New API、API 令牌、数据库、日志或用户私人配置。

## 使用前准备

1. 从 [VRCT 官方 Releases](https://github.com/misyaguziya/VRCT/releases/latest) 下载并解压 VRCT。
2. 从 [New API 官方 Releases](https://github.com/QuantumNous/new-api/releases/latest) 下载并准备 New API。
3. 打开 New API，在浏览器中完成管理员初始化、渠道和令牌配置。
4. 运行一次 VRCT 后正常关闭，让它生成 `config.json`。
5. 获得作者免费分享的安装包后，解压并运行“安装 VRC 翻译助手.exe”。

## 仓库结构

- `src/Installer.cs`：图形安装器。
- `src/Launcher.cs`：图形启动器。
- `bridge/bridge.py`：localhost 兼容中转。
- `docs/`：安装、安全和许可说明。
- `BUILD.txt`：本地构建说明。

## 安全说明

- 中转只绑定 `127.0.0.1`，不会向局域网开放。
- 日志不会记录翻译正文或 Authorization 内容。
- 自动修改 VRCT 配置前会创建带时间戳的备份。
- 仓库与安装包不包含作者或用户的 API 令牌及私人配置。

## 许可

本项目使用自定义非商业许可，详情见 [LICENSE.txt](LICENSE.txt)。这不是 OSI 认可的开源许可证。

VRCT、New API 及其他第三方项目的名称、代码和相关权利归各自权利人所有。本项目并非它们的官方项目。
