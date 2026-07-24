using NUnit.Framework;
using UnityEngine;

namespace CanopyKin.Tests
{
    public sealed class ModelTests
    {
        [Test]
        public void ColonyUpgradeConsumesVerticalSliceResources()
        {
            var gameObject = new GameObject();
            ColonyState colony = gameObject.AddComponent<ColonyState>();
            colony.Add(ResourceKind.Seed, ColonyState.UpgradeSeedCost);
            colony.Add(ResourceKind.Resin, ColonyState.UpgradeResinCost);
            Assert.True(colony.Upgrade());
            Assert.AreEqual(2, colony.Level);
            Assert.AreEqual(0, colony.Seeds);
            Assert.AreEqual(0, colony.Resin);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void UpgradeRequiresResources()
        {
            var gameObject = new GameObject();
            ColonyState colony = gameObject.AddComponent<ColonyState>();
            Assert.False(colony.Upgrade());
            Assert.AreEqual(1, colony.Level);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void GroundHeightKeepsNestApproachLevel()
        {
            Assert.That(Mathf.Abs(WorldBootstrap.GroundHeight(0, -5)), Is.LessThan(.001f));
            Assert.That(Mathf.Abs(WorldBootstrap.GroundHeight(0, -1.5f)), Is.LessThan(.15f));
        }
    }
}
