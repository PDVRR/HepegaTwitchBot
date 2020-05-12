using System.Collections.Generic;

namespace HepegaTwitchBot
{
    public static class HpgItems
    {
        public static List<string> Items { get; set; }
        private static HpgDocParser hpgDoc;
        static HpgItems()
        {
            hpgDoc = new HpgDocParser();
            Items = hpgDoc.GetAllItems();
        }
    }
}