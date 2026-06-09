namespace SimaiAutomator.Core.Services;

/// <summary>
/// Handles cover image placement for AquaMai LocalAssets.
/// </summary>
public static class CoverHandler
{
    /// <summary>
    /// Copies the cover image to LocalAssets/ with the correct 6-digit ID name.
    /// </summary>
    public static void CopyToLocalAssets(string? coverPath, string localAssetsDir, int id6)
    {
        if (string.IsNullOrEmpty(coverPath) || !File.Exists(coverPath))
            return;

        Directory.CreateDirectory(localAssetsDir);

        var ext = Path.GetExtension(coverPath);
        var dest = Path.Combine(localAssetsDir, $"{id6:D6}{ext}");

        File.Copy(coverPath, dest, overwrite: true);
    }

    /// <summary>
    /// Copies BGA to MovieData/ if present.
    /// Note: BGA format conversion (mp4→usm→dat) requires WannaCRI,
    /// which is optional. For now, just copy the raw file as a placeholder.
    /// </summary>
    public static void CopyBga(string? bgaPath, string movieDataDir, int id6)
    {
        if (string.IsNullOrEmpty(bgaPath) || !File.Exists(bgaPath))
            return;

        Directory.CreateDirectory(movieDataDir);

        var dest = Path.Combine(movieDataDir, $"{id6:D6}.dat");
        File.Copy(bgaPath, dest, overwrite: true);
    }
}
