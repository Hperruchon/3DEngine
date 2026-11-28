# The pair check of bodies with holes (K24)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-28. TASK-0493, phase K24. Written under the standing order of 2026-11-28.
- Status: a proposal. Not a rule. It changes no code and no ADR. The row K24 says that its ADR decides the method; the method
  changes the time that a query can hold the session, so the owner chooses it.

## 1 The question

Row K24: the bound of work of ADR-0182 item 8 refuses 8 of the 21 pairs of the lamp case of K10b with `E-CHECK-WORK-LIMIT`, from
126,968 to 2,801,550 products of pieces for one pair, above the limit of 100,000. The travel of K12a uses the same bound for each of
its samples, so the lamp case has no travel either. The row asks for fewer pieces for a body with a hole, or a larger bound with a
time limit.

## 2 The facts

Each fact was read in the repository or measured on 2026-11-28 with a probe test that was not committed (a Debug build on the
computer of the owner).

1. **The bound.** ADR-0182 item 8 counts f_A × f_B for each pair before its first split and refuses the check above 100,000. The
   measures that set it: an intersection of a sphere and a plate took 3.2 s for 23,808 products (about 130 µs for each product), and
   the gap of two spheres apart took 101 s for 15,745,024 products (about 6.4 µs). At the limit an intersection takes about 13 s and
   a gap about 0.6 s.
2. **The pieces of the lamp case.** The booleans of the holes cut each plane face into many convex pieces: the crossbar has 1,966
   pieces on 138 planes, the bracket 1,425 on 204, the knob 538 on 196, and the arm 236 on 70.
3. **A merge of the fragments.** When two convex pieces of one face that share an edge merge while their union is convex, the
   crossbar keeps 776 pieces, the bracket 605, the knob 322 and the arm 196, about 2.5 times fewer. The merge does not join pieces
   that meet at a T-junction, so the worst pair keeps about 660,000 products.
4. **The gap search.** `NearestPoints.Nearest` of the crossbar and the knob, which are apart, takes 9.2 s for 1,057,708 products
   (about 8.7 µs each): it compares the exact boxes of each pair of pieces in rational numbers, and starts from the first pair, so
   its first bound is weak. Two bodies that touch end the search at the first pair with a gap of zero (6 ms for the bracket and the
   arm).
5. **The intersection.** Pairs whose boxes share a volume, as a shaft in a hole, run `PlanarBoolean.Intersect`, which costs about 20
   times more for each product than the gap.

## 3 The recommended method

1. **Fewer pieces in the check (fact 3).** The check, and the travel, merge the convex pieces of each face that share an edge
   before the bound, in exact numbers. The solids of the Document do not change, so the faces, the exports, the hashes and the
   images do not change.
2. **A faster gap search (fact 4).** A filter of the boxes in doubles, rounded outward so that it never skips a pair that the exact
   test keeps, and a first bound from the pair whose boxes are nearest. The answer, the first nearest pair in the order of the
   pieces, does not change; only the time does.
3. **A bound from new measures.** The bound stays a count of products, which a replay and each platform count the same, and no time
   decides a result (ADR-0173 item 4). A new measure of the worst pair of the lamp case with items 1 and 2 sets the new value, so that
   one check takes about the time of today's limit.

## 4 What the method costs

- Item 1 is new exact code in the kernel side of the check, with a test that the merged pieces give the same gap and the same
  intersection volume as the pieces of the solid.
- Item 2 changes no answer, so a test compares the old and the new search on the scenes of the tests of K11.
- Item 3 changes ADR-0182 item 8 and ADR-0188 item 8 (one preview has the bound of one pair, the answer Q4 A of 2026-11-26).
- An intersection stays expensive, so a pair with a shaft in a hole can stay near the new bound.

## 5 The questions for the owner

1. **Q1 (the method).** **A (recommended):** the three items of section 3. **B:** fewer pieces only, by a full convex
   decomposition of each face in the kernel, with T-junctions. **C:** a larger bound with a time limit, which the row names; a
   time then decides an answer, and a slow runner gives another answer than a fast one.
2. **Q2 (where the merge runs).** **A (recommended):** in the check and the travel only; the solids of the Document stay. **B:**
   in the kernel after each boolean, so each reader gets fewer pieces; each solid, hash and image changes, and each test of the
   faces needs a new value.
3. **Q3 (the bound).** **A (recommended):** a new count of products from a new measure, so that one check takes about 13 s at most
   on the computer of the owner, as today. **B:** keep 100,000, and accept that the lamp case stays refused in part.

## 6 Out of this document

- The travel of the lamp case: K12a and K12b work on it when the check works on it.
- The order of K12 and K24: P0.202 (TASK-0492) records it.
