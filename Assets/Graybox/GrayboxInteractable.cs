using UnityEngine;

namespace Luvsic.Graybox
{
    public enum GrayboxInteraction
    {
        Warehouse,
        Departure,
        Chest,
        Loot,
        Extraction,
        Merchant
    }

    public sealed class GrayboxInteractable : MonoBehaviour
    {
        [SerializeField] private GrayboxInteraction kind;
        [SerializeField] private DemoItem item;

        private bool dropped;

        public GrayboxInteraction Kind => kind;
        public DemoItem Item => item;

        public void Collect()
        {
            gameObject.SetActive(false);
            if (dropped)
                Destroy(gameObject);
        }

        public static void SpawnDrop(DemoItem item, Vector3 position, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Dropped " + item;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.28f;
            go.GetComponent<Renderer>().sharedMaterial = material;

            var interaction = go.AddComponent<GrayboxInteractable>();
            interaction.kind = GrayboxInteraction.Loot;
            interaction.item = item;
            interaction.dropped = true;
        }
    }
}
