using UnityEngine;

namespace Luvsic.Graybox
{
    public enum GrayboxInteraction
    {
        Warehouse,
        Departure,
        Chest,
        Loot,
        Extraction
    }

    public sealed class GrayboxInteractable : MonoBehaviour
    {
        [SerializeField] private GrayboxInteraction kind;

        public GrayboxInteraction Kind => kind;
    }
}
