using UnityEngine;
namespace CircusPrototype {
 public sealed class CircusImpact : MonoBehaviour {
  public static void Create(Vector3 position,Color color) { CircusCombatFx.Burst(position,color); }
 }
}
