using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace MZ.Rest
{
    [Serializable]
    public class Response
    {
        public UnityWebRequest request;

        public string uri;
        public Exception exception;
        public bool isNetworkError;
        public long statusCode;

        public string rawData;

        public bool IsSuccessful()
        {
            return exception == null && !isNetworkError && IsStatusCodeOk();
        }

        public bool IsStatusCodeOk()
        {
            return statusCode >= 200 && statusCode < 300;
        }

        public TData ParseData<TData>() where TData : class
        {
            try
            {
                return JsonConvert.DeserializeObject<TData>(rawData);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public override string ToString()
        {
            return ToString(includeRequest: false);
        }

        public string ToString(bool includeRequest, bool includeUri = true, bool includeResponse = true, IEnumerable<string> requestHeaders = null)
        {
            string includedUri = includeUri ? $"{uri} " : string.Empty;

            string s = $"{includedUri}response {statusCode}";

            if (!IsSuccessful())
            {
                if (exception != null)
                    s += $" {exception}";

                if (isNetworkError)
                    s += $" network error";
            }

            bool hasResponse = includeResponse && !isNetworkError;

            if (hasResponse)
                s += $":\n{rawData}";

            if (includeRequest)
            {
                string req = RequestToCurl(requestHeaders);

                if (hasResponse)
                    s += $"\n\n----\n\n{req}";
                else
                    s += $":\n{req}";
            }

            return s;
        }

        public string RequestToCurl(IEnumerable<string> requestHeaders = null)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"curl {uri} -X {request?.method ?? "MOCK"}");

            IEnumerable<string> headers = EnumerateCommonHeaders();

            if (requestHeaders != null)
                headers = headers.Concat(requestHeaders);

            if (request != null)
            {
                foreach (string header in headers)
                {
                    string value = request.GetRequestHeader(header);
                    if (!string.IsNullOrEmpty(value))
                        builder.Append($" -H '{header}: {value}'");
                }
            }

            builder.Append($" -d '{(request?.uploadHandler?.data != null ? Encoding.UTF8.GetString(request.uploadHandler.data) : "")}'");

            return builder.ToString();
        }

        public static IEnumerable<string> EnumerateCommonHeaders()
        {
            yield return "Content-type";
            yield return "X-AUTH-TOKEN";
        }
    }

    [Serializable]
    public class Response<T> : Response
    {
        public T data;
    }
}