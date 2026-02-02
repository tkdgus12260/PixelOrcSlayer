using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PixelSurvival
{
    public class NetworkController : SingletonBehaviour<NetworkController>
    {
        private const string TIME_URL = "https://worldtimeapi.org/api/ip";
        
        private struct TimeDataWrapper
        {
            public string datetime;
        }

        public async Task<DateTime> GetCurrentDateTime()
        {
            // using -> 작업 수행 후 request 객체는 메모리에서 자동 삭제.
            using (UnityWebRequest request = UnityWebRequest.Get(TIME_URL))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Logger.LogError($"GetCurrentDateTime error: {request.error}");
                    return DateTime.MinValue;
                }

                TimeDataWrapper timeData = JsonUtility.FromJson<TimeDataWrapper>(request.downloadHandler.text);
                var currentDateTime = ParseDateTime(timeData.datetime);
                Logger.Log($"Current DateTime: {currentDateTime}");
                return currentDateTime;
            }
        }

        private DateTime ParseDateTime(string dateTimeString)
        {
            string date = Regex.Match(dateTimeString, @"^\d{4}-\d{2}-\d{2}").Value;
            string time = Regex.Match(dateTimeString, @"\d{2}:\d{2}:\d{2}").Value;
            return DateTime.Parse($"{date} {time}");
        }
    }   
}
