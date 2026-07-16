using NUnit.Framework;
using UnityEngine;
namespace CanopyKin.Tests { public sealed class ModelTests { [Test] public void ColonyUpgradeConsumesResources(){var g=new GameObject();var c=g.AddComponent<ColonyState>();c.Add(ResourceKind.Seed,5);c.Add(ResourceKind.Resin,2);Assert.True(c.Upgrade());Assert.AreEqual(2,c.Level);Assert.AreEqual(0,c.Seeds);Object.DestroyImmediate(g);} [Test] public void UpgradeRequiresResources(){var g=new GameObject();var c=g.AddComponent<ColonyState>();Assert.False(c.Upgrade());Object.DestroyImmediate(g);} } }
