using System;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class MoveEffectDto
    {
        // One of:
        //   BuffAttack, BuffDefense, BuffMagic,
        //   DebuffAttack, DebuffDefense, DebuffMagic,
        //   Heal,
        //   Bleed, Poison,           // damage-over-time on target
        //   DamageIncrease,          // flat bonus to outgoing damage
        //   DamageReduction.         // flat reduction to incoming damage
        public string kind;
        public int amount;
        public int durationTurns;
        public string target;
    }
}
