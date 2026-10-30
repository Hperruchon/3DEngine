using Engine.Contracts.Sketch;
using static Engine.Core.Sketch.SketchModel;

namespace Engine.Core.Sketch;

// ADR-0035: the solver of the constraints of a sketch. A Newton iteration whose step is
// the step of minimum norm: each step solves (J·Jᵀ)·λ = r by Gaussian elimination with
// partial pivoting and subtracts Jᵀ·λ from the positions. The code uses + - * / and sqrt
// only, in one fixed order, because the solved record is a result of a command of the log
// and a replay must give the same bits on each platform (CLAUDE.md, determinism rules).
public static class SketchSolver
{
    // ADR-0035 item 5: the limit of the absolute value of each equation, in the length unit
    // of the Document.
    public const double Tolerance = 1e-9;

    public const int MaximumSteps = 50;

    // A pivot whose absolute value is at most this ratio of the largest diagonal entry is
    // zero, and its row is redundant.
    public const double PivotRatio = 1e-12;

    // ADR-0035 items 2, 6 and 7: the record with solved positions, or the record as it
    // is when no solution exists. A held point first stays at its position; when that
    // gives no solution, the solver tries again with no hold.
    public static SketchRecord Solve(SketchRecord sketch, Guid? held = null) => SolveWithHold(sketch, held).Record;

    // The same record, and true when the solver dropped the hold: the solve with the held
    // point did not converge and the solve with no hold did, so the point is not where the
    // command put it. Until TASK-0106 the command gave no sign of it (finding E87 of the
    // codebase review of 2026-10-29). When both solves fail, the record keeps the positions
    // of the command, so the point stays where the command put it.
    public static (SketchRecord Record, bool HoldDropped) SolveWithHold(SketchRecord sketch, Guid? held)
    {
        if (held is { } point && KindOf(sketch, point) == ItemKind.Point && point != sketch.Id)
        {
            if (Converge(sketch, point) is { } holding)
                return (holding, false);
            return Converge(sketch, null) is { } free ? (free, true) : (sketch, false);
        }

        return (Converge(sketch, null) ?? sketch, false);
    }

    // ADR-0035 item 8: true when each equation is within the tolerance, and the number of
    // free degrees at the positions of the record.
    public static (bool Solved, int Freedom) Status(SketchRecord sketch)
    {
        var system = new EquationSystem(sketch, null);
        var x = system.Start();
        system.Evaluate(x);
        var rank = system.Rows == 0 ? 0 : Eliminate(system.Product(), new double[system.Rows], out _);
        return (system.WithinTolerance() && system.CurvesHaveRadius(x), system.Variables - rank);
    }

    // The values and the derivatives of the equations at the positions of the record, for a
    // test that compares each derivative with a central difference (TASK-0110). The columns are
    // the x and the y of each point other than the origin, in the order of the record.
    internal static (double[] Values, double[,] Jacobian) Linearise(SketchRecord sketch)
    {
        var system = new EquationSystem(sketch, null);
        system.Evaluate(system.Start());
        return ((double[])system.Values.Clone(), (double[,])system.Jacobian.Clone());
    }

    // The solved record, or null when the iteration does not converge.
    private static SketchRecord? Converge(SketchRecord sketch, Guid? held)
    {
        var system = new EquationSystem(sketch, held);
        var x = system.Start();
        for (var step = 0; ; step++)
        {
            system.Evaluate(x);
            if (!system.IsFinite())
                return null;
            if (system.WithinTolerance())
                return !system.CurvesHaveRadius(x) ? null : step == 0 ? sketch : system.Record(x);
            if (step == MaximumSteps)
                return null;

            Eliminate(system.Product(), system.Residuals(), out var lambda);
            for (var row = 0; row < system.Rows; row++)
            {
                for (var column = 0; column < system.Variables; column++)
                    x[column] -= system.Jacobian[row, column] * lambda[row];
            }
        }
    }

    // Solves a·λ = b in row echelon form with partial pivoting, and gives the rank. A
    // column with no pivot above the limit gets λ = 0, so a redundant row adds nothing. The
    // search takes the first row of the largest absolute value, so a tie has one result.
    private static int Eliminate(double[,] a, double[] b, out double[] lambda)
    {
        var size = b.Length;
        var largest = 0.0;
        for (var i = 0; i < size; i++)
            largest = Math.Max(largest, Math.Abs(a[i, i]));
        var limit = PivotRatio * largest;

        var pivots = new List<(int Row, int Column)>();
        var row = 0;
        for (var column = 0; column < size && row < size; column++)
        {
            var best = row;
            for (var i = row + 1; i < size; i++)
            {
                if (Math.Abs(a[i, column]) > Math.Abs(a[best, column]))
                    best = i;
            }
            if (Math.Abs(a[best, column]) <= limit)
                continue;

            if (best != row)
            {
                for (var j = 0; j < size; j++)
                    (a[row, j], a[best, j]) = (a[best, j], a[row, j]);
                (b[row], b[best]) = (b[best], b[row]);
            }
            for (var i = row + 1; i < size; i++)
            {
                var factor = a[i, column] / a[row, column];
                if (factor == 0)
                    continue;
                for (var j = column; j < size; j++)
                    a[i, j] -= factor * a[row, j];
                b[i] -= factor * b[row];
            }
            pivots.Add((row, column));
            row++;
        }

        lambda = new double[size];
        for (var p = pivots.Count - 1; p >= 0; p--)
        {
            var (pivotRow, pivotColumn) = pivots[p];
            var sum = b[pivotRow];
            for (var j = pivotColumn + 1; j < size; j++)
                sum -= a[pivotRow, j] * lambda[j];
            lambda[pivotColumn] = sum / a[pivotRow, pivotColumn];
        }
        return pivots.Count;
    }

    // ADR-0035 items 3 and 4: the variables are the x and the y of each point other than
    // the origin, in the order of the record, then the radius of each circle (TASK-0117). The
    // equations come in the order of the constraints, then the equation of each arc, then the
    // two equations of a held point.
    private sealed class EquationSystem
    {
        private readonly SketchRecord _sketch;
        private readonly Guid? _held;
        private readonly Dictionary<Guid, int> _index = new();

        // The column of the radius of each circle (TASK-0117).
        private readonly Dictionary<Guid, int> _radius = new();
        private readonly List<SketchConstraint> _constraints;
        private readonly double[] _sides;
        private double[] _x = [];
        private int _row;

        public EquationSystem(SketchRecord sketch, Guid? held)
        {
            _sketch = sketch;
            _held = held;
            foreach (var point in sketch.Points)
            {
                if (point.Id != sketch.Id)
                    _index[point.Id] = 2 * _index.Count;
            }
            foreach (var circle in sketch.Circles)
                _radius[circle.Id] = (2 * _index.Count) + _radius.Count;
            Variables = (2 * _index.Count) + _radius.Count;
            _constraints = sketch.Constraints.ToList();

            var rows = 0;
            foreach (var constraint in _constraints)
                rows += SketchConstraints.HasTwoRows(constraint.Kind) ? 2 : 1;
            Rows = rows + sketch.Arcs.Count + (held is null ? 0 : 2);
            Jacobian = new double[Rows, Variables];
            Values = new double[Rows];

            // ADR-0035 item 4: the side of a point to a line, from the positions at the start
            // of the solve; +1 when the point is on the line.
            _sides = new double[_constraints.Count];
            for (var c = 0; c < _constraints.Count; c++)
            {
                var constraint = _constraints[c];
                _sides[c] = 1;
                if (constraint.Kind == SketchConstraints.Distance && constraint.Items.Count == 2
                    && KindOf(sketch, constraint.Items[1]) == ItemKind.Line)
                {
                    var line = Line(sketch, constraint.Items[1]);
                    var (p, a, b) = (Point(sketch, constraint.Items[0]), Point(sketch, line.Start), Point(sketch, line.End));
                    if (((b.X - a.X) * (p.Y - a.Y)) - ((b.Y - a.Y) * (p.X - a.X)) < 0)
                        _sides[c] = -1;
                }

                // TASK-0117: the side of the centre to the line of a tangent, and for an inner
                // tangent of two curves the sign of r₁ − r₂, from the positions at the start.
                if (constraint.Kind == SketchConstraints.Tangent && KindOf(sketch, constraint.Items[0]) == ItemKind.Line)
                {
                    var line = Line(sketch, constraint.Items[0]);
                    var (p, a, b) = (Point(sketch, SketchConstraints.Centre(sketch, constraint.Items[1])), Point(sketch, line.Start), Point(sketch, line.End));
                    if (((b.X - a.X) * (p.Y - a.Y)) - ((b.Y - a.Y) * (p.X - a.X)) < 0)
                        _sides[c] = -1;
                }
                else if (constraint.Kind == SketchConstraints.Tangent && constraint.Values[0] < 0
                    && SketchConstraints.CurveRadius(sketch, constraint.Items[0]) < SketchConstraints.CurveRadius(sketch, constraint.Items[1]))
                {
                    _sides[c] = -1;
                }
            }
        }

        public int Variables { get; }

        public int Rows { get; }

        public double[,] Jacobian { get; }

        public double[] Values { get; }

        public double[] Start()
        {
            var x = new double[Variables];
            foreach (var point in _sketch.Points)
            {
                if (_index.TryGetValue(point.Id, out var i))
                    (x[i], x[i + 1]) = (point.X, point.Y);
            }
            foreach (var circle in _sketch.Circles)
                x[_radius[circle.Id]] = circle.Radius;
            return x;
        }

        public SketchRecord Record(double[] x) => _sketch with
        {
            Points = _sketch.Points
                .Select(point => _index.TryGetValue(point.Id, out var i) ? point with { X = x[i], Y = x[i + 1] } : point)
                // Finding E91: a read-only list, so a client that casts it cannot change the Document.
                .ToList()
                .AsReadOnly(),
            Circles = _sketch.Circles.Select(circle => circle with { Radius = x[_radius[circle.Id]] }).ToList().AsReadOnly(),
        };

        // Finding E97 of the codebase review of 2026-10-30 (b) (TASK-0121): true when each circle
        // has a radius greater than 0 and each arc has its start and its end away from its centre.
        // The equations also hold for a negative radius and for a radius of 0, and until TASK-0121
        // a solve gave them and the sketch showed as solved.
        //
        // Finding E102 of the codebase review of 2026-10-31 (TASK-0130): the iteration stops when each
        // equation is within Tolerance, so a radius of 5.6e-17 from a coincident centre and a tangent
        // passed the test "greater than 0". A radius, and the distance of the start and of the end of
        // an arc from its centre, must be greater than Tolerance.
        public bool CurvesHaveRadius(double[] x)
        {
            foreach (var circle in _sketch.Circles)
            {
                if (!(x[_radius[circle.Id]] > Tolerance))
                    return false;
            }
            foreach (var arc in _sketch.Arcs)
            {
                if (!(Distance(x, arc.Start, arc.Center) > Tolerance) || !(Distance(x, arc.End, arc.Center) > Tolerance))
                    return false;
            }
            return true;
        }

        private double Distance(double[] x, Guid a, Guid b)
        {
            var (p, q) = (Position(x, a), Position(x, b));
            return Math.Sqrt(((p.X - q.X) * (p.X - q.X)) + ((p.Y - q.Y) * (p.Y - q.Y)));
        }

        // The position of a point in x, or in the record for a point with no variable: the origin.
        private (double X, double Y) Position(double[] x, Guid point)
        {
            if (_index.TryGetValue(point, out var i))
                return (x[i], x[i + 1]);
            var fixedPoint = _sketch.Points.First(p => p.Id == point);
            return (fixedPoint.X, fixedPoint.Y);
        }

        public bool IsFinite()
        {
            for (var row = 0; row < Rows; row++)
            {
                if (!double.IsFinite(Values[row]))
                    return false;
                for (var column = 0; column < Variables; column++)
                {
                    if (!double.IsFinite(Jacobian[row, column]))
                        return false;
                }
            }
            return true;
        }

        public bool WithinTolerance()
        {
            for (var row = 0; row < Rows; row++)
            {
                if (!(Math.Abs(Values[row]) < Tolerance))
                    return false;
            }
            return true;
        }

        public double[] Residuals() => (double[])Values.Clone();

        // J·Jᵀ, with the sum of each entry in the order of the columns.
        public double[,] Product()
        {
            var product = new double[Rows, Rows];
            for (var i = 0; i < Rows; i++)
            {
                for (var j = 0; j < Rows; j++)
                {
                    var sum = 0.0;
                    for (var k = 0; k < Variables; k++)
                        sum += Jacobian[i, k] * Jacobian[j, k];
                    product[i, j] = sum;
                }
            }
            return product;
        }

        // The value and the derivatives of each equation at the positions x.
        public void Evaluate(double[] x)
        {
            _x = x;
            _row = 0;
            Array.Clear(Jacobian);
            for (var c = 0; c < _constraints.Count; c++)
                Add(_constraints[c], _sides[c]);

            // TASK-0117: the two radii of each arc are equal, |s − c| − |e − c| = 0.
            foreach (var arc in _sketch.Arcs)
            {
                var (from, start) = Spoke(arc.Center, arc.Start);
                var (to, end) = Spoke(arc.Center, arc.End);
                Equation(from - to, (arc.Start, start.X, start.Y), (arc.End, -end.X, -end.Y), (arc.Center, end.X - start.X, end.Y - start.Y));
            }

            if (_held is { } held)
            {
                var point = Point(_sketch, held);
                Equation(X(held) - point.X, (held, 1, 0));
                Equation(Y(held) - point.Y, (held, 0, 1));
            }
        }

        private void Add(SketchConstraint constraint, double side)
        {
            var items = constraint.Items;
            switch (constraint.Kind)
            {
                case SketchConstraints.Coincident:
                    Equation(X(items[0]) - X(items[1]), (items[0], 1, 0), (items[1], -1, 0));
                    Equation(Y(items[0]) - Y(items[1]), (items[0], 0, 1), (items[1], 0, -1));
                    break;
                case SketchConstraints.Horizontal:
                {
                    var line = Line(_sketch, items[0]);
                    Equation(Y(line.End) - Y(line.Start), (line.End, 0, 1), (line.Start, 0, -1));
                    break;
                }
                case SketchConstraints.Vertical:
                {
                    var line = Line(_sketch, items[0]);
                    Equation(X(line.End) - X(line.Start), (line.End, 1, 0), (line.Start, -1, 0));
                    break;
                }
                case SketchConstraints.Distance when items.Count == 1:
                {
                    var line = Line(_sketch, items[0]);
                    Separation(line.End, line.Start, constraint.Values[0]);
                    break;
                }
                case SketchConstraints.Distance when KindOf(_sketch, items[1]) == ItemKind.Point:
                    Separation(items[0], items[1], constraint.Values[0]);
                    break;
                case SketchConstraints.Distance:
                    ToLine(items[0], Line(_sketch, items[1]), side * constraint.Values[0]);
                    break;
                case SketchConstraints.Fixed:
                    Equation(X(items[0]) - constraint.Values[0], (items[0], 1, 0));
                    Equation(Y(items[0]) - constraint.Values[1], (items[0], 0, 1));
                    break;
                // ADR-0035, History of TASK-0110: the equations of K2c, each in length units.
                case SketchConstraints.PointOn when KindOf(_sketch, items[1]) == ItemKind.Line:
                    ToLine(items[0], Line(_sketch, items[1]), 0);
                    break;
                // TASK-0117: |p − c| − r of the circle or of the arc.
                case SketchConstraints.PointOn:
                {
                    var (distance, u) = Spoke(SketchConstraints.Centre(_sketch, items[1]), items[0]);
                    var terms = new List<(Guid, double, double)> { (items[0], u.X, u.Y), (SketchConstraints.Centre(_sketch, items[1]), -u.X, -u.Y) };
                    Equation(distance - RadiusOf(items[1], -1, terms), [.. terms]);
                    break;
                }
                case SketchConstraints.Equal when KindOf(_sketch, items[0]) != ItemKind.Line:
                {
                    var terms = new List<(Guid, double, double)>();
                    var value = RadiusOf(items[0], 1, terms) - RadiusOf(items[1], -1, terms);
                    Equation(value, [.. terms]);
                    break;
                }
                case SketchConstraints.Radius:
                {
                    var terms = new List<(Guid, double, double)>();
                    Equation(RadiusOf(items[0], 1, terms) - constraint.Values[0], [.. terms]);
                    break;
                }
                case SketchConstraints.Tangent when KindOf(_sketch, items[0]) == ItemKind.Line:
                    Tangent(Line(_sketch, items[0]), items[1], side);
                    break;
                case SketchConstraints.Tangent:
                {
                    // |c₁ − c₂| − (r₁ + r₂) outside, or |c₁ − c₂| − s·(r₁ − r₂) inside, with s the sign at the start.
                    var (c1, c2) = (SketchConstraints.Centre(_sketch, items[0]), SketchConstraints.Centre(_sketch, items[1]));
                    var (distance, u) = Spoke(c2, c1);
                    var terms = new List<(Guid, double, double)> { (c1, u.X, u.Y), (c2, -u.X, -u.Y) };
                    var outside = constraint.Values[0] > 0;
                    var r1 = RadiusOf(items[0], outside ? -1 : -side, terms);
                    var r2 = RadiusOf(items[1], outside ? -1 : side, terms);
                    Equation(outside ? distance - (r1 + r2) : distance - (side * (r1 - r2)), [.. terms]);
                    break;
                }
                case SketchConstraints.Midpoint:
                {
                    var line = Line(_sketch, items[1]);
                    Equation(X(items[0]) - ((X(line.Start) + X(line.End)) / 2), (items[0], 1, 0), (line.Start, -0.5, 0), (line.End, -0.5, 0));
                    Equation(Y(items[0]) - ((Y(line.Start) + Y(line.End)) / 2), (items[0], 0, 1), (line.Start, 0, -0.5), (line.End, 0, -0.5));
                    break;
                }
                case SketchConstraints.Parallel or SketchConstraints.Perpendicular or SketchConstraints.Equal or SketchConstraints.Angle:
                    TwoLines(constraint.Kind, Line(_sketch, items[0]), Line(_sketch, items[1]), constraint.Values);
                    break;
                case SketchConstraints.Symmetric:
                    Mirror(items[0], items[1], Line(_sketch, items[2]));
                    break;
                default:
                    throw new InvalidOperationException($"The constraint {constraint.Id} has the kind '{constraint.Kind}', which the solver does not give.");
            }
        }

        // The distance from the point q to the point p and the unit direction from q to p; at a
        // distance of zero the direction is (1, 0).
        private (double Distance, (double X, double Y) Unit) Spoke(Guid q, Guid p)
        {
            var (dx, dy) = (X(p) - X(q), Y(p) - Y(q));
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            return (distance, distance == 0 ? (1.0, 0.0) : (dx / distance, dy / distance));
        }

        // The radius of a circle, a variable, or of an arc, |s − c|, and its derivatives times the
        // weight added to the terms. A term of a circle names the circle, and Equation puts it in
        // the column of its radius.
        private double RadiusOf(Guid curve, double weight, List<(Guid, double, double)> terms)
        {
            if (_radius.TryGetValue(curve, out var column))
            {
                terms.Add((curve, weight, 0));
                return _x[column];
            }
            var arc = Arc(_sketch, curve);
            var (distance, u) = Spoke(arc.Center, arc.Start);
            terms.Add((arc.Start, weight * u.X, weight * u.Y));
            terms.Add((arc.Center, -weight * u.X, -weight * u.Y));
            return distance;
        }

        // tangent of a line a→b and a curve: cross(b − a, c − a) / |b − a| − s·r, with s the side of
        // the centre at the start; a line of length zero gives |c − a| − r.
        private void Tangent(SketchLine line, Guid curve, double side)
        {
            var centre = SketchConstraints.Centre(_sketch, curve);
            var (a, b) = (line.Start, line.End);
            var (dx, dy) = (X(b) - X(a), Y(b) - Y(a));
            var (wx, wy) = (X(centre) - X(a), Y(centre) - Y(a));
            var length = Math.Sqrt((dx * dx) + (dy * dy));
            if (length == 0)
            {
                var (distance, u) = Spoke(a, centre);
                var zero = new List<(Guid, double, double)> { (centre, u.X, u.Y), (a, -u.X, -u.Y) };
                Equation(distance - RadiusOf(curve, -1, zero), [.. zero]);
                return;
            }

            var value = ((dx * wy) - (dy * wx)) / length;
            var (lx, ly) = (dx / length, dy / length);
            var ratio = value / length;
            var terms = new List<(Guid, double, double)>
            {
                (centre, -dy / length, dx / length),
                (a, ((dy - wy) / length) + (ratio * lx), ((wx - dx) / length) + (ratio * ly)),
                (b, (wy / length) - (ratio * lx), (-wx / length) - (ratio * ly)),
            };
            Equation(value - (side * RadiusOf(curve, -side, terms)), [.. terms]);
        }

        // |p − q| − length. At a length of zero the derivative takes the direction (1, 0).
        private void Separation(Guid p, Guid q, double length)
        {
            var (dx, dy) = (X(p) - X(q), Y(p) - Y(q));
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            var (ux, uy) = distance == 0 ? (1.0, 0.0) : (dx / distance, dy / distance);
            Equation(distance - length, (p, ux, uy), (q, -ux, -uy));
        }

        // cross(b − a, p − a) / |b − a| − signed length. A line of length zero is a point, so
        // the equation is then the distance to its start.
        private void ToLine(Guid p, SketchLine line, double signedLength)
        {
            var (a, b) = (line.Start, line.End);
            var (dx, dy) = (X(b) - X(a), Y(b) - Y(a));
            var (wx, wy) = (X(p) - X(a), Y(p) - Y(a));
            var length = Math.Sqrt((dx * dx) + (dy * dy));
            if (length == 0)
            {
                Separation(p, a, Math.Abs(signedLength));
                return;
            }

            var cross = (dx * wy) - (dy * wx);
            var value = cross / length;
            // The derivative of cross / length: d(cross) / length − cross · d(length) / length².
            var (lx, ly) = (dx / length, dy / length);
            var ratio = value / length;
            Equation(
                value - signedLength,
                (p, -dy / length, dx / length),
                (a, ((dy - wy) / length) + (ratio * lx), ((wx - dx) / length) + (ratio * ly)),
                (b, (wy / length) - (ratio * lx), (-wx / length) - (ratio * ly)));
        }

        // parallel: cross(d₁, d₂) / (|d₁|·|d₂|), the sine of the angle between the lines.
        // perpendicular: dot(d₁, d₂) / (|d₁|·|d₂|), its cosine. equal: |d₁| − |d₂|, in length units.
        // angle: cross(w, u₂) / (1 + dot(w, u₂)) with w = R(θ)·u₁, which is zero only when d₂ has
        // the direction of d₁ turned by θ, so the solve keeps the branch of the inequality of
        // ADR-0033 item 3. Two rows u₂ − w have rank one away from the solution, and the solver
        // then drops one of them and does not converge. For a direction of length zero the
        // quotient takes 1 for its length and the unit direction (1, 0), as Separation does.
        private void TwoLines(string kind, SketchLine first, SketchLine second, IReadOnlyList<double> values)
        {
            var (d1x, d1y) = (X(first.End) - X(first.Start), Y(first.End) - Y(first.Start));
            var (d2x, d2y) = (X(second.End) - X(second.Start), Y(second.End) - Y(second.Start));
            var l1 = Math.Sqrt((d1x * d1x) + (d1y * d1y));
            var l2 = Math.Sqrt((d2x * d2x) + (d2y * d2y));
            var (n1, n2) = (l1 == 0 ? 1 : l1, l2 == 0 ? 1 : l2);
            var (u1x, u1y) = l1 == 0 ? (1.0, 0.0) : (d1x / l1, d1y / l1);
            var (u2x, u2y) = l2 == 0 ? (1.0, 0.0) : (d2x / l2, d2y / l2);

            // Finding of the codebase review of 2026-10-30: an equation of a direction that grows
            // with |d₂| has the root d₂ = 0, and the step of minimum norm from an exact parallel
            // start went to it, so a line shrank to one point. Each equation of a direction is
            // now a function of the two unit directions only, so no length changes its value.
            switch (kind)
            {
                case SketchConstraints.Parallel:
                {
                    var value = ((d1x * d2y) - (d1y * d2x)) / (n1 * n2);
                    var byFirst = Quotient(d2y / (n1 * n2), -d2x / (n1 * n2), value, d1x, d1y, l1);
                    var bySecond = Quotient(-d1y / (n1 * n2), d1x / (n1 * n2), value, d2x, d2y, l2);
                    Directions(first, second, value, byFirst, bySecond);
                    break;
                }
                case SketchConstraints.Perpendicular:
                {
                    var value = ((d1x * d2x) + (d1y * d2y)) / (n1 * n2);
                    var byFirst = Quotient(d2x / (n1 * n2), d2y / (n1 * n2), value, d1x, d1y, l1);
                    var bySecond = Quotient(d1x / (n1 * n2), d1y / (n1 * n2), value, d2x, d2y, l2);
                    Directions(first, second, value, byFirst, bySecond);
                    break;
                }
                case SketchConstraints.Equal:
                    Directions(first, second, l1 - l2, (u1x, u1y), (-u2x, -u2y));
                    break;
                default:
                {
                    var (cos, sin) = (values[0], values[1]);
                    var (wx, wy) = ((cos * u1x) - (sin * u1y), (sin * u1x) + (cos * u1y));
                    // g = cross(w, u₂) / (1 + dot(w, u₂)), the tangent of half the angle φ from w
                    // to u₂. Its step of Gauss and Newton is Δφ = −sin φ, so a solve goes to φ = 0
                    // from each start but the opposite one, where g is not finite and the solve
                    // stops.
                    var c = (wx * u2y) - (wy * u2x);
                    var k = 1 + (wx * u2x) + (wy * u2y);
                    var value = c / k;
                    var (gu2x, gu2y) = ((-wy - (value * wx)) / k, (wx - (value * wy)) / k);
                    var (gwx, gwy) = ((u2y - (value * u2x)) / k, (-u2x - (value * u2y)) / k);
                    // By u₁ through w = R(θ)·u₁, then by d₁ and d₂ through P and Q, the
                    // derivatives (I − u·uᵀ) / |d| of the unit directions; zero for a length of zero.
                    var (gu1x, gu1y) = ((cos * gwx) + (sin * gwy), (-sin * gwx) + (cos * gwy));
                    Directions(first, second, value, Unit(gu1x, gu1y, u1x, u1y, l1), Unit(gu2x, gu2y, u2x, u2y, l2));
                    break;
                }
            }
        }

        // symmetric p, q, a→b: the midpoint m of p and q is on the line, cross(d, m − a) / |d| = 0,
        // and q − p is normal to it, dot(q − p, d) / |d| = 0.
        private void Mirror(Guid p, Guid q, SketchLine line)
        {
            var (a, b) = (line.Start, line.End);
            var (dx, dy) = (X(b) - X(a), Y(b) - Y(a));
            var length = Math.Sqrt((dx * dx) + (dy * dy));
            var n = length == 0 ? 1 : length;

            var (wx, wy) = (((X(p) + X(q)) / 2) - X(a), ((Y(p) + Y(q)) / 2) - Y(a));
            var across = ((dx * wy) - (dy * wx)) / n;
            var (cx, cy) = Quotient(wy / n, -wx / n, across, dx, dy, length);
            var (hx, hy) = (-dy / (2 * n), dx / (2 * n));
            Equation(across, (p, hx, hy), (q, hx, hy), (a, (dy / n) - cx, (-dx / n) - cy), (b, cx, cy));

            var (sx, sy) = (X(q) - X(p), Y(q) - Y(p));
            var along = ((sx * dx) + (sy * dy)) / n;
            var (ex, ey) = Quotient(sx / n, sy / n, along, dx, dy, length);
            Equation(along, (p, -dx / n, -dy / n), (q, dx / n, dy / n), (a, -ex, -ey), (b, ex, ey));
        }

        // The derivative by d of f(d) / |d|, from the derivative g / |d| of its numerator: g / |d|
        // − value · d / |d|². For a direction of length zero the quotient takes 1, so the
        // derivative is g.
        private static (double X, double Y) Quotient(double gx, double gy, double value, double dx, double dy, double length)
            => length == 0 ? (gx, gy) : (gx - (value * dx / (length * length)), gy - (value * dy / (length * length)));

        // The derivative by d of a function of the unit direction u = d / |d|, from its derivative
        // g by u: (I − u·uᵀ)·g / |d|, or zero for a direction of length zero.
        private static (double X, double Y) Unit(double gx, double gy, double ux, double uy, double length)
        {
            if (length == 0)
                return (0, 0);
            var along = (ux * gx) + (uy * gy);
            return ((gx - (along * ux)) / length, (gy - (along * uy)) / length);
        }

        // One row of two directions d₁ = b₁ − a₁ and d₂ = b₂ − a₂, from its derivatives by d₁ and d₂.
        private void Directions(SketchLine first, SketchLine second, double value, (double X, double Y) byFirst, (double X, double Y) bySecond)
            => Equation(
                value,
                (first.End, byFirst.X, byFirst.Y),
                (first.Start, -byFirst.X, -byFirst.Y),
                (second.End, bySecond.X, bySecond.Y),
                (second.Start, -bySecond.X, -bySecond.Y));

        // One row: its value, and the derivatives of x and y of each point. A term of the
        // origin is a constant and has no column; two terms of one point add.
        private void Equation(double value, params (Guid Point, double Dx, double Dy)[] terms)
        {
            Values[_row] = value;
            foreach (var (point, dx, dy) in terms)
            {
                if (_index.TryGetValue(point, out var i))
                {
                    Jacobian[_row, i] += dx;
                    Jacobian[_row, i + 1] += dy;
                }
                else if (_radius.TryGetValue(point, out var column))
                {
                    // A term of a circle: dx is the derivative by its radius.
                    Jacobian[_row, column] += dx;
                }
            }
            _row++;
        }

        private double X(Guid point) => _index.TryGetValue(point, out var i) ? _x[i] : Point(_sketch, point).X;

        private double Y(Guid point) => _index.TryGetValue(point, out var i) ? _x[i + 1] : Point(_sketch, point).Y;
    }
}
