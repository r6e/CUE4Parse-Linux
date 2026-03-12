using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;

namespace CUE4Parse_Conversion.Textures.BC;

public static class DetexHelper
{
    private static readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static string ManifestResourceName => _isWindows
        ? "CUE4Parse_Conversion.Resources.Detex.dll"
        : "CUE4Parse_Conversion.Resources.Detex.so";
    public static string DLL_NAME => _isWindows ? "Detex.dll" : "libDetex.so";

    private static Detex? Instance { get; set; }

    /// <summary>
    /// Initializes the Detex library with a given path.
    /// </summary>
    public static void Initialize(string path)
    {
        Instance?.Dispose();
        if (File.Exists(path))
            Instance = new Detex(path);
    }

    /// <summary>
    /// Initializes Detex with a pre-existing instance.
    /// </summary>
    public static void Initialize(Detex instance)
    {
        Instance?.Dispose();
        Instance = instance;
    }

    /// <summary>
    /// Returns the default absolute path where the Detex library will be extracted.
    /// </summary>
    public static string DefaultLibraryPath => Path.Combine(AppContext.BaseDirectory, DLL_NAME);

    /// <summary>
    /// Load the Detex library DLL.
    /// </summary>
    public static bool LoadDll(string? path = null)
    {
        var resolvedPath = path ?? DefaultLibraryPath;
        if (File.Exists(resolvedPath))
            return true;
        return LoadDllAsync(resolvedPath).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Decode the encoded data using the Detex library.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] DecodeDetexLinear(byte[] inp, int width, int height, bool isFloat, DetexTextureFormat inputFormat, DetexPixelFormat outputPixelFormat)
    {
        if (Instance is null)
        {
            const string message = "Detex decompression failed: not initialized";
            throw new Exception(message);
        }

        var dst = new byte[width * height * (isFloat ? 16 : 4)];
        Instance.DecodeDetexLinear(inp, dst, width, height, inputFormat, outputPixelFormat);
        return dst;
    }

    /// <summary>
    /// Asynchronously loads the Detex library from embedded resources.
    /// </summary>
    public static async Task<bool> LoadDllAsync(string? path)
    {
        try
        {
            var dllPath = path ?? DefaultLibraryPath;

            if (File.Exists(dllPath))
            {
                Log.Information($"Detex library already exists at \"{dllPath}\".");
                return true;
            }

            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ManifestResourceName);
            if (stream == null)
            {
                throw new MissingManifestResourceException($"Couldn't find {ManifestResourceName} in Embedded Resources.");
            }

            await using var dllFs = File.Create(dllPath);
            await stream.CopyToAsync(dllFs).ConfigureAwait(false);

            Log.Information($"Successfully extracted Detex library from embedded resources to \"{dllPath}\"");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Uncaught exception while loading Detex library");
            return false;
        }
    }

}
