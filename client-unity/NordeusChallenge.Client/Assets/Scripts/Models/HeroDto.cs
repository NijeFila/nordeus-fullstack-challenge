using System;
using System.Collections.Generic;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class HeroDto
    {
        public string id;
        public string name;
        public int level;
        public int xp;
        public StatsDto stats;
        public List<string> equippedMoves;
        public List<string> learnedMovePool;
    }
}
