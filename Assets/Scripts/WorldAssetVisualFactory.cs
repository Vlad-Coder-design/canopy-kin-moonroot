using UnityEngine;

namespace CanopyKin
{
    public static class WorldAssetVisualFactory
    {
        public static GameObject Cargo(
            Transform parent,
            ResourceKind kind,
            Vector3 localPosition,
            float scale,
            int variant = 0)
        {
            var root = new GameObject($"Detailed {kind.ToString().ToLowerInvariant()} cargo");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localScale = Vector3.one * scale;

            switch (kind)
            {
                case ResourceKind.Seed:
                    CreateSeed(root.transform, variant);
                    break;
                case ResourceKind.Resin:
                    CreateResin(root.transform, variant);
                    break;
                default:
                    CreateProtein(root.transform, variant);
                    break;
            }
            return root;
        }

        public static GameObject Brood(
            Transform parent,
            BroodStage stage,
            Vector3 localPosition,
            float scale,
            int variant)
        {
            var root = new GameObject($"Detailed ant {stage.ToString().ToLowerInvariant()}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localScale = Vector3.one * scale;
            root.transform.localRotation = Quaternion.Euler(
                stage == BroodStage.Larva ? -8f : 4f,
                variant * 61f,
                (variant % 5 - 2) * 4f);

            Color warmIvory = stage switch
            {
                BroodStage.Egg => new Color(.68f, .61f, .43f),
                BroodStage.Larva => new Color(.58f, .47f, .31f),
                _ => new Color(.47f, .38f, .27f)
            };
            Mesh mesh = stage switch
            {
                BroodStage.Egg => WorldAssetMeshFactory.Egg(variant),
                BroodStage.Larva => WorldAssetMeshFactory.Larva(variant),
                _ => WorldAssetMeshFactory.Pupa(variant)
            };
            VisualFactory.MeshObject(
                stage == BroodStage.Egg ? "Pearlescent egg shell" :
                stage == BroodStage.Larva ? "Segmented larval body" : "Fibrous pupa with folded limbs",
                root.transform,
                mesh,
                Vector3.zero,
                Vector3.one,
                VisualFactory.Material(warmIvory, stage == BroodStage.Egg ? .38f : .09f));

            if (stage == BroodStage.Egg)
            {
                for (int i = 0; i < 2; i++)
                    VisualFactory.MeshObject(
                        "Smaller clustered egg",
                        root.transform,
                        WorldAssetMeshFactory.Egg(variant + i + 1),
                        new Vector3((i == 0 ? -.34f : .31f), -.05f, (i == 0 ? -.2f : .18f)),
                        Vector3.one * (.7f + i * .08f),
                        VisualFactory.Material(Color.Lerp(warmIvory, Color.white, .12f), .4f));
            }
            else if (stage == BroodStage.Larva)
            {
                VisualFactory.MeshObject(
                    "Larval head capsule",
                    root.transform,
                    OrganicMeshFactory.Body(OrganicMeshFactory.BodyShape.Head),
                    new Vector3(0, .04f, .57f),
                    new Vector3(.22f, .18f, .2f),
                    VisualFactory.Material(new Color(.34f, .22f, .12f), .18f));
            }
            return root;
        }

        public static GameObject ChamberBerm(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            int variant,
            bool collider = true)
        {
            GameObject berm = VisualFactory.MeshObject(
                name,
                parent,
                WorldAssetMeshFactory.ChamberBerm(variant),
                localPosition,
                localScale,
                VisualFactory.NestSoilMaterial(),
                collider);
            berm.transform.localRotation = Quaternion.Euler(0, variant * 37f, 0);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i / 5f * Mathf.PI * 2f) + variant * .41f;
                float radius = Mathf.Lerp(1.48f, 2.02f, (i % 3) / 2f);
                VisualFactory.MeshObject(
                    "Packed soil clod",
                    berm.transform,
                    WorldAssetMeshFactory.SoilClod(variant * 11 + i),
                    new Vector3(Mathf.Cos(angle) * radius, .035f + (i % 2) * .045f, Mathf.Sin(angle) * radius),
                    new Vector3(.26f + i % 3 * .055f, .16f, .24f + i % 2 * .05f),
                    VisualFactory.NestSoilMaterial());
            }
            return berm;
        }

        static void CreateSeed(Transform parent, int variant)
        {
            Color coat = Color.Lerp(new Color(.32f, .13f, .025f), new Color(.58f, .33f, .08f), (variant % 4) / 3f);
            VisualFactory.MeshObject("Ridged seed coat", parent, WorldAssetMeshFactory.Seed(variant),
                Vector3.zero, Vector3.one, VisualFactory.Material(coat, .16f));
            VisualFactory.MeshObject("Seed hilum", parent, OrganicMeshFactory.Body(OrganicMeshFactory.BodyShape.Eye),
                new Vector3(0, .09f, .3f), new Vector3(.13f, .07f, .09f),
                VisualFactory.Material(new Color(.12f, .055f, .018f), .08f));
            Vector3[] awnPath =
            {
                new(0, 0, -.64f),
                new(.02f, .03f, -.88f),
                new(.08f + variant * .01f, .05f, -1.1f)
            };
            VisualFactory.MeshObject("Dry seed awn", parent, OrganicMeshFactory.Tube(awnPath,
                    new[] { .025f, .017f, .006f }, 7), Vector3.zero, Vector3.one,
                VisualFactory.Material(new Color(.37f, .22f, .08f), .04f));
        }

        static void CreateResin(Transform parent, int variant)
        {
            VisualFactory.MeshObject("Fused translucent amber drops", parent,
                WorldAssetMeshFactory.Resin(variant), Vector3.zero, Vector3.one,
                VisualFactory.Material(new Color(.62f, .15f, .018f), .58f));
            for (int i = 0; i < 3; i++)
                VisualFactory.MeshObject("Entrained resin bubble", parent,
                    OrganicMeshFactory.Body(OrganicMeshFactory.BodyShape.Eye),
                    new Vector3(-.12f + i * .13f, .15f - i * .035f, .06f - i * .09f),
                    Vector3.one * (.035f + i * .008f),
                    VisualFactory.Material(new Color(.86f, .46f, .09f), .78f));
        }

        static void CreateProtein(Transform parent, int variant)
        {
            VisualFactory.MeshObject("Broken chitin plate", parent,
                WorldAssetMeshFactory.Protein(variant), Vector3.zero, Vector3.one,
                VisualFactory.Material(new Color(.3f, .075f, .018f), .24f));
            Vector3[] leg =
            {
                new(-.28f, .03f, -.08f),
                new(-.52f, .08f, .12f),
                new(-.69f, .03f, .31f)
            };
            VisualFactory.MeshObject("Insect leg fragment", parent,
                OrganicMeshFactory.Tube(leg, new[] { .05f, .036f, .014f }, 8),
                Vector3.zero, Vector3.one,
                VisualFactory.Material(new Color(.16f, .04f, .012f), .2f));
        }
    }
}
