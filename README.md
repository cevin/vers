# Vers

Vers is a cross-platform GUI runtime switcher built with .NET 10, Avalonia, and Semi.Avalonia.
It creates lightweight command proxies such as `php`, `java`, and `node`, then selects the real
executable from the current directory, a temporary environment override, or the configured default.

## Runtime behavior

On first launch, `ver` uses its current working directory as the installation root and creates:

```text
./
├── ver[.exe]
├── util/tool[.exe]
├── bin/<group>[.exe]
└── data/settings.json
```

Creating or deleting a group creates or deletes the matching proxy in `bin`. The GUI can append
`bin` to the user PATH after the user clicks **Set PATH**. Existing terminals must then be reopened.

Windows builds also create an extensionless shell companion for every proxy. With standard WSL
Windows-PATH import enabled, commands such as `php` work from WSL and pass the converted working
directory to the Windows proxy.

Resolution order for a proxy is:

1. `VER_<GROUP>_VERSION`, for example `VER_PHP_VERSION=php84`
2. The longest matching project directory
3. The configured default version

Bind the current directory from any proxy without opening the GUI:

```bash
php @bind:82
java @bind:27
```

The value after `@bind:` must exactly match a configured version name, ignoring letter case. Binding
updates the group's project-directory mapping and does not launch the underlying runtime.

Each version can define environment variables. They inherit the proxy process environment and may
reference an existing value with `${NAME}`, `$NAME`, or `%NAME%`. `JAVA_HOME` is an ordinary manual
version setting; Vers does not infer it.

## Build and test

.NET 10 SDK is required.

```bash
dotnet build Vers.slnx
dotnet test Vers.slnx
./scripts/publish.sh osx-arm64
```

To test first-run behavior in a temporary working directory:

```bash
mkdir -p tmp/test/gui
cd tmp/test/gui
dotnet run --project ../../../src/Vers.Gui/Vers.Gui.csproj
```

Startup only checks PATH. The GUI changes the user PATH after the user clicks **Add to PATH**.

On PowerShell:

```powershell
./scripts/publish.ps1 win-x64
```

Published GUI builds and generated command proxies are self-contained, so the target machine does
not need a separate .NET installation. Proxies are fully trimmed and compressed to keep each copied
command reasonably small.
