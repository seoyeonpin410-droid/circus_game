using UnityEngine;
namespace CircusPrototype
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CircusAreaTrigger : MonoBehaviour
    {
        public CircusCombatArea area;
        void OnTriggerEnter(Collider other){Enter(other);}
        void OnTriggerStay(Collider other){Enter(other);}
        void Enter(Collider other)
        {
            var player=other.GetComponentInParent<CircusPlayer>();
            if(player && area && area.State==CircusAreaState.NotStarted)FindFirstObjectByType<CircusSession>()?.TryEnterArea(area);
        }
    }
}
