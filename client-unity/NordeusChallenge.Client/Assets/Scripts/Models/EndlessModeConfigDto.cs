using System;
using System.Collections.Generic;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class EndlessModeConfigDto
    {
        public bool enabled;
        public int startingFloor = 1;
        public int eliteEvery = 5;
        public int shopEvery = 3;
        public int bossEvery = 10;
        public int baseLevel = 1;
        public int levelIncreaseEvery = 2;
        public int rewardGoldBase = 15;
        public int rewardGoldPerFloor = 2;
        public int xpBase = 30;
        public int xpPerFloor = 3;

        // Multipliers in basis points (100 = +1.00x). 0 / negative = no scaling.
        public int endlessGoldScalingBp;
        public int endlessXpScalingBp;

        public List<string> monsterPool;
        public List<string> eliteMonsterPool;
        public List<string> bossMonsterPool;
        public List<string> environmentPool;
    }
}
