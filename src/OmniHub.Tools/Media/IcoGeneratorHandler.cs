using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.Tools.Media;

public class IcoGeneratorHandler : IToolHandler
{
    public string HandlerId => "media.ico.generator";

    // Standard Windows Icon Dimensions
    private static readonly int[] StandardSizes = [16, 32, 48, 64, 128, 256];

    public async Task<ProcessExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        IProgress<LogEntry> progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (!request.Parameters.TryGetValue("InputImage", out var inputImage) || string.IsNullOrWhiteSpace(inputImage))
        {
            progress.Report(new LogEntry("❌ Missing parameter: InputImage", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "InputImage missing");
        }

        if (!File.Exists(inputImage))
        {
            progress.Report(new LogEntry($"❌ Image file not found: {inputImage}", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "File not found");
        }

        var outputIco = request.Parameters.TryGetValue("OutputIco", out var outVal) && !string.IsNullOrWhiteSpace(outVal)
            ? outVal
            : Path.ChangeExtension(inputImage, ".ico");

        progress.Report(new LogEntry($"🖼️ Loading source image: {inputImage}", LogLevel.Info));

        await Task.Yield();

        try
        {
            using var originalBitmap = new Bitmap(inputImage);
            progress.Report(new LogEntry($"📐 Original Dimensions: {originalBitmap.Width}x{originalBitmap.Height} px", LogLevel.Standard));

            var pngStreams = new List<(int Size, byte[] Data)>();

            foreach (var size in StandardSizes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress.Report(new LogEntry($"⚙️ Resampling layer: {size}x{size} px (Bicubic)...", LogLevel.Standard));

                using var resized = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(resized))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    graphics.Clear(Color.Transparent);
                    graphics.DrawImage(originalBitmap, 0, 0, size, size);
                }

                using var ms = new MemoryStream();
                resized.Save(ms, ImageFormat.Png);
                pngStreams.Add((size, ms.ToArray()));
            }

            // Ensure destination directory exists
            var outDir = Path.GetDirectoryName(outputIco);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            progress.Report(new LogEntry($"💾 Packaging Windows ICO binary to: {outputIco}", LogLevel.Info));

            using (var fileStream = new FileStream(outputIco, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(fileStream))
            {
                // Write ICONDIR (6 bytes)
                writer.Write((ushort)0); // Reserved. Must always be 0.
                writer.Write((ushort)1); // Image type: 1 for icon (.ICO)
                writer.Write((ushort)pngStreams.Count); // Number of images

                // Calculate initial image offset (Header + Entry count * 16 bytes)
                uint offset = (uint)(6 + (pngStreams.Count * 16));

                // Write ICONDIRENTRY for each layer (16 bytes each)
                foreach (var (size, data) in pngStreams)
                {
                    byte bDim = (byte)(size >= 256 ? 0 : size);
                    writer.Write(bDim);               // Width
                    writer.Write(bDim);               // Height
                    writer.Write((byte)0);            // Color count (0 for 32bpp/PNG)
                    writer.Write((byte)0);            // Reserved
                    writer.Write((ushort)1);          // Color planes
                    writer.Write((ushort)32);         // Bits per pixel
                    writer.Write((uint)data.Length);  // Size of image data
                    writer.Write(offset);             // Offset of image data
                    offset += (uint)data.Length;
                }

                // Write PNG image payloads
                foreach (var (_, data) in pngStreams)
                {
                    writer.Write(data);
                }
            }

            stopwatch.Stop();
            var outFi = new FileInfo(outputIco);
            progress.Report(new LogEntry(
                $"🎉 Windows Multi-size ICO successfully generated! ({outFi.Length / 1024.0:F1} KB, 6 layers: 16-256px in {stopwatch.Elapsed.TotalSeconds:F2}s)",
                LogLevel.Success));

            return new ProcessExecutionResult(0, true, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            progress.Report(new LogEntry("🛑 Icon generation cancelled.", LogLevel.Warning));
            return new ProcessExecutionResult(-1, false, stopwatch.Elapsed, "Cancelled");
        }
        catch (Exception ex)
        {
            progress.Report(new LogEntry($"💥 Failed to generate ICO: {ex.Message}", LogLevel.Error));
            return new ProcessExecutionResult(-1, false, stopwatch.Elapsed, ex.Message);
        }
    }
}

