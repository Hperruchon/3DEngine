namespace Engine.Contracts.Images;

// ADR-0045 items 1 and 2: the record of one live image on a datum. A record never changes:
// an edit gives a new record with the same identifier. Asset is the name of the bytes in the
// store of the account (ADR-0044 item 1). The origin is the lower-left corner of the image in
// the frame of the datum, in the length unit of the Document; the image covers Width × Scale
// along the axis U and Height × Scale along the axis V, and row 0 of the image is at the top.
// Scale is in millimetres for each pixel.
public sealed record ImageRecord(
    Guid Id,
    string Asset,
    Guid DatumId,
    double OriginU,
    double OriginV,
    int Width,
    int Height,
    double Scale);
