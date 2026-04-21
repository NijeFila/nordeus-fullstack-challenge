using System;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class MoveEffectDto
    {
        public string kind;
        public int amount;
        public int durationTurns;
        public string target;
    }
}
