using System;
using System.Collections.Generic;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class MonsterDto
    {
        public string id;
        public string name;
        public StatsDto baseStats;
        public List<string> moves;
    }
}
