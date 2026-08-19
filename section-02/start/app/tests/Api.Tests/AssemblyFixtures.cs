using ExpenseTracker.Api.Tests;

// One Postgres container is started once for the whole test assembly and injected into
// test classes via their constructor. Test parallelization is disabled because the classes
// share a single database and each test resets it; running them in parallel would let one
// class's reset wipe another's rows mid-test.
[assembly: AssemblyFixture(typeof(ExpenseApiFactory))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
