using UnityEngine;

namespace CanopyKin
{
    public sealed class AntVisual : MonoBehaviour
    {
        readonly Transform[] legRoots = new Transform[6];
        readonly Quaternion[] legRest = new Quaternion[6];
        Vector3 previousPosition;
        float stride;
        bool built;

        public static AntVisual Create(Transform parent, Color shell, float scale = 1f)
        {
            var visualRoot = new GameObject("Animated ant").transform;
            visualRoot.SetParent(parent, false);
            visualRoot.localScale = Vector3.one * scale;
            var visual = visualRoot.gameObject.AddComponent<AntVisual>();
            visual.Build(shell);
            return visual;
        }

        void Build(Color shell)
        {
            if (built) return;
            built = true;
            Color dark = Color.Lerp(shell, Color.black, .48f);
            Color eye = new(.015f, .012f, .008f);

            VisualFactory.Primitive(PrimitiveType.Sphere, "Abdomen", transform, new Vector3(0, .34f, -.33f), new Vector3(.34f, .25f, .48f), shell, false, .48f);
            VisualFactory.Primitive(PrimitiveType.Sphere, "Thorax", transform, new Vector3(0, .32f, .12f), new Vector3(.28f, .23f, .31f), Color.Lerp(shell, dark, .16f), false, .42f);
            VisualFactory.Primitive(PrimitiveType.Sphere, "Head", transform, new Vector3(0, .34f, .48f), new Vector3(.27f, .21f, .26f), dark, false, .4f);

            VisualFactory.Primitive(PrimitiveType.Sphere, "Left eye", transform, new Vector3(-.205f, .41f, .59f), Vector3.one * .055f, eye, false, .7f);
            VisualFactory.Primitive(PrimitiveType.Sphere, "Right eye", transform, new Vector3(.205f, .41f, .59f), Vector3.one * .055f, eye, false, .7f);
            VisualFactory.Segment("Left antenna", transform, new Vector3(-.12f, .47f, .65f), new Vector3(-.28f, .64f, .92f), .018f, dark);
            VisualFactory.Segment("Right antenna", transform, new Vector3(.12f, .47f, .65f), new Vector3(.28f, .64f, .92f), .018f, dark);
            VisualFactory.Segment("Left mandible", transform, new Vector3(-.1f, .28f, .66f), new Vector3(-.2f, .22f, .84f), .035f, dark);
            VisualFactory.Segment("Right mandible", transform, new Vector3(.1f, .28f, .66f), new Vector3(.2f, .22f, .84f), .035f, dark);

            float[] z = { .28f, .07f, -.16f };
            for (int pair = 0; pair < 3; pair++)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    int index = pair * 2 + sideIndex;
                    Transform root = new GameObject($"Leg {index + 1}").transform;
                    root.SetParent(transform, false);
                    root.localPosition = new Vector3(side * .16f, .29f, z[pair]);
                    float forward = (1 - pair) * .16f;
                    Vector3 knee = new(side * .37f, -.07f, forward);
                    Vector3 foot = new(side * .62f, -.28f, forward + (pair - 1) * .1f);
                    VisualFactory.Segment("Upper leg", root, Vector3.zero, knee, .025f, dark);
                    VisualFactory.Segment("Lower leg", root, knee, foot, .019f, dark);
                    legRoots[index] = root;
                    legRest[index] = root.localRotation;
                }
            }
            previousPosition = transform.position;
        }

        void Update()
        {
            if (!built) return;
            float delta = Vector3.Distance(transform.position, previousPosition);
            previousPosition = transform.position;
            float targetStride = Time.deltaTime > 0 ? Mathf.Clamp01(delta / Time.deltaTime / 3f) : 0;
            stride = Mathf.Lerp(stride, targetStride, 10f * Time.deltaTime);
            for (int i = 0; i < legRoots.Length; i++)
            {
                float tripodPhase = (i == 0 || i == 3 || i == 4) ? 0 : Mathf.PI;
                float wave = Mathf.Sin(Time.time * 10f + tripodPhase) * 24f * stride;
                legRoots[i].localRotation = legRest[i] * Quaternion.Euler(0, wave, Mathf.Abs(wave) * (i % 2 == 0 ? -0.12f : .12f));
            }
            transform.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 10f)) * .018f * stride, 0);
        }
    }

    public static class CreatureVisuals
    {
        public static void BuildBeetle(Transform parent)
        {
            Color shell = new(.08f, .17f, .12f);
            Color wing = new(.17f, .28f, .18f);
            VisualFactory.Primitive(PrimitiveType.Sphere, "Beetle body", parent, new Vector3(0, .42f, 0), new Vector3(.52f, .32f, .72f), shell, false, .75f);
            VisualFactory.Primitive(PrimitiveType.Sphere, "Wing case left", parent, new Vector3(-.18f, .55f, -.08f), new Vector3(.28f, .12f, .58f), wing, false, .82f);
            VisualFactory.Primitive(PrimitiveType.Sphere, "Wing case right", parent, new Vector3(.18f, .55f, -.08f), new Vector3(.28f, .12f, .58f), Color.Lerp(wing, Color.black, .12f), false, .82f);
            VisualFactory.Primitive(PrimitiveType.Sphere, "Beetle head", parent, new Vector3(0, .38f, .64f), new Vector3(.4f, .28f, .35f), shell, false, .7f);
            BuildLegs(parent, 3, .33f, .72f, new Color(.04f, .08f, .05f));
        }

        public static void BuildSpider(Transform parent)
        {
            Color body = new(.11f, .045f, .025f);
            VisualFactory.Primitive(PrimitiveType.Sphere, "Ashback abdomen", parent, new Vector3(0, .62f, -.25f), new Vector3(.72f, .48f, .82f), body, false, .42f);
            VisualFactory.Primitive(PrimitiveType.Sphere, "Ash marking", parent, new Vector3(0, .98f, -.3f), new Vector3(.38f, .08f, .48f), new Color(.55f, .18f, .06f), false, .25f);
            VisualFactory.Primitive(PrimitiveType.Sphere, "Spider head", parent, new Vector3(0, .48f, .55f), new Vector3(.5f, .38f, .45f), Color.Lerp(body, Color.black, .2f), false, .35f);
            BuildLegs(parent, 4, .34f, 1.05f, new Color(.07f, .025f, .018f));
        }

        static void BuildLegs(Transform parent, int pairs, float y, float reach, Color color)
        {
            for (int pair = 0; pair < pairs; pair++)
            {
                float z = Mathf.Lerp(.42f, -.42f, pairs == 1 ? 0 : pair / (float)(pairs - 1));
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 hip = new(s * .22f, y, z);
                    Vector3 knee = new(s * reach * .58f, y + .08f, z + (pair - (pairs - 1) * .5f) * .1f);
                    Vector3 foot = new(s * reach, .04f, z + (pair - (pairs - 1) * .5f) * .22f);
                    VisualFactory.Segment("Insect leg upper", parent, hip, knee, .035f, color);
                    VisualFactory.Segment("Insect leg lower", parent, knee, foot, .026f, color);
                }
            }
        }
    }
}
