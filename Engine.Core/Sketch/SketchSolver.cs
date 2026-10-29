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
    public static SketchRecord Solve(SketchRecord sketch, Guid? held = null)
    {
        if (held is { } point && KindOf(sketch, point) == ItemKind.Point && point != sketch.Id
            && Converge(sketch, point) is { } holding)
        {
            return holding;
        }

        return Converge(sketch, null) ?? sketch;
    }

    // ADR-0035 item 8: true when each equation is within the tolerance, and the number of
    // free degrees at the positions of the record.
    public static (bool Solved, int Freedom) Status(SketchRecord sketch)
    {
        var system = new EquationSystem(sketch, null);
        var x = system.Start();
        system.Evaluate(x);
        var rank = system.Rows == 0 ? 0 : Eliminate(system.Product(), new double[system.Rows], out _);
        return (system.WithinTolerance(), system.Variables - rank);
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
                return step == 0 ? sketch : system.Record(x);
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
    // the origin, in the order of the record. The equations come in the order of the
    // constraints, then the two equations of a held point.
    private sealed class EquationSystem
    {
        private readonly SketchRecord _sketch;
        private readonly Guid? _held;
        private readonly Dictionary<Guid, int> _index = new();
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
            Variables = 2 * _index.Count;
            _constraints = sketch.Constraints.ToList();

            var rows = 0;
            foreach (var constraint in _constraints)
                rows += constraint.Kind is SketchConstraints.Coincident or SketchConstraints.Fixed ? 2 : 1;
            Rows = rows + (held is null ? 0 : 2);
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
            return x;
        }

        public SketchRecord Record(double[] x) => _sketch with
        {
            Points = _sketch.Points
                .Select(point => _index.TryGetValue(point.Id, out var i) ? point with { X = x[i], Y = x[i + 1] } : point)
                .ToList(),
        };

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
                default:
                    throw new InvalidOperationException($"The constraint {constraint.Id} has the kind '{constraint.Kind}', which the solver does not give.");
            }
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
            }
            _row++;
        }

        private double X(Guid point) => _index.TryGetValue(point, out var i) ? _x[i] : Point(_sketch, point).X;

        private double Y(Guid point) => _index.TryGetValue(point, out var i) ? _x[i + 1] : Point(_sketch, point).Y;
    }
}
