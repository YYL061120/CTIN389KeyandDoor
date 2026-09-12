using UnityEngine;

/// <summary>
/// Identifies a completed burger to gameplay systems. Add this to every burger
/// prefab produced by the microwave once that system is implemented.
/// </summary>
public class BurgerMarker : MonoBehaviour
{
    [SerializeField] private bool consumableByMonster = true;

    public bool ConsumableByMonster => consumableByMonster;

    public void SetConsumableByMonster(bool consumable)
    {
        consumableByMonster = consumable;
    }
}
