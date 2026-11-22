namespace Engine.Tests.Features;

// The collection "kept booleans" (TASK-0457, phase P0.186): each test class that fills the kept results of the boolean steps
// past their limit, or that counts the booleans of the kernel or the kept results. The projection keeps those results in one
// static store of the process, at most KernelProjection.BooleanLimit, and the result of the oldest use goes first (ADR-0059
// item 2). A test that adds more results than the limit pushes out the results of a test that runs at the same time, and a
// reader of a pushed-out result makes the boolean again: the same result, but one boolean more. In run 45021528072 on main,
// A_Sphere_Cuts_A_Hole_Of_One_Name counted two booleans for this reason.
//
// With parallel runs off, xUnit runs this collection after the parallel collections and alone, as the collection "real host"
// (Engine.Tests/Http/RealHostCollection.cs). KeptBooleanCollectionGateTests holds the rule: each test class whose source
// names BooleansOnThisThread, BooleansKept, IsBooleanKept or BooleanLimit carries [Collection("kept booleans")].
[CollectionDefinition("kept booleans", DisableParallelization = true)]
public sealed class KeptBooleanCollection;
