namespace ThreeDEngine.Core.Vision;

// ADR-0047 items 1 and 2 (phase V3, TASK-0155): the outer contour of the largest region of an image, as
// a closed polygon in pixels, which the desktop proposes and the person accepts as a sketch. The grey
// value, the threshold and the trace use integers, and the simplification uses + - * / and sqrt only,
// so the proposal is the same on each platform. A vertex is at the centre of a pixel: x to the right
// and y down from the upper-left corner of the image, so pixel (i, j) has its centre at (i + 0.5, j + 0.5).
public static class ContourProposal
{
    // ADR-0047 item 2: a proposal has at most this many vertices, so an accept sends a bounded number of
    // commands.
    public const int MaximumVertices = 200;

    // Clockwise in the image, with y down, from the west: W, NW, N, NE, E, SE, S, SW.
    private static readonly (int Dx, int Dy)[] Around = [(-1, 0), (-1, -1), (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1)];

    // The polygon of the proposal, or null when the image has no two classes of grey or its largest
    // region has fewer than three vertices.
    public static IReadOnlyList<(double X, double Y)>? Propose(int width, int height, ReadOnlySpan<byte> rgba)
    {
        if (width < 1 || height < 1 || rgba.Length != (long)width * height * 4)
            throw new ArgumentException("The pixels do not fill the picture.", nameof(rgba));

        var grey = new byte[width * height];
        for (var i = 0; i < grey.Length; i++)
            grey[i] = (byte)(((299 * rgba[i * 4]) + (587 * rgba[(i * 4) + 1]) + (114 * rgba[(i * 4) + 2]) + 500) / 1000);

        if (Threshold(grey) is not { } threshold)
            return null;
        var foreground = Foreground(grey, width, height, threshold);
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

        return polygon.Count < 3 ? null : polygon.Select(point => (point.X + 0.5, point.Y + 0.5)).ToList();
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
