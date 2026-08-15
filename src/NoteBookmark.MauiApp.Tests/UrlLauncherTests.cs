using FluentAssertions;
using Microsoft.JSInterop;
using Moq;
using NoteBookmark.MauiApp.Data;
using NoteBookmark.SharedUI;
using Xunit;

namespace NoteBookmark.MauiApp.Tests;

public class UrlLauncherTests
{
    [Fact]
    public async Task JsUrlLauncher_WithNullOrWhitespaceUrl_DoesNotInvokeJs()
    {
        var jsMock = new Mock<IJSRuntime>();
        var launcher = new JsUrlLauncher(jsMock.Object);

        await launcher.OpenUrlAsync(null);
        await launcher.OpenUrlAsync("   ");

        jsMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task JsUrlLauncher_WithValidUrl_InvokesWindowOpen()
    {
        var jsMock = new Mock<IJSRuntime>();
        jsMock.Setup(x => x.InvokeAsync<object>("open", It.IsAny<object[]>()))
              .ReturnsAsync(null!);

        var launcher = new JsUrlLauncher(jsMock.Object);

        await launcher.OpenUrlAsync("https://example.com");

        jsMock.Verify(x => x.InvokeAsync<object>("open", It.Is<object[]>(args =>
            args.Length == 2 && (string)args[0] == "https://example.com" && (string)args[1] == "_blank"
        )), Times.Once);
    }

    [Fact]
    public async Task MauiUrlLauncher_WithNullOrInvalidUrl_DoesNotThrow()
    {
        var launcher = new MauiUrlLauncher();

        var act1 = async () => await launcher.OpenUrlAsync(null);
        var act2 = async () => await launcher.OpenUrlAsync("not-a-valid-url");
        var act3 = async () => await launcher.OpenUrlAsync("https://example.com");

        await act1.Should().NotThrowAsync();
        await act2.Should().NotThrowAsync();
        await act3.Should().NotThrowAsync();
    }
}
