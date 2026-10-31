using Engine.Contracts.Images;
using Engine.Contracts.Sketch;
using ThreeDEngine.Core.Rendering;

namespace ThreeDEngine.Desktop;

// ADR-0045 item 2 and ADR-0046 item 2: the quad of an image on its datum, and the pixel of the
// image at a point of the datum. The three datums are origin planes, so the origin of each frame
// is the origin of the scene. The texture of a quad is the texture of its asset, so two images of
// one asset share one texture.
internal static class ImagePlacement
{
    public static ImageQuad? Quad(ImageRecord image)
    {
        if (Datums.Find(image.DatumId) is not { } datum)
            return null;
        var (u, v) = (datum.U, datum.V);
        var (width, height) = (image.Width * image.Scale, image.Height * image.Scale);
        return new ImageQuad(
            image.Asset,
            (image.OriginU * u.X + image.OriginV * v.X, image.OriginU * u.Y + image.OriginV * v.Y, image.OriginU * u.Z + image.OriginV * v.Z),
            (width * u.X, width * u.Y, width * u.Z),
            (height * v.X, height * v.Y, height * v.Z));
    }

    // The pixel (x, y) at the point (U, V) of the datum: x to the right and y down from the
    // upper-left corner, so row 0 is the top row of the image.
    public static (double X, double Y) Pixel(ImageRecord image, (double U, double V) at)
        => ((at.U - image.OriginU) / image.Scale, image.Height - (at.V - image.OriginV) / image.Scale);
}
