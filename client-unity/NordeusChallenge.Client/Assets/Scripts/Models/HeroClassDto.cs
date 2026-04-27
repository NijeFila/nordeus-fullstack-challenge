using System;
using System.Collections.Generic;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class HeroClassDto
    {
        public string id;
        public string name;
        public string description;
        public StatsDto startingStats;
        public List<string> startingMoves;
        public List<string> startingLearnedMoves;
    }
}
