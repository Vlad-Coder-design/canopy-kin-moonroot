using NUnit.Framework;
using UnityEngine;

namespace CanopyKin.Tests
{
    public sealed class ModelTests
    {
        [Test]
        public void ColonyUpgradeConsumesAllConstructionResources()
        {
            var gameObject = new GameObject();
            ColonyState colony = gameObject.AddComponent<ColonyState>();
            colony.Add(ResourceKind.Seed, ColonyState.UpgradeSeedCost);
            colony.Add(ResourceKind.Resin, ColonyState.UpgradeResinCost);
            colony.Add(ResourceKind.Protein, ColonyState.UpgradeProteinCost);
            Assert.True(colony.Upgrade());
            Assert.AreEqual(2, colony.Level);
            Assert.AreEqual(0, colony.Seeds);
            Assert.AreEqual(0, colony.Resin);
            Assert.AreEqual(0, colony.Protein);
            Assert.Greater(colony.Capacity, 10);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void UpgradeRequiresEveryResourceType()
        {
            var gameObject = new GameObject();
            ColonyState colony = gameObject.AddComponent<ColonyState>();
            colony.Add(ResourceKind.Seed, ColonyState.UpgradeSeedCost);
            colony.Add(ResourceKind.Resin, ColonyState.UpgradeResinCost);
            Assert.False(colony.Upgrade());
            Assert.AreEqual(1, colony.Level);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void GroundHeightKeepsNestApproachWalkableButRegionHasRelief()
        {
            Assert.That(Mathf.Abs(WorldBootstrap.GroundHeight(0, -7)), Is.LessThan(.001f));
            Assert.That(Mathf.Abs(WorldBootstrap.GroundHeight(0, -3f)), Is.LessThan(.45f));
            float difference = Mathf.Abs(
                WorldBootstrap.GroundHeight(-18f, 14f) -
                WorldBootstrap.GroundHeight(17f, 19f));
            Assert.That(difference, Is.GreaterThan(.35f));
        }

        [Test]
        public void AntRolesHaveDistinctGameplayDefinitions()
        {
            AntDefinition worker = GameDefinitions.Ant(UnitRole.Worker);
            AntDefinition light = GameDefinitions.Ant(UnitRole.LightSoldier);
            AntDefinition heavy = GameDefinitions.Ant(UnitRole.HeavySoldier);
            Assert.Greater(light.damage, worker.damage);
            Assert.Greater(heavy.maxHealth, light.maxHealth);
            Assert.Greater(worker.carryCapacity, 0);
            Assert.AreEqual(0, light.carryCapacity);
        }

        [Test]
        public void PredatorDefinitionsExposeReadableAttackTiming()
        {
            EnemyDefinition beetle = GameDefinitions.Enemy(Creature.Species.Beetle);
            EnemyDefinition spider = GameDefinitions.Enemy(Creature.Species.Spider);
            Assert.Greater(spider.maxHealth, beetle.maxHealth);
            Assert.Greater(spider.attackRange, beetle.attackRange);
            Assert.Greater(beetle.attackInterval, 0.4f);
        }

        [Test]
        public void OrganicAntMeshesAreNotUnityPrimitiveMeshes()
        {
            Mesh head = OrganicMeshFactory.Body(OrganicMeshFactory.BodyShape.Head);
            Mesh abdomen = OrganicMeshFactory.Body(OrganicMeshFactory.BodyShape.Abdomen);
            Assert.That(head.name, Does.Contain("Original"));
            Assert.AreNotSame(head, abdomen);
            Assert.Greater(head.vertexCount, 150);
            Assert.Greater(abdomen.bounds.size.z, abdomen.bounds.size.x);
        }

        [Test]
        public void BroadLeafGrassHasWindWeightsAndRealSurfaceArea()
        {
            Mesh grass = OrganicMeshFactory.BladeCluster(3);
            Assert.Greater(grass.vertexCount, 70);
            Assert.AreEqual(grass.vertexCount, grass.colors.Length);
            Assert.Greater(grass.bounds.size.x, .2f);
            Assert.Greater(grass.bounds.size.y, .7f);
        }
    }
}
