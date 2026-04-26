using System;
using System.Collections.Generic;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class RunConfigResponseDto
    {
        public string runId;
        public HeroDto hero;
        public List<EncounterDto> encounters;
        public List<MonsterDto> monsters;
        public List<MoveDto> moves;
        public List<BattleEnvironmentDto> environments;
        public RulesConfigDto rules;
    }
}
