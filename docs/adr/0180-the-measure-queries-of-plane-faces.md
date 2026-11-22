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
notes: K15a, K15b, K15c and K15d. The answers Q1 A to Q4 A of the codebase review of 2026-11-21, by the standing order of the owner of 2026-11-20.
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
   10 gives the volume and the centre of the curve where a closed form exists, since K15c item 14 for a body with a
   Subtract, and since K15d item 20 for holes on one axis and a tube. `facets` is true when a face of the body is curved
   (item 6) and items 10, 14 and 20 give no closed form: the volume and the centre are then those of the plane facets of
   the solid, and not those of the curve (Q2 A).
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
   since K15c items 15 to 17 add the side of a cone, a part past an end of a cylinder, two curved faces and the side
   of an arc, and since K15d items 21 and 22 add the side of a hole or of a tube and three more pairs of curved faces.
   `E-MEASURE-CURVED` refuses each other case; item 23 lists them. A plane face of a curved body, as the top of a
   cylinder, is measured as the polygon of its facets. `GetMassProperties` refuses no curved body (items 3, 10, 14 and 20).
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
   side (item 16), which compare doubles; item 19 gives the precision of each value. Since K15d, items 20 to 22 decide
   each new case on exact rationals, and item 24 gives the precision of each new value.
9. **The kernel does not change.** The queries read the public members `KernelSolid.Polygons`, `KernelSolid.Volume`,
   `KernelSolid.Box`, `KernelSolid.Translate`, `PlanarBoolean.Subtract`, `Brep.Of`, `PlaneR` and `Vector3R`. K15b reads the
   values of the design through `KernelProjection` of `Engine.Core` (items 10 and 11), and the kernel does not change. K15c
   reads the same members and `KernelProjection.Of` of the body before a Subtract; the kernel does not change. K15d
   reads the same members; the kernel does not change.
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
   extrude of arcs or of several commands, a first body with a curved face (a tube), and a Union or an Intersect. Since
   K15d, item 20 gives holes that meet on one axis and a tube.
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
   side of an arc with a curved face. Since K15d, item 22 measures them in the cases that it gives.
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
   Since K15d, item 23 replaces this list.
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
20. **Holes on one axis and a tube (K15d).** The tools of item 14 of a cylinder, a cone or an extrude of one circle whose
   axes are on one line make one group, as a hole with its counterbore or its countersink (`CutMass`). A later tool on
   that line joins the group while no Translate or Rotate comes between them, because the group keeps the solid before its
   first tool for the test of inside. The radius of the group at the part s of its line is the largest radius of its tools
   there. The parts are cut at the planes of item 14, at the ends of the tools and at each point where two radii are equal,
   so on each part the radius is one linear function α + β s, and the integrals of (α + β s)² and of s (α + β s)² are exact.
   The removed volume is π |a| times the integral over the parts that the body holds, with the conditions of item 14 for
   the largest radius of the group; the tools of two groups keep the sum of their radii apart. A first body of one
   CreateCylinder, CreateCone or extrude of one circle, with only Translate and Rotate steps after it, takes tools on its
   axis only (a tube): its volume and its centre are those of item 10, and the removed volume is the integral of the square
   of the lower of its radius and the radius of the group over its span, with no test of inside. A sphere stays alone in
   its group. A plate of 40 by 40 by 10 with a hole of radius 3 and a counterbore of radius 5 and depth 4 gives
   16000 - 154 π mm³; a tube of radius 10 less 6 and height 20 gives 1280 π mm³.
21. **The side of a hole (K15d).** The parts of item 20 give the faces too. On a part that the body holds, the side of the
   tool with the largest radius is a face of the body; on a part of a tube where the radius of the group is below that of
   the first body, the side of the first body is a face as well. So the side of a hole of items 14 and 20, and each side of a
   tube, is the side of a cylinder or of a frustum on each part, from its larger radius R0 to its smaller radius R1
   (`CutMass.Sides`), and a hole through the two arms of a clevis has two parts. `GetDistance` measures each part by items
   11, 15 and 16 with the profile from (R0, 0) to (R1, H), and gives the lowest, the first part in the order of the line
   when several give it. `GetAngle` gives the angle of item 12 with cos β = (R0 - R1) / L and L² = (R0 - R1)² + h². The
   outward normals of the side of a hole point to the axis, at 180 degrees less β, so the terms of cos β change their sign:
   the side of a countersink of 90 degrees is at 45 degrees to the top face, and its tool alone at 135 degrees. Two tools
   with the same radius on one part give no one face there, and the face is refused. The zone of a sphere, the side of a
   notch, of a hole at an angle or of two holes that meet on two axes, and a face of a body with a Union or an Intersect
   have no parts and are refused. `GetConnectors` reads `MeasureCurve.Whole` only, so the side of a hole stays `Curved`
   there (ADR-0183).
22. **Two curved faces (K15d).** Three more pairs have a closed form:
   - Two sides on one axis, as a cylinder and a cone, two cones, or a pin in a countersink. For x on one side and y on the
     other, |x - y|² = ρ² + σ² - 2 ρ σ cos Δθ + Δz² is lowest at Δθ = 0, so the distance is that of the two profiles in one
     half-plane. With the heights Z = (x - P) · A, the square of a length there is dρ² + dZ² / (A · A), and the method of
     the segments of item 4 gives the nearest pair on exact rationals. The half-plane is in each arc; two arcs on one axis
     with no common direction are refused.
   - Two cylinders whose axes are not parallel. Each point of a side is within its radius of the line of its axis, so
     D - r - s is a lower bound, with D the distance of the two lines. The common perpendicular of the lines reaches it
     when its two feet are on the two spans and D ≥ r + s, each decided on exact rationals.
   - The side of an arc and a curved face. The side of an arc is a part of its whole turn. The lowest distance over a part
     is not below the lowest over the whole turn, and is equal when the part holds the pair. So a pair of items 11, 16 or 22
     holds when its point on the side of each arc is on the arc, by the exact test of item 17; else the case is refused.
23. **The cases that stay refused (K15d).** `E-MEASURE-CURVED` refuses these cases, each with its reason:
   - A slanted piece near the side of a cone, near a rim of a cylinder or of a part of a hole; an edge of a curved face; a
     body with a curved face as a side of `GetDistance` (its plane faces end at a rim); a torus; two cylinders whose common
     perpendicular is past an end of a span or whose sides cross; a cone and another side on another axis. The nearest pair
     can be between a segment, or a circle, and a circle. On the line x(t), the distance to a circle of radius r is
     √(A(t) + r² - 2 r √B(t)) with A and B quadratics, and its stationary points are the roots of the polynomial of degree
     four A'² B - r² B'². Its formula needs a cube root, and a search for its roots on exact rationals (a Sturm sequence and
     halvings) needs a proved bound of its work, which this item does not give. The distance to a torus is the distance to
     its centre circle less its tube radius, the same polynomial. No iterative solver is added.
   - The side of an arc whose nearest point of the whole turn is not on the arc: the nearest point is then on an end line of
     the arc, on an arc of a rim, or at another local minimum of the side.
   - A notch, a hole at an angle, two holes that meet on two axes, a tool of a torus, of an extrude of arcs or of several
     commands, and a Union or an Intersect: the volume keeps its facets with `facets` true, and the side is refused. A notch
     and two holes on parallel axes need the area of a disc cut by lines or by a second disc, a later closed form with the
     angle of item 13; a hole at an angle needs a test that each slanted face holds its whole ellipse, and moves the centre
     by a term for each slanted face; two holes on axes that are not parallel meet in a volume of elliptic integrals.
   - A sphere for an angle (item 12).
   - A vertex as a side of `GetDistance`. No query gives a name of a vertex: `GetFaces` gives faces and edges, with the
     segments of an edge as positions, and `GetConnectors` gives datums, faces and axes. K15b, K15c and K15d added no name
     of a vertex, and none is invented.
24. **The precision of the values (K15d).** Each decision of items 20 to 22 is exact, except those of item 8 that the
   code of items 11, 15 and 16 makes for each part of a hole. The values are doubles:
   - Item 20: the integral of each part is exact; the volume of a group is π |a| times the nearest double to the exact
     integral, with one root of a · a, and the balance of the centres adds about three roundings for each group. So the
     volume is within a few units of the last place of the larger volume. No other bound is proved; the tests hold 1e-12
     of the volume and 1e-9 mm of the centre.
   - Item 21: a part of a hole gives the values of items 11, 15 and 16, about ten operations. Its point for the test of
     inside is a point of the curve from doubles, within the rounding of the turn, as the vertex of the facets of item 11.
   - Item 22: two sides on one axis give an exact square and one root; the points add the direction across the axis in
     doubles. Two cylinders give the exact square of D, one root and two subtractions. Each value is within a few units of
     the last place of the sizes of the case. The tests hold 1e-12.

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

Since K15d: the side of a hole, a counterbore, a countersink, a tube, two sides on one axis, two cylinders that cross at a
distance and the side of an arc with a curved face have a closed form. The cases of item 23 need the distance from a circle,
a root of a polynomial of degree four, and stay refused: a slanted face near a rim, a torus, a body with a curved face as a
side, a notch and a hole at an angle. Item 20 builds the solid of the body before each group again, and the test of inside
runs once for each part, as in item 14. A tool on the axis of a group joins it only before a Translate or a Rotate.

**Next:** The row K15d of the roadmap keeps the cases of item 23. The distance from a circle needs a search for the roots of
a polynomial of degree four with a proved bound of its work; a measure from a vertex waits for a name of a vertex. K11
builds its pair check on item 4.

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
- 2026-11-21 · TASK-0450 · Items 20 to 24: holes on one axis and a tube, the side of a hole and of a tube, two sides on one
  axis, two cylinders whose axes are not parallel, the side of an arc with a curved face, the cases that stay refused with
  their reasons, and the precision of each value; items 3, 6, 8, 9, 14, 16 and 18 say so.
