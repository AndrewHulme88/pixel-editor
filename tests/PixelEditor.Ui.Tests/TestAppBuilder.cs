using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using pixel_editor;

[assembly: AvaloniaTestApplication(typeof(PixelEditor.Ui.Tests.TestAppBuilder))]

namespace PixelEditor.Ui.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
