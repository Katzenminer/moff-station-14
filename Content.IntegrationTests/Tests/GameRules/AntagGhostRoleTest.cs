#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._ES.Voting.Components;
using Content.Shared.Antag;
using Content.Shared.Players;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed partial class AntagGhostRoleTest : AntagTest
{
    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        Map = PoolManager.TestStation
    };

    [SidedDependency(Side.Server)] private IRobustRandom _random = default!;
    [SidedDependency(Side.Server)] private GhostRoleSystem _ghostRole = default!;

    private static readonly string[] AntagGameRules = GameDataScrounger.EntitiesWithComponent("AntagSelection");

    /// <summary>
    /// This is the one test in this fixture whose cost scales with GameRule count - it used to be
    /// [TestCaseSource]'d (one case per rule), but this class's Dirty = true PoolSettings meant every
    /// case paid a full round restart even though each rule's check is only ~1-2s of actual work; on
    /// real CI that was ~900s for ~40 rules. TestAntagGhostRolesSequential already proves a single
    /// round can safely start/take-from many rules in a row, so this loops all rules in one round
    /// instead, calling ClearGameRules() between each rule the same way the per-case version did
    /// between pooled pairs. That surfaced a real gap: ClearGameRules() removes rule tracking but not
    /// the ghost-role-spawner entities a rule spawned, so later iterations must filter the spawner
    /// query down to the current gameRule instead of assuming the world is otherwise empty of them
    /// (true under the old fresh-pair-per-case model, not true here). Wrapped in
    /// Assert.EnterMultipleScope() so one bad rule doesn't hide the rest; unlike the per-case version,
    /// assertions here don't throw-to-short-circuit, so the one spot that dereferences a value from a
    /// preceding assertion (antag, from the AntagSelectionComponent lookup) gets an explicit continue
    /// instead of relying on that.
    /// </summary>
    [Test]
    [TestOf(typeof(GameTicker)), TestOf(typeof(AntagSelectionSystem)), TestOf(typeof(AntagSelectionComponent)), TestOf(typeof(GhostRoleSystem))]
    [Description($"Ensures all GameRule entities with {nameof(AntagSelectionComponent)} can properly spawn those roles and they can be taken.")]
    [RunOnSide(Side.Server)]
    public void TestAntagGhostRoles()
    {
        using var _ = Assert.EnterMultipleScope();
        foreach (var ruleId in AntagGameRules)
        {
            var rule = SProtoMan.Index<EntityPrototype>(ruleId);
            if (!rule.TryComp<AntagSelectionComponent>(out var antag, SEntMan.ComponentFactory))
            {
                Assert.Fail($"{ruleId} has no {nameof(AntagSelectionComponent)}");
                continue;
            }

            STicker.StartGameRule(ruleId, out var gameRule);

            Dictionary<ProtoId<AntagSpecifierPrototype>, int> rules = [];

            // Moff start - Enrolls don't spawn ghost roles. instead we check if vote entities were spawned
            if (antag!.SelectionTime == AntagSelectionTime.Enroll)
            {
                if (!STryComp<ESSynchronizedVoteManagerComponent>(gameRule, out var voteManager) || voteManager!.VoteEntities.Count == 0)
                    Assert.Fail($"{ruleId} (Enroll) did not spawn vote entities");

                STicker.ClearGameRules();
                continue;
            }
            // Moff end

            foreach (var selector in antag!.Antags)
            {
                var specifier = SProtoMan.Index(selector.Proto);
                var count = selector.GetTargetAntagCount(_random, 1);
                // We should always spawn at least one antag if we add a GameRule
                Assert.That(count, Is.GreaterThan(0), $"{ruleId}: expected at least one antag from {selector.Proto}");

                if (specifier.SpawnerPrototype == null)
                    continue;

                var value = rules.GetValueOrDefault(specifier);
                rules[selector.Proto] = value + count;
            }

            var roleEnumerator = SEntMan.EntityQueryEnumerator<GhostRoleAntagSpawnerComponent, GhostRoleComponent, TransformComponent>();
            while (roleEnumerator.MoveNext(out var spawner, out var role, out var xform))
            {
                // Only look at spawners belonging to the rule we just started. ClearGameRules()
                // removes rule tracking but not the spawner entities themselves, so leftover
                // spawners from earlier rules in this loop are expected here - they were already
                // checked (and counted) in their own iteration, back when they were the only ones.
                if (spawner.Rule != gameRule)
                    continue;

                if (spawner.Definition is null)
                {
                    Assert.Fail($"{ruleId}: ghost role spawner has no Definition");
                    continue;
                }

                AssertGhostRoleTaken(spawner, role, xform);
                var value = rules.GetValueOrDefault(spawner.Definition.Value);
                rules[spawner.Definition.Value] = value - 1;
            }

            // Ensure all ghost roles spawned and were assigned!!!
            Assert.That(rules.Values, Is.All.Zero, $"{ruleId}: not all expected ghost roles spawned/were assigned");

            // End all rules
            STicker.ClearGameRules();
            Assert.That(STicker.GetAddedGameRules(), Is.Empty, $"{ruleId}: ClearGameRules did not remove all added rules");
        }
    }

    [Test]
    [TestOf(typeof(GameTicker)), TestOf(typeof(AntagSelectionSystem)), TestOf(typeof(AntagSelectionComponent)), TestOf(typeof(GhostRoleSystem))]
    [Description("Ensures a player can take all antag ghost roles sequentially without transferring unwanted mind data.")]
    [RunOnSide(Side.Server)]
    public void TestAntagGhostRolesSequential()
    {
        foreach (var ruleId in AntagGameRules)
        {
            var rule = SProtoMan.Index<EntityPrototype>(ruleId);
            Assert.That(rule.TryComp<AntagSelectionComponent>(out var antag, SEntMan.ComponentFactory), Is.True);
            // Moff start - Enrolls don't spawn ghost roles. instead we check if vote entities were spawned
            if (antag!.SelectionTime == AntagSelectionTime.Enroll)
                continue;
            // Moff end
            STicker.StartGameRule(ruleId);
        }

        var mind = ServerSession!.GetMind();

        var roleEnumerator = SEntMan.EntityQueryEnumerator<GhostRoleAntagSpawnerComponent, GhostRoleComponent, TransformComponent>();
        while (roleEnumerator.MoveNext(out var spawner, out var role, out var xform))
        {
            AssertGhostRoleTaken(spawner, role, xform);
            var newMind = ServerSession!.GetMind();
            Assert.That(newMind, Is.Not.EqualTo(mind));
            mind = newMind;
        }

        // End all rules
        STicker.ClearGameRules();
        Assert.That(STicker.GetAddedGameRules(), Is.Empty);
    }

    private void AssertGhostRoleTaken(GhostRoleAntagSpawnerComponent spawner, GhostRoleComponent role, TransformComponent xform)
    {
        // Ensure the ghost role spawner spawned correctly!
        Assert.That(spawner.Definition, Is.Not.Null);
        Assert.That(xform.MapUid, Is.Not.Null);
        Assert.That(xform.MapID, Is.Not.EqualTo(MapId.Nullspace));

        // Take the ghost role and ensure we take it!
        Assert.That(_ghostRole.Takeover(ServerSession!, role.Identifier), Is.True);
        Assert.That(ServerSession!.AttachedEntity, Is.Not.Null);
        var antag = SProtoMan.Index(spawner.Definition);
        SAssertAntagInitialized(antag, ServerSession);

        // Ensure we spawned in the correct location
        var sessionXform = SEntMan.GetComponent<TransformComponent>(ServerSession.AttachedEntity.Value);
        Assert.That(sessionXform.MapUid, Is.EqualTo(xform.MapUid));

        // We break it up like this cause otherwise it'll sometimes randomly fail
        // TODO: Engine IEquatable for EntityCoordinates
        Assert.That(sessionXform.Coordinates.EntityId, Is.EqualTo(xform.Coordinates.EntityId));

        // I will not get heisentest due to floating point errors
        Assert.That(MathHelper.CloseTo(sessionXform.Coordinates.X, xform.Coordinates.X, 0.001f), Is.True);
        Assert.That(MathHelper.CloseTo(sessionXform.Coordinates.Y, xform.Coordinates.Y, 0.001f), Is.True);
    }
}
