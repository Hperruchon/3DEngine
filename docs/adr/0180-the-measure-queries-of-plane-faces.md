---
id: 0180
title: The measure queries GetMassProperties, GetDistance and GetAngle read the exact solid of the owned kernel and the curve of the design
status: Accepted
topic: Kernel, queries
date: 2026-11-21
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Core/Queries/GetMassPropertiesQuery.cs
  - Engine.Core/Queries/GetMassPropertiesQueryHandler.cs
  - Engine.Core/Queries/GetDistanceQuery.cs
  - Engine.Core/Queries/GetDistanceQueryHandler.cs
  - Engine.Core/Queries/GetAngleQuery.cs
  - Engine.Core/Queries/GetAngleQueryHandler.cs
  - Engine.Core/Queries/MeasureTarget.cs
  - Engine.Core/Queries/NearestPoints.cs
  - Engine.Core/Queries/MeasureCurve.cs
  - Engine.Core/Queries/CurvedDistance.cs
  - Engine.Core/Queries/RevolvedDistance.cs
  - Engine.Core/Queries/CutMass.cs
  - Engine.Core/Features/KernelProjection.cs
  - Engine.Tests/Queries/MeasureQueryTests.cs
enforced-by: Engine.Tests/Queries/MeasureQueryTests.cs
notes: K15a, K15b and K15c. The answers Q1 A to Q4 A of the codebase review of 2026-11-21, by the standing order of the owner of 2026-11-20.
---

# ADR-0180 — The measure queries GetMassProperties, GetDistance and GetAngle read the exact solid of the owned kernel and the curve of the design

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K15 of the roadmap (ADR-0157 item 11) asks for measure queries in the engine, so that the window and an agent measure
by one path: the volume and the centre of a body, the minimum distance between two bodies, faces, edges or vertices with its
two points, and the cosine and the sine between two faces. The checks of K11 and the preview of P15 need the same measures.

The owned kernel holds each body as exact convex polygons of rationals (ADR-0057), and `KernelProjection` gives the solid of a
live body from the steps in effect (ADR-0058). `KernelSolid.Volume` gives the exact volume. No query gives a measure today,
so a client computes it from the doubles of `GetFaces`, and two clients can give two answers.

The codebase review of 2026-11-21 (sections 4 to 6) researched the task and asked four questions. The standing order of the
owner of 2026-11-20 takes option A of each:

- **Q1 A.** K15 comes first, in a slice for bodies of plane faces (K15a). K10b waits for the sizes of the lamp (R-0070).
- **Q2 A.** A body with curved faces gives the exact volume of its facets in the first slice, with a flag that says so.
- **Q3 A.** A query names a face or an edge by the body and the name that `GetFaces` gives, as a datum does.
- **Q4 A.** The results are doubles in millimetres, and a distance gives its two points.

The constraint that removes the obvious alternative, a measure on the doubles of the mesh, is the determinism of the
engine: two platforms must give the same answer, and an agent must get the answer of the window. The exact solid gives one
answer, and `+`, `-`, `*`, `/` and `sqrt` give it on each platform.

## Decision

1. **One path.** Three queries of `Engine.Core/Queries`, registered in `HandlerCatalog`: `GetMassProperties`,
   `GetDistance` and `GetAngle`. The route `/queries` of HTTP, the command `engine query` and `DocumentSession.Query` reach
   them through the registry, with no new route (ADR-0016). Each query reads the solid of the owned kernel through
   `KernelProjection.OfRecord`, so it needs no capability of the backend. A query writes nothing to the log.
2. **The names (Q3 A).** A body is its id. A face is the reference that `GetFaces` gives: the feature, the element, and the
   item when `GetFaces` gives one. An edge is the pair of the names of the two faces that a B-Rep edge bounds, as an edge of
   `GetFaces` gives them, in either order (ADR-0077 items 1 and 6). The parameters are flat, as those of `CreateDatum`,
   because a parameter of a query holds no object (ADR-0016): `bodyIdA`, `featureIdA`, `elementA`, `itemIdA`,
   `secondFeatureIdA`, `secondElementA`, `secondItemIdA`, and the same with `B`. `GetFaces` gives no name of a vertex, so
   K15a measures no vertex.
3. **GetMassProperties(bodyId).** The result is `volume` in mm³, `centre` (x, y, z) in mm, and `facets`. The volume is
   `KernelSolid.Volume`. The centre is the sum of the centres of the tetrahedra of a fan of each polygon with the origin,
   each weighted by its signed volume. Both are exact rationals, and each leaves as the nearest double. Since K15b, item
   10 gives the volume and the centre of the curve where a closed form exists, and since K15c item 14 for a body with a
   Subtract. `facets` is true when a face of the body is curved (item 6) and items 10 and 14 give no closed form: the volume
   and the centre are then those of the plane facets of the solid, and not those of the curve (Q2 A).
4. **GetDistance(A, B).** The result is `distance` in mm, `pointA` on side A and `pointB` on side B (Q4 A). Each side is a
   body, a face or an edge of a body of plane faces. The pieces of a side are the convex polygons of the body or of the face,
   or the segments of the edge. Between two pieces the nearest pair has a point on an edge of one of them, so the steps are
   a segment against a polygon and a segment against a segment, in exact rationals. A pair of pieces whose boxes are not
   nearer than the best pair is skipped. A body is a solid: when the pieces do not touch, a side inside the body of the
   other side is at the distance 0, and its first vertex is the common point. The test of inside is the exact subtract of
   the kernel: a cube about the point, small enough not to touch the surface, minus the solid gives no solid. When several
   pairs give the minimum, the result is the first pair in the order of the pieces, so a replay gives the same points.
5. **GetAngle(A, B).** The result is `cosine` and `sine` of the angle between the outward normals of two plane faces, from 0
   to 180 degrees. The two faces can be on two bodies. With the normals m and n, the square of the cosine is
   (m · n)² / (|m|² |n|²) and the square of the sine is |m × n|² / (|m|² |n|²), both exact; the sign of the cosine is the
   sign of m · n, and the sine is never negative. No angle is given (CLAUDE.md, determinism rule 2).
6. **Plane faces first (Q1 A).** A face name is curved when its pieces lie on more than one plane: the facets of a cylinder,
   a cone, a sphere, a torus or an arc of a profile, as `GetFaces` gives no normal for them. K15a refused each curved face.
   Since K15b, items 11 and 12 measure a whole curved face of a cylinder, a cone or a sphere in the cases that they give,
   and since K15c items 15 to 17 add the side of a cone, a part past an end of a cylinder, two curved faces and the side
   of an arc. `E-MEASURE-CURVED` refuses each other case; item 18 lists them. A plane face of a curved body, as the top of
   a cylinder, is measured as the polygon of its facets. `GetMassProperties` refuses no curved body (items 3, 10 and 14).
7. **The refusals.** A body that is not live gives `E-GEOM-BODY-NOT-FOUND`, a mesh body `E-MESH-UNSUPPORTED`, a body with a
   fault `E-GEOM-BODY-FAULTED`, and a body that the kernel cannot build `E-GEOM-FACES-FAILED`, as for `GetFaces`. A face or
   an edge that the body does not have, or a part of a name, gives `E-MEASURE-NAME-NOT-FOUND`.
8. **Determinism.** Each step that decides a case uses `+`, `-`, `*`, `/` and comparisons on exact rationals. In K15a the
   last step is one `Math.Sqrt` of a double that is the nearest double to an exact square: the distance, the cosine and the
   sine. Since K15b a value of the curve (items 10 to 12) is computed in doubles from the values of the commands with `+`,
   `-`, `*`, `/` and `Math.Sqrt`, the constant `Math.PI`, and the angle of an arc of item 13. No transcendental function of
   the framework is called, the gate of the determinism rules scans these files as each file of `Engine.Core`, and a query
   writes nothing to the log. Three steps of the distance from a curved face (item 11) compare doubles and not exact
   rationals: the choice of the nearest piece, the test of a point inside the solid, which runs only when the lowest
   distance is above 0, and the refusal when the bound past an end of a cylinder is below the best distance. Each gives the
   same bits on each platform, but an exact tie can refuse a case that has an answer (finding E232, TASK-0438). Since K15c,
   items 14 to 17 keep these rules: each decision of a case is exact, except the skip of a piece whose lower bound is not
   below the best distance, the refusal of item 11 against the best distance, and the decisions of a sphere against a
   side (item 16), which compare doubles; item 19 gives the precision of each value.
9. **The kernel does not change.** The queries read the public members `KernelSolid.Polygons`, `KernelSolid.Volume`,
   `KernelSolid.Box`, `KernelSolid.Translate`, `PlanarBoolean.Subtract`, `Brep.Of`, `PlaneR` and `Vector3R`. K15b reads the
   values of the design through `KernelProjection` of `Engine.Core` (items 10 and 11), and the kernel does not change. K15c
   reads the same members and `KernelProjection.Of` of the body before a Subtract; the kernel does not change.
10. **The volume of the curve (K15b).** `GetMassProperties` gives the volume and the centre of the curve of the design,
   with `facets` false, when the body is one command with only Translate and Rotate steps after it
   (`KernelProjection.Placed`, the map of the walk of the solid):
   - CreateCylinder: π r² h, the centre at the origin of the command;
   - CreateCone: π r² h / 3, the centre a quarter of the height over the base;
   - CreateSphere: 4 π r³ / 3; CreateTorus: 2 π² R r², each at the origin of the command;
   - an extrude of a sketch with an arc or a circle: the area of the profile with its exact arcs times the height
     (u × v) · n d on the exact frame of the kernel, and the centre of the profile at half the height. The area is the
     exact polygon of the first point of each edge of the loop of `SketchProfile`, and each arc adds its segment
     r² (θ - sin θ) / 2, with the moment c³ / 12 along its bisector for a chord of length c: with + when the walk follows
     the arc from its start to its end, and with - when it walks the arc back.

   The values are doubles from the doubles of the commands. A primitive is within a few units of the last place; the
   tests hold 1e-12 of the volume and 1e-9 mm for the centre, and no bound is proved. A Rotate moves the centre with the
   exact turn of the solid; the volume is that of the design. A body with a Subtract of a curved tool, as a plate with a
   hole, has item 14 since K15c; each other body with a curved face keeps the exact values of its facets with `facets` true.
11. **The distance from a whole curved face (K15b).** A curved face is whole when it is the side of a CreateCylinder, of a
   CreateCone or of an extrude of a circle, or the surface of a CreateSphere, with only Translate and Rotate steps after
   the command (`MeasureCurve.Whole`); its curve is that of ADR-0143 item 1. `GetDistance` measures a whole side of a
   cylinder and a whole sphere from the curve. The other side is a side of item 4, or a second sphere:
   - A sphere with the centre c and the radius r, against each piece P: with the exact nearest point q of P to c and the
     vertex v of P farthest from c, P is outside the ball at |q - c| - r, inside it at r - |v - c|, or it crosses the
     surface at the distance 0, at the point of the segment from q to v at the distance r from c.
   - The side of a cylinder from p to p + a, with the radius r: the part of a piece in the slab 0 ≤ (x - p) · a ≤ a · a,
     cut exactly, gives a closed form as for a sphere, with the distance ρ to the axis in place of the distance to c. A
     part past an end gives the lower bound (its distance to the axis segment) - r. When that bound is below the best
     distance, the nearest point can be past an end: K15b refused the case, and since K15c item 15 measures that part,
     and the case is refused only when item 15 has no closed form for it.
   - Two spheres at the distance D of their centres: D - r - s apart, |r - s| - D one inside the other, else 0.

   A body is a solid, as in item 4: when the curved face keeps a distance above 0 from each piece of a body, one point of
   the face decides if it lies inside the solid: the point of the sphere at +x, which is exact, or a vertex of the facets
   of the side, which is on the curve within the rounding of the turn. The pair of points is the first in the order of the
   pieces. K15b refused the side of a cone, two cylinders, a cylinder and a sphere, a torus, the side of an arc and a face
   of a body with a Subtract; since K15c items 15 to 17 measure the first five in the cases that they give.
12. **The angle of the side of a cylinder or a cone (K15b).** The outward normals of such a side make one angle β with its
   axis a, from the base to the other end or to the apex: 90 degrees for a cylinder, and cos β = r / L, sin β = h / L for a
   cone, with h = |a| and L² = r² + h². With a plane face whose outward normal is at the angle φ to a, `GetAngle` gives the
   angle |φ - β| to the nearest of these normals: cos φ cos β + sin φ sin β and |sin φ cos β - cos φ sin β|. Each term is
   the root of an exact square, as in item 5. So the side of a cylinder along the normal of a face gives 90 degrees, and the
   side of a cone and its base give 180 degrees less the angle at the rim. A sphere has an outward normal in each
   direction, so it is refused. Two curved faces have no one angle between their normals; since K15c item 17 gives the
   angle of their axes.
13. **The angle of an arc.** The sweep θ of an arc in item 10 comes from `MeasureCurve.Angle`, which uses `+`, `-`, `*`, `/`
   and `Math.Sqrt` only: each step tan(φ / 2) = tan φ / (1 + √(1 + tan² φ)) halves the angle until its tangent is below
   1e-3, a series of four terms gives that angle, and the halvings double it back. `Math.Atan2` is not used, because the
   gate of determinism rule 1 scans each file of `Engine.Core`, and because a measure must give the same bits on each
   platform as the queries of K15a do. Each halving rounds, so the angle is at most about ten units of the last place from
   the true angle: 8.8 in the probe of the second codebase review of 2026-11-21 against a reference of 240 bits, and
   `MeasureQueryTests` holds ten against `Math.Atan`. The angle is in [0, 2π]: a direction just under the axis x, as
   (1, -1e-17), gives exactly 2π, which the caller takes as a full turn (finding E232, TASK-0438).
14. **The volume of a body with a Subtract (K15c).** `GetMassProperties` gives the volume and the centre of the curve, with
   `facets` false, when the chain of the body is Translate, Rotate and Subtract steps on a first body of plane faces, and
   each Subtract that cuts a curve has a tool of one command with only Translate and Rotate steps after it
   (`CutMass`): a CreateCylinder, a CreateCone, a CreateSphere, or an extrude of a sketch of one circle and no other line
   or arc. The tool is a solid of revolution about the segment from p to p + a, with the largest radius R and the square of
   its radius R² q(s) at the part s of the segment: q = 1 for a cylinder, (1 - s)² for a cone with its apex at s = 1, and
   4 s (1 - s) for a sphere on its diameter along the axis z of its command. The closed form needs three conditions, each
   decided on exact rationals:
   - Each tool of an earlier Subtract keeps the distance R + R' from this segment to its segment, so the two holes do not
     meet; the faces of the earlier tools are left out of the next condition.
   - Each other face of the body before the Subtract, in the slab 0 ≤ s ≤ 1, is across the axis (its normal is parallel
     to a), or keeps the distance R from the segment.
   - The planes of the faces across the axis cut [0, 1] into parts. In each part the body holds the tool whole or not at
     all, and the test of inside of item 4 at the middle of the segment decides which, with a cube that keeps a quarter of
     the room: the lower of R and half the length of the part.

   The removed volume is π R² |a| times the exact integral of q over the parts that the body holds, and its centre is on
   the segment at the exact mean of s q over q. The volume is the volume of the body before the Subtract less the removed
   volume, and the centre is the balance of the two centres. A plate of 100 by 50 by 10 mm with a hole of radius 5 through
   it gives 50000 - 250 π mm³. Each other body with a curved face keeps the exact values of its facets with `facets` true:
   a hole that meets a face at an angle or that crosses a side face (a notch), two holes that meet, a tool of a torus, of an
   extrude of arcs or of several commands, a first body with a curved face (a tube), and a Union or an Intersect.
15. **The distance from the side of a cone, and past an end of a cylinder (K15c).** A side of revolution has a profile: the
   segment from (R0, 0) to (R1, H) in the half-plane (ρ, z) of a point, with ρ the distance to the axis, z the height from
   the base and H = |a|; a cylinder has R1 = R0, a cone R1 = 0. The distance from a point to the side is the distance from
   its (ρ, z) to the profile (`RevolvedDistance`). The part of a piece past an end of a cylinder, and each part of a piece
   against a cone, cut exactly at the plane of the base, has a closed form in three cases:
   - A piece in a plane across the axis at the part s: it holds each ρ from its exact nearest point to the axis to its
     farthest vertex, and the distance to the profile at the fixed height is a convex function of ρ. So the minimum is at
     the target radius R0 + (R1 - R0) s, with s clamped to [0, 1], clamped to that range: an exact comparison of squares.
     When the target is inside the range and 0 ≤ s ≤ 1, the piece crosses the side at the distance 0.
   - A segment along the axis: the distance to the profile is a convex function of z, so the minimum is at the height of
     the profile nearest to the line, clamped to the segment: each height of a cylinder, and for a cone the height where its
     radius is ρ, or its base or its apex, by exact comparisons of squares.
   - A polygon in a plane along the axis whose edges are each across or along the axis: the gradient of the distance in the
     plane is 0 only on the line of the plane nearest to the axis, where the distance is a convex function of z, or on the
     side. The curve where the side meets the plane ends at the rim of the base or at the apex, so it leaves the cut polygon
     through an edge. So the minimum is on an edge, and each edge is one of the first two cases.

   Each other piece, a slanted polygon or segment, has no closed form: its lower bound (its distance to the segment of the
   axis) - R0 skips it when it is not below the best distance, and else the query refuses the case. A box beside a cone or
   over a cylinder has a closed form; a turned box near the rim does not.
16. **Two curved faces (K15c).** Three pairs have a closed form:
   - A sphere with the centre c and the radius s, and the side of a cylinder or a cone: with the distance m from c to the
     side (item 15) and the distance M to its farthest point, an end of the profile on the other side of the axis, the
     sphere is m - s from the side, s - M around it, or it crosses it at the distance 0, at a point of the side at the
     distance s from c: on the half-plane of c between the nearest point and that end, or on the circle of that end.
   - The sides of two cylinders with parallel axes, at the exact distance D of the axes: each side is a circle times a span
     along the axis, so the square of the distance is the square of the distance of the circles, D - r - s apart, |r - s| - D
     one inside the other, or 0 when they cross, plus the square of the gap between the two spans. The cases are exact.
   - Two spheres, as in item 11.

   The query refuses the side of a cylinder and of a cone, two cones, two cylinders whose axes are not parallel, and the
   side of an arc with a curved face.
17. **The side of an arc, and the angle of two curved faces (K15c).** The side of an arc of the sketch of an extrude, with
   only Translate and Rotate steps after it, is a part of the side of a cylinder: its sector goes from the direction of the
   start counter-clockwise about u × v of the exact frame to the direction of the end. Its distance is that of the whole
   turn when the nearest point of the whole turn is in the sector, an exact test of the signs of cross products on the
   rationals of that point; else the query refuses it. Its angle to a plane face is that of item 12 when the part of the
   normal m across the axis points into the sector; else it is the angle of item 5 to the outward normal at the end of the
   arc with the larger signed square of the cosine. `GetConnectors` keeps the side of an arc as `Curved` (ADR-0183).
   Two sides of a cylinder, a cone or an arc have no one angle between their normals, because two turns of normals share a
   direction. `GetAngle` gives the angle of their axes as lines, from 0 to 90 degrees: the cosine |a · b| / (|a| |b|) and
   the sine |a × b| / (|a| |b|), each the root of an exact square. The direction of an axis depends on the order of the
   commands, as the base of a cylinder, and not on the part, so it does not give a sign. A sphere has no axis, and is refused.
18. **The cases that stay refused (K15c).** `E-MEASURE-CURVED` refuses: a body with a curved face as a side of
   `GetDistance`, because a body with a curved face is a solid with no closed form for its inside; an edge of a curved
   face; a face of a body with a Subtract, a Union or an Intersect, as the side of a hole; a torus; a slanted piece near the
   side of a cone or past an end of a cylinder, as item 15 gives; the side of an arc whose nearest point of the whole turn
   is not on the arc; the pairs of item 16 with no closed form; and a sphere for an angle. A vertex is not a side of
   `GetDistance`: no query gives a name of a vertex. `GetFaces` and `GetConnectors` give faces, edges and datums, and the
   segments of `GetFaces` give positions only, so no public name is invented. Row K15d of the roadmap holds these cases.
19. **The precision of the values (K15c).** Each decision is exact except those of item 8. The values are doubles:
   - Item 14: the volume of the body before the Subtract is the nearest double to an exact rational; the removed volume is
     π R² |a| times the nearest double to an exact integral, with one root of a · a. So about five roundings: the volume
     is within a few units of the last place of the larger of the two volumes for each Subtract, and the centre adds three
     more. No other bound is proved; the tests hold 1e-12 of the volume and 1e-9 mm of the centre.
   - Items 15 and 16: one root of the exact square of ρ, and the projection of (ρ, z) on the profile in doubles, about ten
     operations; a crossing is the root of a quadratic in doubles, as in item 11. The value is within a few units of the
     last place of the sizes of the case. The decisions of a sphere against a side compare doubles, so at a tie within the
     rounding the value is 0 or within the rounding of 0. The tests hold 1e-12.
   - Item 17: each cosine and sine is the root of the nearest double to an exact square, as in item 5: within one unit of
     the last place. The test of the sector of a distance is exact on the double of the point, so an end of the arc within
     the rounding of that double can be taken as in the arc or not.

## Consequences

**Good:** The window, an agent, the checks of K11 and the preview of P15 can measure by one query, with one answer on each
platform. A gap of the bookcase of K10a is one query. No contract changes: the results use `DatumPoint` of
`Engine.Contracts.Sketch`, as `GetFaces` does.

**Bad:** The exact numbers have a cost. After 150 exact turns the first read of a solid took about 2 s (ledger entry v0.413),
and a measure of such a body pays it. The count of the pairs of pieces is the product of the counts of the two sides; the
test of the boxes removes most pairs, but a body of many polygons inside the box of the other side is slow. The test of
inside builds two trees of the kernel. A vertex has no name, so no query measures from a vertex. Fourteen flat parameters are
long for a person to type; a client gives them from `GetFaces`.

Since K15b: the values of the curve are doubles of the design, and not exact rationals, so they are not exact as the values of
K15a are. A body with a Subtract and a curved face, as a plate with a hole, still gives the volume of its facets, which is
off the curve by the segments of the facets. The distance from the side of a cone, from the side of an arc, between two
cylinders, and past an end of a cylinder, and the angle of two curved faces, are refused. The angle of a curved face is to
the nearest outward normal of the whole turn, a choice that a person must know. The test of inside for a cylinder reads a
vertex of its facets, which is off the curve by the rounding of the turn, so a distance below about 1e-12 of the radius can
give a wrong answer to that test.

Since K15c: a plate with holes gives the volume of its curve, and the side of a cone, a part past an end of a cylinder,
the side of an arc and three pairs of curved faces have a distance. The closed forms hold for faces across or along an
axis only: a slanted piece near a cone or past an end of a cylinder, a hole at an angle, a notch and two holes that meet
still give a refusal or the volume of the facets. Item 14 builds the solid of the body before each Subtract again, and
the test of inside of item 4 runs once for each part of the segment of a tool, so a plate with many holes pays one exact
boolean for each hole. The angle of two curved faces is the angle of their axes, with no sign, a choice that a person must know. The
exact facets of a sphere through a plate take about 45 s in the kernel before a measure, so the test uses a cap.

**Next:** K15d holds the cases of item 18 and a measure from a vertex when a name of a vertex exists. K11 builds its pair
check on item 4.

## History

- 2026-11-21 · TASK-0432 · The three queries for bodies of plane faces, the volume of the facets of a curved body, and the
  codes `E-MEASURE-NAME-NOT-FOUND` and `E-MEASURE-CURVED`.
- 2026-11-21 · TASK-0434 · Items 10 to 13: the volume of the curve of a primitive and of an extrude of arcs, the distance
  from a whole curved face of a cylinder or a sphere, the angle of the side of a cylinder or a cone; items 3, 6, 8 and 9
  say so, and the rest waits for K15c.
- 2026-11-21 · TASK-0438 · Items 8 and 13: the three steps that compare doubles, the bound of about ten units of the last
  place of the arctangent with its test, and the range [0, 2π] (finding E232 of the second codebase review of 2026-11-21).
- 2026-11-21 · TASK-0444 · Items 14 to 19: the volume of a body with a Subtract of a curved tool, the distance from the
  side of a cone and past an end of a cylinder, three pairs of curved faces, the side of an arc, the angle of two curved
  faces, the cases that stay refused and the precision of each value; items 3, 6 and 8 to 12 say so, and the rest waits
  for K15d.
