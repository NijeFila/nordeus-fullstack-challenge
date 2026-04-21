using System;
using System.Collections;
using System.Globalization;
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
            Action<string> onSuccess,
            Action<string> onError)
        {
            string url = string.Format(
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
    }
}
