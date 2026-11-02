namespace ThreeDEngine.Core.Vision;

// ADR-0047 items 1 and 2 (phase V3, TASK-0155): the outer contour of the largest region of an image, as
// a closed polygon in pixels, which the desktop proposes and the person accepts as a sketch. The grey
// value, the threshold, the opening, the trace and the check of a simple polygon use integers, and the
// simplification uses + - * / and sqrt only,
// so the proposal is the same on each platform. A vertex is at the centre of a pixel: x to the right
// and y down from the upper-left corner of the image, so pixel (i, j) has its centre at (i + 0.5, j + 0.5).
public static class ContourProposal
{
    // ADR-0047 item 2: a proposal has at most this many vertices, so an accept sends a bounded number of
    // commands.
    public const int MaximumVertices = 200;

    // Clockwise in the image, with y down, from the west: W, NW, N, NE, E, SE, S, SW.
    private static readonly (int Dx, int Dy)[] Around = [(-1, 0), (-1, -1), (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1)];

    // The reason of a proposal that the image does not give.
    public const string NoRegion = "The image has no region that gives a contour.";

    // The reason of a polygon that is not simple, which the engine refuses as a profile.
    public const string NotSimple = "The contour of the region crosses or touches itself, so its sketch does not extrude.";

    // The polygon of the proposal, or null with the reason: the image has no two classes of grey, its
    // largest region has fewer than three vertices, or the polygon is not simple.
    public static IReadOnlyList<(double X, double Y)>? Propose(int width, int height, ReadOnlySpan<byte> rgba, out string? reason)
    {
        reason = NoRegion;
        if (width < 1 || height < 1 || rgba.Length != (long)width * height * 4)
            throw new ArgumentException("The pixels do not fill the picture.", nameof(rgba));

        var grey = new byte[width * height];
        for (var i = 0; i < grey.Length; i++)
            grey[i] = (byte)(((299 * rgba[i * 4]) + (587 * rgba[(i * 4) + 1]) + (114 * rgba[(i * 4) + 2]) + 500) / 1000);

        if (Threshold(grey) is not { } threshold)
            return null;
        var foreground = Open(Foreground(grey, width, height, threshold), width, height);
        var region = Largest(foreground, width, height);
        if (region is null)
            return null;

        var boundary = Trace(region, width, height);
        if (boundary.Count < 3)
            return null;

        var tolerance = Math.Max(1.0, 0.005 * boundary.Count);
        var polygon = Simplify(boundary, tolerance);
        while (polygon.Count > MaximumVertices)
        {
            tolerance *= 1.5;
            polygon = Simplify(boundary, tolerance);
        }

        if (polygon.Count < 3)
            return null;

        // Finding V69 of the codebase review of 2026-11-01 (TASK-0162): the engine refuses a profile that
        // crosses or touches itself, so the proposal refuses it first, with its reason.
        if (!Simple(polygon))
        {
            reason = NotSimple;
            return null;
        }

        reason = null;
        return polygon.Select(point => (point.X + 0.5, point.Y + 0.5)).ToList();
    }

    // The threshold of Otsu: the grey value that separates the two classes with the largest variance
    // between them. A pixel at the threshold or below it is dark. Null when one class is empty for
    // each threshold.
    private static int? Threshold(byte[] grey)
    {
        var histogram = new long[256];
        foreach (var value in grey)
            histogram[value]++;
        long total = grey.Length, sum = 0;
        for (var value = 0; value < 256; value++)
            sum += value * histogram[value];

        long dark = 0, darkSum = 0;
        double best = -1;
        int? threshold = null;
        for (var value = 0; value < 255; value++)
        {
            dark += histogram[value];
            darkSum += value * histogram[value];
            var light = total - dark;
            if (dark == 0 || light == 0)
                continue;
            var difference = ((double)darkSum * light) - ((double)(sum - darkSum) * dark);
            var between = difference / dark * difference / light;
            if (between > best)
                (best, threshold) = (between, value);
        }

        return threshold;
    }

    // The class of the region: the class that holds fewer pixels of the border, because the background
    // of a photo of a part touches the border.
    private static bool[] Foreground(byte[] grey, int width, int height, int threshold)
    {
        long border = 0, light = 0;
        for (var x = 0; x < width; x++)
        {
            foreach (var y in height == 1 ? [0] : new[] { 0, height - 1 })
            {
                border++;
                if (grey[(y * width) + x] > threshold)
                    light++;
            }
        }

        for (var y = 1; y < height - 1; y++)
        {
            foreach (var x in width == 1 ? [0] : new[] { 0, width - 1 })
            {
                border++;
                if (grey[(y * width) + x] > threshold)
                    light++;
            }
        }

        var lightIsForeground = 2 * light <= border;
        var mask = new bool[grey.Length];
        for (var i = 0; i < grey.Length; i++)
            mask[i] = grey[i] > threshold == lightIsForeground;
        return mask;
    }

    // ADR-0047 item 2: the opening with a square of 3 × 3. A pixel stays when a square of nine pixels of
    // the mask holds it, so a part of the mask that is one or two pixels wide goes, and the corner of a
    // rectangle stays. Finding V69 of the codebase review of 2026-11-01 (TASK-0162): the trace went out
    // and back along such a part, and the polygon touched itself.
    private static bool[] Open(bool[] mask, int width, int height)
    {
        bool In(bool[] cells, int x, int y) => x >= 0 && y >= 0 && x < width && y < height && cells[(y * width) + x];

        // True when each pixel, or when one pixel, of the square of 3 × 3 about (x, y) is in the cells.
        bool Square(bool[] cells, int x, int y, bool all)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (In(cells, x + dx, y + dy) != all)
                        return !all;
                }
            }

            return all;
        }

        var centre = new bool[mask.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                centre[(y * width) + x] = Square(mask, x, y, all: true);
        }

        var opened = new bool[mask.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                opened[(y * width) + x] = Square(centre, x, y, all: false);
        }

        return opened;
    }

    // The largest region of the mask whose pixels touch by a side, or null when the mask is empty. Of two
    // regions of one size, the first in the order of the rows.
    private static bool[]? Largest(bool[] mask, int width, int height)
    {
        var label = new int[mask.Length];
        var (bestLabel, bestSize, next) = (0, 0, 0);
        var stack = new Stack<int>();
        for (var start = 0; start < mask.Length; start++)
        {
            if (!mask[start] || label[start] != 0)
                continue;
            next++;
            var size = 0;
            label[start] = next;
            stack.Push(start);
            while (stack.Count > 0)
            {
                var at = stack.Pop();
                size++;
                var (x, y) = (at % width, at / width);
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var (nx, ny) = (x + dx, y + dy);
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    var neighbour = (ny * width) + nx;
                    if (mask[neighbour] && label[neighbour] == 0)
                    {
                        label[neighbour] = next;
                        stack.Push(neighbour);
                    }
                }
            }

            if (size > bestSize)
                (bestLabel, bestSize) = (next, size);
        }

        return bestLabel == 0 ? null : label.Select(value => value == bestLabel).ToArray();
    }

    // The pixels of the outer boundary of the region, in order, clockwise in the image, by the trace of
    // the neighbours of Moore from the first pixel of the region in the order of the rows.
    private static List<(int X, int Y)> Trace(bool[] region, int width, int height)
    {
        bool In(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && region[(y * width) + x];

        var first = Array.IndexOf(region, true);
        var start = (X: first % width, Y: first / width);
        var boundary = new List<(int X, int Y)> { start };

        // The pixel to the west of the first pixel is outside the region, so the search starts there.
        // The stop of Jacob: the trace ends when it is on the first pixel and would take its first step
        // again.
        var (current, from) = (start, 0);
        (int X, int Y)? firstStep = null;
        for (var steps = 0; steps < (4 * region.Length) + 8; steps++)
        {
            var direction = -1;
            for (var turn = 1; turn <= 8 && direction < 0; turn++)
            {
                var (dx, dy) = Around[(from + turn) % 8];
                if (In(current.X + dx, current.Y + dy))
                    direction = (from + turn) % 8;
            }

            // A region of one pixel.
            if (direction < 0)
                return boundary;

            var next = (X: current.X + Around[direction].Dx, Y: current.Y + Around[direction].Dy);
            if (current == start && firstStep == next)
                break;
            firstStep ??= next;

            // The neighbour that the search saw last before the next pixel is outside the region, and the
            // search from the next pixel starts after it. Two cells of the ring next to each other differ
            // by one step, so the difference is a direction.
            var (bx, by) = (current.X + Around[(direction + 7) % 8].Dx, current.Y + Around[(direction + 7) % 8].Dy);
            from = Array.IndexOf(Around, (bx - next.X, by - next.Y));
            current = next;
            if (current != start)
                boundary.Add(current);
        }

        return boundary;
    }

    // ADR-0047 item 2: the rule of the engine for a profile (Engine.Core/Sketch/SketchProfile.cs). No side
    // has the length zero, no two sides that share no vertex cross or touch, no two sides that share a
    // vertex go back along one line, and the area is not zero. The vertices are pixels, so each product
    // is an exact integer.
    private static bool Simple(List<(int X, int Y)> polygon)
    {
        var n = polygon.Count;
        long twiceArea = 0;
        for (var i = 0; i < n; i++)
        {
            var (a, b, c) = (polygon[i], polygon[(i + 1) % n], polygon[(i + 2) % n]);
            if (a == b)
                return false;
            twiceArea += ((long)a.X * b.Y) - ((long)b.X * a.Y);
            if (Side(a, b, c) == 0 && (((long)(b.X - a.X) * (c.X - b.X)) + ((long)(b.Y - a.Y) * (c.Y - b.Y))) < 0)
                return false;
            for (var j = i + 2; j < n; j++)
            {
                if (i == 0 && j == n - 1)
                    continue;
                if (Meet(a, b, polygon[j], polygon[(j + 1) % n]))
                    return false;
            }
        }

        return twiceArea != 0;
    }

    // True when the segments pq and rs cross or touch.
    private static bool Meet((int X, int Y) p, (int X, int Y) q, (int X, int Y) r, (int X, int Y) s)
    {
        var (d1, d2, d3, d4) = (Side(r, s, p), Side(r, s, q), Side(p, q, r), Side(p, q, s));
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;
        return (d1 == 0 && Within(r, s, p)) || (d2 == 0 && Within(r, s, q)) || (d3 == 0 && Within(p, q, r)) || (d4 == 0 && Within(p, q, s));
    }

    // The cross product of (b − a) and (c − a): positive when c is left of the line ab.
    private static long Side((int X, int Y) a, (int X, int Y) b, (int X, int Y) c)
        => ((long)(b.X - a.X) * (c.Y - a.Y)) - ((long)(b.Y - a.Y) * (c.X - a.X));

    // True when c, on the line ab, is inside the box of the segment ab.
    private static bool Within((int X, int Y) a, (int X, int Y) b, (int X, int Y) c)
        => Math.Min(a.X, b.X) <= c.X && c.X <= Math.Max(a.X, b.X) && Math.Min(a.Y, b.Y) <= c.Y && c.Y <= Math.Max(a.Y, b.Y);

    // The closed polygon of Douglas and Peucker: the first point and the point farthest from it split the
    // boundary into two chains, and each chain keeps the points farther than the tolerance from its chord.
    private static List<(int X, int Y)> Simplify(List<(int X, int Y)> boundary, double tolerance)
    {
        var far = 0;
        double farthest = -1;
        for (var i = 1; i < boundary.Count; i++)
        {
            var (dx, dy) = ((double)(boundary[i].X - boundary[0].X), (double)(boundary[i].Y - boundary[0].Y));
            if ((dx * dx) + (dy * dy) > farthest)
                (farthest, far) = ((dx * dx) + (dy * dy), i);
        }

        var keep = new bool[boundary.Count];
        keep[0] = keep[far] = true;
        Chain(boundary, 0, far, tolerance, keep);
        Chain(boundary, far, boundary.Count, tolerance, keep);
        return boundary.Where((_, i) => keep[i]).ToList();
    }

    // The chain from index a to index b, where b equal to the count is the first point again.
    private static void Chain(List<(int X, int Y)> boundary, int a, int b, double tolerance, bool[] keep)
    {
        var pending = new Stack<(int A, int B)>();
        pending.Push((a, b));
        while (pending.Count > 0)
        {
            var (from, to) = pending.Pop();
            var (ax, ay) = boundary[from];
            var (bx, by) = boundary[to % boundary.Count];
            var (cx, cy) = ((double)(bx - ax), (double)(by - ay));
            var chord = Math.Sqrt((cx * cx) + (cy * cy));
            var (index, worst) = (-1, tolerance);
            for (var i = from + 1; i < to; i++)
            {
                var (px, py) = ((double)(boundary[i].X - ax), (double)(boundary[i].Y - ay));
                var distance = chord > 0 ? Math.Abs((cx * py) - (cy * px)) / chord : Math.Sqrt((px * px) + (py * py));
                if (distance > worst)
                    (index, worst) = (i, distance);
            }

            if (index < 0)
                continue;
            keep[index] = true;
            pending.Push((from, index));
            pending.Push((index, to));
        }
    }
}
