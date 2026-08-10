using Xunit;

// MyriaLib is built around static, process-wide services (see README §21, Known
// Limitations) — GameService, ItemFactory, GameConfig, Inventory.PageSize, etc. are
// all shared mutable state. Running test classes in parallel would make tests
// interfere with each other nondeterministically, so parallelization is disabled
// for this whole assembly rather than chased down per test class.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
