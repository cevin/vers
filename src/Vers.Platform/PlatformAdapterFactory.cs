namespace Vers.Platform;

public static class PlatformAdapterFactory
{
    public static IPlatformAdapter Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsPlatformAdapter();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new UnixPlatformAdapter(isMacOS: true);
        }

        if (OperatingSystem.IsLinux())
        {
            return new UnixPlatformAdapter(isMacOS: false);
        }

        throw new PlatformNotSupportedException("Vers supports Windows, macOS, and Linux.");
    }
}
