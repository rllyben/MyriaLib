namespace MyriaLib.Entities.Items
{
    public struct EquipmentBonuses
    {
        public int HP { get; set; }
        public int MP { get; set; }

        // Core stat bonuses
        public int STR { get; set; }
        public int DEX { get; set; }
        public int END { get; set; }
        public int INT { get; set; }
        public int SPR { get; set; }

        // Derived combat bonuses
        public int ATK { get; set; }
        public int DEF { get; set; }
        public int MATK { get; set; }
        public int MDEF { get; set; }
        public int Aim { get; set; }
        public int Evasion { get; set; }
        public float Crit { get; set; }
        public float Block { get; set; }

        // Returns a scaled copy — used when applying upgrade levels
        public EquipmentBonuses Scale(float multiplier) => new()
        {
            HP      = (int)(HP   * multiplier),
            MP      = (int)(MP   * multiplier),
            STR     = (int)(STR  * multiplier),
            DEX     = (int)(DEX  * multiplier),
            END     = (int)(END  * multiplier),
            INT     = (int)(INT  * multiplier),
            SPR     = (int)(SPR  * multiplier),
            ATK     = (int)(ATK  * multiplier),
            DEF     = (int)(DEF  * multiplier),
            MATK    = (int)(MATK * multiplier),
            MDEF    = (int)(MDEF * multiplier),
            Aim     = (int)(Aim  * multiplier),
            Evasion = (int)(Evasion * multiplier),
            Crit    = Crit  * multiplier,
            Block   = Block * multiplier,
        };
    }
}
