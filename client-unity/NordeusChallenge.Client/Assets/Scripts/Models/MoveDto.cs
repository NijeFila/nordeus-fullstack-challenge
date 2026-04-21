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
        public MoveEffectDto effect;
        public string description;
    }
}
