# Innocence

这是一个可以帮助你在特定的时间加密敏感文件的程序。

Innocence 服务启动时会执行检查，如果当前系统时间大于设置的时间点或超过设置的时间范围，程序将开始加密文件和文件名并创建一个 .recovery 文件。如果加密是意料之外的，可以通过 .recovery 文件进行恢复。

**注意：Innocence 的功能还未完善，目前仅能够加密文件名且无法恢复（通过 display-recovery 命令查看和解密文件名信息）。**

#### 使用

1. 安装 Innocence 服务

2. 添加需要被保护的目录

3. 设置一个时间点或时间范围

4. 创建一个密钥并添加（通过 .recovery 文件进行恢复时仍需要提供此密钥）

5. 保存配置

#### 示例

```powershell
Innocence> install
Innocence> set-dir d:\example\files
Innocence> set-range 2592000
Innocence> createKey:d:\myKey -size:2048
Innocence> installKey:d:\myKey
Innocence> saveConfigs -y
```

## 命令

| 名称                                    | 描述                                                         |
| --------------------------------------- | ------------------------------------------------------------ |
| install                                 | 安装 Innocence 服务。                                        |
| uninstall                               | 卸载 Innocence 服务。                                        |
| start                                   | 启动 Innocence 服务。                                        |
| stop                                    | 停止 Innocence 服务。                                        |
| createKey:<文件> [ -size:<值> ]         | 创建一个新密钥。<br />*<文件>：指定密钥文件名<br />-size:<值>：指定密钥大小，默认为 2048* |
| set-dir <目录> [ -remove ]              | 将一个目录添加到保护列表。<br />*<目录>：指定要添加的目录<br />-remove：移除指定目录* |
| set-range <值>                          | 设置开始加密时间点（日期）或时间范围（秒）。<br />*<值>：时间点（类型：DateTime）或时间范围（类型：double）* |
| display-lastshutdown                    | 显示 Innocence 记录的系统最后一次关闭的时间。                |
| display-dirs                            | 显示保护目录列表。                                           |
| display-range                           | 显示开始加密时间点（日期）或时间范围（秒）。                 |
| display-recovery <文件> [ -key:<文件> ] | 显示指定的恢复信息。<br />*<文件>：指定要显示的 .recovery 文件<br />-key:<文件>：指定用于解密的密钥文件* |
| service:<命令>                          | 执行服务命令。<br />*Encrypt：忽略检查条件，开始加密文件<br />CheckAll：执行检查，符合条件时开始加密文件<br />Refresh：刷新配置文件* |
| saveconfigs [ -y \| -n ]                | 保存配置文件。<br />*-y：跳过询问，刷新配置文件并执行检查<br />-n：跳过询问，不执行任何操作* |

