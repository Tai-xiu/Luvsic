using System;
using UnityEngine;

namespace Luvsic.Graybox
{
    public enum DemoItem
    {
        Scrap,
        Potion,
        Sword,
        Armor
    }

    [CreateAssetMenu(menuName = "Luvsic/Item Catalog")]
    public sealed class DemoItemCatalog : ScriptableObject
    {
        [Serializable]
        public struct Definition
        {
            public string name;
            public int buyPrice;
            public int sellPrice;
            [Min(1)] public int stackSize;
        }

        public static readonly DemoItem[] Items = (DemoItem[])Enum.GetValues(typeof(DemoItem));

        [SerializeField] private Definition[] definitions =
        {
            new Definition { name = "粗铁废料", buyPrice = -1, sellPrice = 4, stackSize = 1 },
            new Definition { name = "小瓶药水", buyPrice = 12, sellPrice = 6, stackSize = 3 },
            new Definition { name = "铁短剑", buyPrice = 55, sellPrice = 27, stackSize = 1 },
            new Definition { name = "皮甲", buyPrice = 34, sellPrice = 17, stackSize = 1 }
        };

        public Definition this[DemoItem id] => definitions[(int)id];
    }
}
