namespace Engine.Tests.Http;

// The collection "real host": the tests that start the real host as a process,
// and the heartbeat test, which measures time. With parallel runs off, xUnit runs
// this collection after the other collections and alone, so a process start does
// not run beside a measurement of time and the load of the other tests does not
// reach it.
//
// Until TASK-0059 the name was on two test classes and no definition existed,
// so the collection ran beside the other tests, against the comment in
// HostGuardTests (finding T12 of the codebase review of 2026-10-04).
[CollectionDefinition("real host", DisableParallelization = true)]
public sealed class RealHostCollection;
