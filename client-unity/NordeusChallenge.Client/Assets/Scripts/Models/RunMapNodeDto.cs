using System;
using System.Collections.Generic;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class RunMapNodeDto
    {
        public string id;
        public int depth;
        public int position;

        // One of: "Battle", "Elite", "Shop", "Boss".
        public string type;

        // Index into RunConfigResponseDto.encounters for Battle/Elite/Boss.
        // -1 for Shop nodes (JsonUtility cannot serialize nullable ints cleanly).
        public int encounterIndex = -1;

        public List<string> connectedTo;
    }
}
