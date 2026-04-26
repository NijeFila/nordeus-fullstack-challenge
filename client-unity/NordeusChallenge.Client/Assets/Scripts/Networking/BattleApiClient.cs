using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NordeusChallenge.Client.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace NordeusChallenge.Client.Networking
{
    public class BattleApiClient
    {
        private readonly string _baseUrl;

        public BattleApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public IEnumerator GetNextMove(
            string monsterId,
            int monsterLevel,
            int monsterHealth,
            int monsterMaxHealth,
            int heroHealth,
            int heroMaxHealth,
            int turn,
            int monsterMana,
            IList<string> monsterEffectKinds,
            IList<string> heroEffectKinds,
            Action<string> onSuccess,
            Action<string> onError)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0}/battle/next-move?monsterId={1}&monsterLevel={2}&monsterHealth={3}&monsterMaxHealth={4}&heroHealth={5}&heroMaxHealth={6}&turn={7}",
                _baseUrl,
                UnityWebRequest.EscapeURL(monsterId),
                monsterLevel,
                monsterHealth,
                monsterMaxHealth,
                heroHealth,
                heroMaxHealth,
                turn);

            sb.AppendFormat(CultureInfo.InvariantCulture, "&monsterMana={0}", monsterMana);

            string monsterEffects = JoinKinds(monsterEffectKinds);
            if (!string.IsNullOrEmpty(monsterEffects))
            {
                sb.Append("&monsterEffects=").Append(UnityWebRequest.EscapeURL(monsterEffects));
            }

            string heroEffects = JoinKinds(heroEffectKinds);
            if (!string.IsNullOrEmpty(heroEffects))
            {
                sb.Append("&heroEffects=").Append(UnityWebRequest.EscapeURL(heroEffects));
            }

            string url = sb.ToString();

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Request failed: {request.error}");
                    yield break;
                }

                NextMoveResponseDto parsed;
                try
                {
                    parsed = JsonUtility.FromJson<NextMoveResponseDto>(request.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Failed to parse response: {ex.Message}");
                    yield break;
                }

                if (parsed == null || string.IsNullOrEmpty(parsed.moveId))
                {
                    onError?.Invoke("Empty move id.");
                    yield break;
                }

                onSuccess?.Invoke(parsed.moveId);
            }
        }

        private static string JoinKinds(IList<string> kinds)
        {
            if (kinds == null || kinds.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < kinds.Count; i++)
            {
                string kind = kinds[i];
                if (string.IsNullOrEmpty(kind)) continue;
                if (sb.Length > 0) sb.Append(',');
                sb.Append(kind);
            }
            return sb.ToString();
        }
    }
}
