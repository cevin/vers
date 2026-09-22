# Vers

Vers 是一个使用 .NET 10、Avalonia 和 Semi.Avalonia 开发的跨平台运行时版本切换工具。它可以
为 `php`、`java`、`node` 或任意自定义分组创建命令代理，并根据当前目录、临时环境变量或默认
配置选择真正需要执行的程序。

<p align="center">
  <img src="docs/main.png" alt="Vers 主界面" width="900">
</p>

## 使用方法

1. 新建分组。分组名称就是用户实际执行的命令，例如 `php`、`java`、`mvn` 或 `gradle`。Vers
   会在 `bin` 目录中创建同名代理。
2. 在分组中添加一个或多个版本。每个版本需要配置真实可执行文件，也可以配置该版本专用的环境变量。
3. 选择默认版本；如果某个项目需要不同版本，再为该分组添加项目目录映射。
4. 点击一次 **设置 PATH**，然后重新打开已经存在的终端。

当当前目录是已配置的项目目录，或者位于该目录的任意子目录中时，执行分组命令会自动路由到该项目
选择的预设版本：

```bash
cd /work/my-project
php --version
java --version
mvn --version
gradle --version
```

每个分组都有独立的项目映射，因此同一个项目可以同时使用 PHP 84、Java 21、Maven 3.9 和指定的
Gradle 版本。Maven 通常使用的实际命令是 `mvn`；如果需要拦截 Maven，请将分组名称设置为
`mvn`。

> [!NOTE]
> Maven 和 Gradle 会在内部启动 Java。如果选中的 `mvn` 或 `gradle` 版本配置了
> `JAVA_HOME`，启动脚本通常会直接执行 `$JAVA_HOME/bin/java`，不会经过 Vers 的 `java`
> 代理。如果没有设置 `JAVA_HOME`，并且启动脚本通过 PATH 查找 `java`，则应当同时在 `java`
> 分组中为同一个项目目录设置版本；否则 Vers 会使用 Java 分组的默认版本。Gradle 还可能复用
> 之前使用旧 JVM 启动的 daemon，修改 Java 配置后应执行一次 `gradle --stop`。

## 托管文件

在 macOS 和 Linux 上，Vers 将托管文件放在 `~/.vers`。在 Windows 上，Vers 使用启动
`ver.exe` 时的工作目录。所有平台都可以通过 `VERS_HOME` 覆盖根目录。

```text
<vers-root>/
├── util/tool[.exe]
├── bin/<分组>[.exe]
└── data/settings.json
```

新建或删除分组时，Vers 会同时创建或删除 `bin` 中对应的代理程序。根目录选择由平台 adapter
负责，因此以后封装成 macOS `.app` 时不会依赖 Finder 提供的工作目录。

Vers 不会向受管理的项目目录写入任何文件，也不会修改或删除系统中已经安装的真实运行时。它只会
通过 PATH 中的代理程序改变命令路由，不污染项目目录。

## PATH 优先级

> [!WARNING]
> Shell 总是执行 `PATH` 中最先匹配到的程序。如果 `/usr/bin` 位于 Vers 的 `bin` 之前，且存在
> `/usr/bin/php`，执行 `php` 时可能不会经过 Vers。这个规则适用于所有分组和平台。

Vers 只会在用户点击 **设置 PATH** 后修改 PATH：

- macOS/Linux：替换用户 shell profile 中唯一的 Vers 托管区块，并将 `~/.vers/bin` 放在最前面。
- Windows：将 Vers 的 `bin` 放在系统 PATH 最前面，因此需要通过 UAC 获取管理员权限。

重复设置不会产生重复路径。PATH 发生变化后，需要重新打开已经存在的终端。

Vers 会在启动和配置发生变化时模拟检查所有分组的命令解析结果。如果 PATH 优先找到了其他程序，
或者完全找不到该命令，分组右侧会显示 `[WARN]`。将鼠标移到警告上，可以查看实际命中的路径和
预期的 Vers 代理路径。

Windows 还会为每个代理生成一个无扩展名的 WSL 启动脚本。在 WSL 正常导入 Windows PATH 的
情况下，执行 `php` 等命令时会把转换后的工作目录传给 Windows 代理。

## 版本解析

代理选择版本的顺序为：

1. `VER_<分组>_VERSION`，例如 `VER_PHP_VERSION=84`
2. 最长匹配的项目目录
3. 分组默认版本

无需打开 GUI，即可把当前目录绑定到某个版本：

```bash
php @bind:72
java @bind:27
```

`@bind:` 后的值必须与配置中的版本名称完全匹配，字母大小写除外。执行没有版本值的 `@bind` 或
`@bind:` 时，会列出所有可用版本名称及其可执行文件路径。绑定只更新 `settings.json`，不会启动
真正的 PHP、Java 或其他运行时。

每个版本都可以配置独立的环境变量。子进程会继承代理进程的环境，并可使用 `${NAME}`、`$NAME`
或 `%NAME%` 引用已有变量。`JAVA_HOME` 也是普通的手动配置项，Vers 不会自动推断其值。

## 构建和测试

需要安装 .NET 10 SDK。

```bash
dotnet build Vers.slnx
dotnet test Vers.slnx
```

测试首次启动行为时，可以使用 `VERS_HOME`，避免修改真实的 `~/.vers`：

```bash
VERS_HOME="$PWD/tmp/test/gui" dotnet run --project src/Vers.Gui/Vers.Gui.csproj
```

在 macOS 上构建 `.app`。脚本会自动识别 Apple Silicon 或 Intel：

```bash
./scripts/mac.sh
open artifacts/macos/Vers.app
```

在 Windows PowerShell 中构建 Windows 可执行文件。脚本会自动识别 x64 或 ARM64：

```powershell
./scripts/windows.ps1
./artifacts/windows/ver.exe
```

GUI 和生成的命令代理均以自包含方式发布，目标机器无需单独安装 .NET。原生渲染库会嵌入最终的
单文件 GUI 可执行程序。

可编辑的图标源文件位于 `src/Vers.Gui/Assets/logo.png`。生成的 `.ico` 和 `.icns` 文件与源图
放在同一目录，并由各平台打包脚本自动嵌入。

## 开源协议

Vers 使用 [MIT License](LICENSE) 发布。
