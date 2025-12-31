namespace BrickDex.Web.Extensions;

public static class ImageUrlExtensions {
    /// <summary>
    /// Converts a Rebrickable CDN image URL to a thumbnail URL with the specified dimensions.
    /// </summary>
    /// <param name="imageUrl">The original image URL from Rebrickable</param>
    /// <param name="width">Desired width in pixels (max 1000)</param>
    /// <param name="height">Desired height in pixels (max 1000)</param>
    /// <returns>The thumbnail URL, or the original URL if conversion isn't possible</returns>
    public static string ToThumbnail(this string? imageUrl, int width, int height) {
        if(string.IsNullOrEmpty(imageUrl)) {
            return string.Empty;
        }

        // Only process Rebrickable CDN URLs
        if(!imageUrl.Contains("cdn.rebrickable.com/media/", StringComparison.Ordinal)) {
            return imageUrl;
        }

        // Already a thumbnail URL
        if(imageUrl.Contains("/thumbs/", StringComparison.Ordinal)) {
            return imageUrl;
        }

        // Convert: /media/sets/xxx.jpg -> /media/thumbs/sets/xxx.jpg/WxHp.jpg
        var thumbnailUrl = imageUrl.Replace("/media/", "/media/thumbs/", StringComparison.Ordinal);
        return $"{thumbnailUrl}/{width}x{height}p.jpg";
    }
}
