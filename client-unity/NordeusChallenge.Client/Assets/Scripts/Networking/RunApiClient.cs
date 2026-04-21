using System;
using System.Collections;
using NordeusChallenge.Client.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace NordeusChallenge.Client.Networking
{
    public class RunApiClient
    {
        private readonly string _baseUrl;

        public RunApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public IEnumerator GetRunConfig(Action<RunConfigResponseDto> onSuccess, Action<string> onError)
        {
            string url = _baseUrl + "/run/config";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Request failed: {request.error}");
                    yield break;
                }

                string json = request.downloadHandler.text;

                RunConfigResponseDto parsed;
                try
                {
                    parsed = JsonUtility.FromJson<RunConfigResponseDto>(json);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Failed to parse response: {ex.Message}");
                    yield break;
                }

                if (parsed == null)
                {
                    onError?.Invoke("Empty response from server.");
                    yield break;
                }

                onSuccess?.Invoke(parsed);
            }
        }
    }
}
