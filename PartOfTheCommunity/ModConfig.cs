using System;

namespace SB_PotC
{

    public class ModConfig
    {
        public int witnessBonus { get; set; }
        public int storytellerBonus { get; set; }
        public int ujamaaBonus { get; set; }
        public int umojaBonusFestival { get; set; }
        public int umojaBonusMarry { get; set; }
        public int umojaBonus { get; set; }
        public int ujimaBonusStore { get; set; }
        public int ujimaBonus { get; set; }
        public int kuumbaBonus { get; set; }
        public bool hasGottenInitialUjimaBonus { get; set; }
        public bool hasGottenInitialKuumbaBonus { get; set; }

        public ModConfig()
        {
            witnessBonus = 2;
            storytellerBonus = 4;
            ujamaaBonus = 4;
            umojaBonus = 10;
            umojaBonusFestival = 16;
            umojaBonusMarry = 240;
            ujimaBonusStore = 20;
            ujimaBonus = 2;
            kuumbaBonus = 2;
            hasGottenInitialUjimaBonus = false;
            hasGottenInitialKuumbaBonus = false;
        }
    }
}
