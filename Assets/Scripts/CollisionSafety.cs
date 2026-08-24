using UnityEngine;

namespace CanopyKin
{
    /// <summary>Marker used by runtime audits and collision filtering.</summary>
    [DisallowMultipleComponent]
    public sealed class SolidWorldGeometry : MonoBehaviour
    {
    }

    /// <summary>
    /// Swept, non-allocating movement for non-Rigidbody actors. It prevents the
    /// squad and creatures from being teleported through world geometry by a
    /// direct transform assignment and supplies stable wall sliding.
    /// </summary>
    public static class CollisionSafety
    {
        static readonly RaycastHit[] Hits = new RaycastHit[24];

        public static Vector3 MoveSphere(
            Transform actor,
            Collider self,
            Vector3 direction,
            float distance,
            float centerHeight,
            float radius)
        {
            if (!actor || distance <= .00001f || direction.sqrMagnitude <= .00001f)
                return Vector3.zero;

            direction.Normalize();
            float maximumStep = Mathf.Max(.045f, radius * .7f);
            int steps = Mathf.Clamp(Mathf.CeilToInt(distance / maximumStep), 1, 12);
            float stepDistance = distance / steps;
            Vector3 start = actor.position;
            for (int step = 0; step < steps; step++)
            {
                Vector3 origin = actor.position + Vector3.up * centerHeight;
                Vector3 displacement = direction * stepDistance;
                if (Cast(origin, radius, displacement, actor, self, out RaycastHit hit))
                {
                    float travel = Mathf.Max(0, hit.distance - .012f);
                    if (travel > .0001f)
                        actor.position += displacement.normalized * travel;

                    Vector3 remainder = displacement.normalized *
                                        Mathf.Max(0, displacement.magnitude - travel);
                    Vector3 slide = Vector3.ProjectOnPlane(remainder, hit.normal);
                    if (slide.sqrMagnitude > .00001f)
                    {
                        bool slideBlocked = Cast(
                            actor.position + Vector3.up * centerHeight,
                            radius,
                            slide,
                            actor,
                            self,
                            out RaycastHit slideHit);
                        if (!slideBlocked)
                            actor.position += slide;
                        else if (slideHit.distance > .012f)
                            actor.position += slide.normalized * (slideHit.distance - .012f);
                    }
                }
                else
                    actor.position += displacement;
            }
            return actor.position - start;
        }

        static bool Cast(
            Vector3 origin,
            float radius,
            Vector3 displacement,
            Transform actor,
            Collider self,
            out RaycastHit best)
        {
            best = default;
            float distance = displacement.magnitude;
            if (distance <= .00001f) return false;
            int count = Physics.SphereCastNonAlloc(
                origin,
                radius,
                displacement / distance,
                Hits,
                distance + .015f,
                ~0,
                QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = Hits[i];
                Collider collider = candidate.collider;
                if (!collider || collider == self || collider.isTrigger) continue;
                Transform candidateTransform = collider.transform;
                if (candidateTransform == actor || candidateTransform.IsChildOf(actor)) continue;
                if (candidate.distance >= nearest) continue;
                nearest = candidate.distance;
                best = candidate;
            }
            return best.collider != null;
        }
    }
}
