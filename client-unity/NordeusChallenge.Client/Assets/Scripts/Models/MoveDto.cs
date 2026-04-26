using System;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class MoveDto
    {
        public string id;
        public string name;
        public string category;
        public int power;

        // Optional resource costs. The server omits these when 0/null,
        // and JsonUtility leaves them at 0, which is treated as "no cost".
        public int manaCost;
        public int hpCost;

        public MoveEffectDto effect;
        public string description;
    }
}
