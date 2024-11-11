using System.Collections.Generic;
using Content.Server._Sunrise.ERP.Systems;
using Content.Shared._Sunrise.ERP;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Lust;

[TestFixture]
public sealed class LactationTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: FemaleDummy
  parent: MobHumanDummy
  components:
  - type: HumanoidAppearance
    sex: Female";

    [Test]
    public async Task TestLactation()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var map = await pair.CreateTestMap();

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var entityManager = server.ResolveDependency<IEntityManager>();

        var interactionSystem = entityManager.System<InteractionSystem>();
        var solutionContainerSystem = entityManager.System<SharedSolutionContainerSystem>();

        var femaleUid = entityManager.SpawnEntity("FemaleDummy", new MapCoordinates(0, 0, map.MapId));
        var maleUid = entityManager.SpawnEntity("MaleDummy", new MapCoordinates(0, 1, map.MapId));

        var maleSolutionComponent = entityManager.GetComponent<SolutionContainerManagerComponent>(maleUid);
        var femaleSolutionComponent = entityManager.GetComponent<SolutionContainerManagerComponent>(femaleUid);

        interactionSystem.ProcessInteraction(entityManager.GetNetEntity(maleUid),
            entityManager.GetNetEntity(femaleUid),
            new InteractionPrototype()
            {
                AmountLactate = 5,
                Category = "грудь",
                Coefficient = 0.2f,
                Emotes = new HashSet<string>(),
                Erp = true,
                Icon = new SpriteSpecifier.Texture(new ResPath("_Sunrise/Interface/ERP/boobs_suck.png")),
                InhandObject = new HashSet<string>(),
                LactationStimulationFlag = true,
                LovePercentTarget = 5,
                LovePercentUser = 0,
                Name = "test1",
                UseSelf = false,
            });
        // Первый тест. Мужик высасывает из женщины.
        {
            if (!solutionContainerSystem.TryGetSolution((maleUid, maleSolutionComponent),
                    "chemicals",
                    out var maleSolutionEntity,
                    out var maleSolution) ||
                !solutionContainerSystem.TryGetSolution((femaleUid, femaleSolutionComponent),
                    "bloodstream",
                    out var femaleSolutionEntity,
                    out var femaleSolution))
            {
                return;
            }

            Assert.That(maleSolution[new ReagentId("Milk", null)] == new ReagentQuantity("Milk", 5),
                "Нет молока в мужике");
            // Assert.That(femaleSolution[new ReagentId("blood", null)]);
        }
    }
}
