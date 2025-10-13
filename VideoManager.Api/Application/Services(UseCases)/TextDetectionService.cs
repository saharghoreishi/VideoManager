using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using FFMpegCore;
using Tesseract;
using System.Globalization;
using VideoManager.Api.Application.Interfaces;

namespace VideoManager.Api.Application.Services
{
    /// <summary>
    /// TextDetectionService
    /// 
    /// Dieser Dienst extrahiert in einem definierten Intervall (FPS) Frames aus einem Video (via FFmpeg),
    /// führt eine leichte Bildvorverarbeitung (ImageSharp) durch und erkennt anschließend Text mittels Tesseract OCR.
    /// Ziel ist es, ein bestimmtes Zielwort/-phrase in irgendeinem Frame des Videos zu finden.
    /// 
    /// Architektur-Hinweis:
    /// - In einer strikt "Clean Architecture" sollte das Interface (z. B. ITextDetectionService) in eine innere Schicht
    ///   (Application/Core) ausgelagert werden. Diese Implementierung ist dann Infrastruktur.
    /// </summary>
    public sealed class TextDetectionService : ITextDetectionService, IDisposable
    {
        private readonly string _tessDataPath;
        private readonly string _lang;
        private readonly TesseractEngine _engine;

        /// <summary>
        /// Initialisiert den OCR-Dienst.
        /// </summary>
        /// <param name="tessDataPath">Pfad zum Tesseract-Datenordner (z. B. "tessdata" mit eng.traineddata etc.).</param>
        /// <param name="lang">Sprachkürzel für Tesseract, z. B. "eng", "eng+deu" oder "eng+fas".</param>
        /// <param name="ffmpegBinFolder">Optionaler Pfad zu ffmpeg/ffprobe, falls sie nicht im System-PATH liegen.</param>
        /// <exception cref="DirectoryNotFoundException">Wenn der tessdata-Ordner nicht existiert.</exception>
        public TextDetectionService(string tessDataPath = "tessdata", string lang = "eng", string? ffmpegBinFolder = null)
        {
            _tessDataPath = tessDataPath;
            _lang = lang;

            // Validierung tessdata
            if (!Directory.Exists(_tessDataPath))
                throw new DirectoryNotFoundException($"tessdata not found: {_tessDataPath}");

            // FFmpeg-Binaries konfigurieren, falls nicht im PATH vorhanden.
            if (!string.IsNullOrWhiteSpace(ffmpegBinFolder))
            {
                GlobalFFOptions.Configure(opt =>
                {
                    opt.BinaryFolder = ffmpegBinFolder;             // z. B. C:\ffmpeg\bin
                    opt.TemporaryFilesFolder = Path.GetTempPath();  // temporäres Verzeichnis
                });
            }

            // Tesseract Engine initialisieren (wirft Ausnahmen, wenn Sprache fehlt/beschädigt ist).
            _engine = new TesseractEngine(_tessDataPath, _lang, EngineMode.Default);
        }

        /// <summary>
        /// Prüft, ob ein gegebener Text (case-insensitive) in einem der extrahierten Videoframes vorkommt.
        /// </summary>
        /// <param name="videoPath">Dateipfad des Videos.</param>
        /// <param name="targetText">Gesuchter Zieltext (case-insensitive).</param>
        /// <param name="sampleRateFps">
        /// Wie viele Bilder pro Sekunde extrahiert werden sollen (z. B. 1.0 = 1 Frame/Sekunde).
        /// Größere Werte beschleunigen die Suche, können aber Text übersehen.
        /// </param>
        /// <param name="cancellationToken">Abbruch-Token für lange Operationen.</param>
        /// <returns>True, wenn der Text in irgendeinem Frame erkannt wurde, sonst False.</returns>
        /// <exception cref="ArgumentException">Wenn videoPath leer ist.</exception>
        /// <exception cref="FileNotFoundException">Wenn die Videodatei nicht existiert.</exception>
        /// <exception cref="InvalidOperationException">Wenn die Tesseract-Engine nicht initialisiert ist.</exception>
        public async Task<bool> ContainsTextAsync(
            string videoPath,
            string targetText,
            double sampleRateFps = 1.0,
            CancellationToken cancellationToken = default)
        {
            // Eingaben validieren
            if (string.IsNullOrWhiteSpace(videoPath))
                throw new ArgumentException("videoPath is required", nameof(videoPath));
            if (!File.Exists(videoPath))
                throw new FileNotFoundException("Video not found", videoPath);
            if (string.IsNullOrWhiteSpace(targetText))
                return false;

            if (_engine is null)
                throw new InvalidOperationException("Tesseract engine not initialized");

            // Arbeitsverzeichnis für extrahierte Frames
            // Hinweis: Wir nutzen ein eindeutiges TMP-Verzeichnis und löschen es am Ende.
            var framesDir = Path.Combine(Path.GetTempPath(), "vm_frames_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(framesDir);
            var pattern = Path.Combine(framesDir, "frame_%06d.png");

            try
            {
                // FPS sauber als Culture-invariant String ausgeben (Punkt statt Komma).
                var fps = Math.Max(0.1, sampleRateFps);
                var fpsArg = fps.ToString(CultureInfo.InvariantCulture);

                // FFmpeg: Frames extrahieren (-vf fps=...)
                // Beispiel: ffmpeg -i input.mp4 -vf fps=1 -f image2 frame_%06d.png
                await FFMpegArguments
                    .FromFileInput(videoPath)
                    .OutputToFile(pattern, overwrite: true, options => options
                        .WithCustomArgument($"-vf fps={fpsArg}")    // Kompatibler Weg für diverse FFMpegCore-Versionen
                        .ForceFormat("image2"))
                    .ProcessAsynchronously();

                var normalizedTarget = Normalize(targetText);

                // Über alle erzeugten Frames iterieren
                foreach (var imgPath in Directory.EnumerateFiles(framesDir, "frame_*.png"))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Leichte Vorverarbeitung zur Verbesserung der OCR-Ergebnisse:
                    // - Graustufen
                    // - leichte Helligkeits-/Kontrastanpassung
                    // Tipp: Falls Untertitel/Einblendungen meist unten sind, kann hier gezielt gecroppt werden.
                    using var img = Image.Load(imgPath);
                    img.Mutate(c =>
                    {
                        c.Grayscale();
                        c.Brightness(1.15f);
                        c.Contrast(1.10f);
                        // Beispiel für Crop auf unteres Viertel:
                        // var h = img.Height; var w = img.Width;
                        // c.Crop(new Rectangle(0, (int)(h * 0.75), w, (int)(h * 0.25)));
                    });

                    // Ohne erneutes Schreiben auf die Platte weiterreichen (direkt aus dem Speicher):
                    using var ms = new MemoryStream();
                    img.SaveAsPng(ms);
                    ms.Position = 0;

                    using var pix = Pix.LoadFromMemory(ms.ToArray());
                    using var page = _engine.Process(pix);
                    var text = page.GetText() ?? string.Empty;

                    // Normalisieren und prüfen, ob der gesuchte Text enthalten ist
                    if (!string.IsNullOrWhiteSpace(text) &&
                        Normalize(text).Contains(normalizedTarget))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                // Aufräumen: temporäre Frames löschen (Fehler beim Löschen ignorieren).
                try { Directory.Delete(framesDir, true); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Normalisiert Text für robuste Vergleiche:
        /// - Kleinschreibung
        /// - Mehrfache Whitespaces → ein Leerzeichen
        /// - Trim
        /// </summary>
        private static string Normalize(string text)
        {
            var lower = text.ToLowerInvariant();
            lower = Regex.Replace(lower, @"\s+", " ").Trim();
            return lower;
        }

        /// <summary>
        /// Ressourcen-Freigabe für Tesseract.
        /// </summary>
        public void Dispose()
        {
            _engine?.Dispose();
        }
    }
}
