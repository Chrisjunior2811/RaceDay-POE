namespace RaceDay.Web.Infrastructure;

/// <summary>Quick checks on an image before it is sent to the API (the API repeats the checks).</summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    /// <summary>Returns an error message, or null when the file is acceptable.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "Please choose an image file.";
        }

        if (file.Length > MaxBytes)
        {
            return "The image is too large. The maximum size is 5 MB.";
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return "Only JPG, PNG or WebP images are allowed.";
        }

        return null;
    }
}
