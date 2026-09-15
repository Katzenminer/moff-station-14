using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Server.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction
{
    [TestFixture]
    public sealed class ConstructionPrototypeTest : GameTest
    {
        // discount linter for construction graphs
        // TODO: Create serialization validators for these?
        // Top test definitely can be but writing a serializer takes ages.

        private static string[] _constructablePrototypes = GameDataScrounger.EntitiesWithComponent("Construction");
        private static string[] _constructions = GameDataScrounger.PrototypesOfKind<ConstructionPrototype>();

        /// <summary>
        /// Checks every entity prototype with a construction component has a valid start node.
        /// </summary>
        /// <remarks>
        /// This loops over every prototype in one pass instead of using a
        /// <see cref="TestCaseSourceAttribute"/> per prototype (as it used to): each case here is a
        /// cheap dictionary lookup with no entity spawning or ticking, so the ~480 pooled
        /// pair setup/teardown cycles that used to cost were pure overhead, not test time. See
        /// <see cref="MachineBoardTest"/> for the same pattern applied to board validation.
        /// </remarks>
        [Test]
        [TestOf(typeof(ConstructionComponent))]
        [Description("Tests that every entity specifies a valid node for construction, and optionally a valid one for deconstruction.")]
        public async Task ConstructionComponentValid()
        {
            var pair = Pair;
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    foreach (var protoKey in _constructablePrototypes)
                    {
                        var proto = protoMan.Index(protoKey);
                        var construction = (ConstructionComponent)proto.Components["Construction"].Component;

                        var graph = protoMan.Index<ConstructionGraphPrototype>(construction.Graph);

                        Assert.That(graph.Nodes.ContainsKey(construction.Node),
                            $"Found no node \"{construction.Node}\" on graph \"{graph.ID}\" for entity \"{proto.ID}\"!");

                        if (construction.DeconstructionNode is not { } target)
                            continue;

                        Assert.That(graph.Nodes.ContainsKey(target),
                            $"Invalid deconstruction node \"{target}\" on graph \"{graph.ID}\" for construction entity \"{proto.ID}\"!");
                    }
                }
            });
        }

        /// <remarks>
        /// See the remarks on <see cref="ConstructionComponentValid"/> - same one-pass-over-everything
        /// change, same reasoning.
        /// </remarks>
        [Test]
        [TestOf(typeof(ConstructionPrototype))]
        [Description("Tests that every construction prototype has a valid starting and target node, and a valid path between them.")]
        public async Task ConstructionFormsValidGraph()
        {
            var pair = Pair;
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();
            var entMan = server.ResolveDependency<IEntityManager>();

            await server.WaitAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    foreach (var protoKey in _constructions)
                    {
                        var proto = protoMan.Index<ConstructionPrototype>(protoKey);
                        var start = proto.StartNode;
                        var target = proto.TargetNode;
                        var graph = protoMan.Index(proto.Graph);

                        Assert.That(graph.Nodes.ContainsKey(start),
                            $"Found no startNode \"{start}\" on graph \"{graph.ID}\"!");
                        Assert.That(graph.Nodes.ContainsKey(target),
                            $"Found no targetNode \"{target}\" on graph \"{graph.ID}\"!");

                        // These depend on each other (each step dereferences the previous one's
                        // result), so - unlike the two checks above - they can't just be more
                        // Assert.That calls inside this Multiple scope: those don't throw until the
                        // scope closes, so execution would fall through into a null path[0]
                        // instead of stopping here.
                        if (!graph.TryPath(start, target, out var path))
                        {
                            Assert.Fail($"Unable to find path from \"{start}\" to \"{target}\" on graph \"{graph.ID}\"");
                            continue;
                        }

                        if (path is not { Length: >= 1 })
                        {
                            Assert.Fail($"Unable to find path from \"{start}\" to \"{target}\" on graph \"{graph.ID}\".");
                            continue;
                        }

                        var next = path[0];
                        var nextId = next.Entity.GetId(null, null, new(entMan));
                        if (nextId is null)
                        {
                            Assert.Fail($"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) must specify an entity! Graph: {graph.ID}");
                            continue;
                        }

                        if (!protoMan.TryIndex(nextId, out EntityPrototype entity))
                        {
                            Assert.Fail($"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) specified an invalid entity prototype ({nextId} [{next.Entity}])");
                            continue;
                        }

                        Assert.That(entity.Components.ContainsKey("Construction"),
                            $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) specified an entity prototype ({next.Entity}) without a ConstructionComponent.");
                    }
                }
            });
        }
    }
}
