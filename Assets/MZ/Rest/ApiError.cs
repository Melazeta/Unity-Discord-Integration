using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MZ.Rest
{
    [Serializable]
    public class ApiError
    {
        [JsonProperty("error")]
        private int? code = null;

        [JsonProperty("code")]
        private int? code2 = null;

        public string message;

        [JsonIgnore]
        public long statusCode;

        public int GetCode()
        {
            return code ?? code2 ?? 0;
        }

        public string GetFormattedCode()
        {
            if (code.HasValue)
                return $"{code}";
            else if (code2.HasValue)
                return $"c{code2}";
            else
                return "n42";
        }

        public string GetFullFormattedCode()
        {
            return $"{statusCode}-{GetFormattedCode()}";
        }

        public static ApiError FromResponse<T>(Response<T> response)
        {
            ApiError error = response.ParseData<ApiError>();

            if (error == null)
                error = new ApiError();

            error.statusCode = response.statusCode;

            return error;
        }

        public static string GetFullFormattedCode<T>(Response<T> response)
        {
            return FromResponse(response).GetFullFormattedCode();
        }
    }
}