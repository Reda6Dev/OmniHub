using System.Drawing;
using System.Drawing.Imaging;
using OmniHub.Core.Models;
using OmniHub.Tools.Media;
using Xunit;

namespace OmniHub.Tests;

public class IcoGeneratorTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldGenerateValidMultiSizeIco()
    {
        var tempPng = Path.Combine(Path.GetTempPath(), $"test_input_{Guid.NewGuid():N}.png");
        var tempIco = Path.Combine(Path.GetTempPath(), $"test_output_{Guid.NewGuid():N}.ico");

        try
        {
            // Create a test 512x512 PNG image
            using (var bmp = new Bitmap(512, 512, PixelFormat.Format32bppArgb))
            {
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.DarkSlateBlue);
                bmp.Save(tempPng, ImageFormat.Png);
            }

            var handler = new IcoGeneratorHandler();
            var request = new ToolExecutionRequest
            {
                Parameters = new Dictionary<string, string>
                {
                    { "InputImage", tempPng },
                    { "OutputIco", tempIco }
                }
            };

            var progress = new Progress<LogEntry>();
            var result = await handler.ExecuteAsync(request, progress, CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(File.Exists(tempIco));

            // Verify binary structure
            using var fs = new FileStream(tempIco, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            var reserved = reader.ReadUInt16();
            var type = reader.ReadUInt16();
            var count = reader.ReadUInt16();

            Assert.Equal(0, reserved);
            Assert.Equal(1, type);
            Assert.Equal(6, count); // 16, 32, 48, 64, 128, 256
        }
        finally
        {
            if (File.Exists(tempPng)) File.Delete(tempPng);
            if (File.Exists(tempIco)) File.Delete(tempIco);
        }
    }
}

