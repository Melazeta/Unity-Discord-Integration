using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;
using UnityEngine;
using UnityEngine.Networking;


namespace MZ.Rest
{
    public class Postman : MonoBehaviour
    {
        public static string AddQuery(string url, Dictionary<string, string> query)
        {
            if (url.Substring(url.Length - 1, 1) == "/")
                url = url.Substring(0, url.Length - 1);

            StringBuilder queryBuilder = new StringBuilder("?");

            foreach (var entry in query)
                queryBuilder.Append($"&{entry.Key}={entry.Value}");

            return url + queryBuilder.ToString();
        }

        // N.B. se pensi di aggiungere un json alle chiamate get:
        // 1) sei un pagliaccio
        // 2) ios comunque non te le fa inviare
        public Coroutine Get<T>(string uri, Dictionary<string, string> headers = null, Action<Response<T>> callback = null)
        {
            return StartCoroutine(
                SendRequest(uri, UnityWebRequest.kHttpVerbGET, headers: headers, callback: callback)
            );
        }

        public Coroutine Post<T>(string uri, Dictionary<string, string> headers = null, object json = null, Action<Response<T>> callback = null)
        {
            return StartCoroutine(
                SendRequest(
                    uri,
                    UnityWebRequest.kHttpVerbPOST,
                    headers: headers,
                    json: json != null ? JsonConvert.SerializeObject(json) : null,
                    callback: callback
                )
            );
        }

        public Coroutine PostForm<T>(string uri, List<IMultipartFormSection> form, Dictionary<string, string> headers = null, object json = null, Action<Response<T>> callback = null)
        {
            return StartCoroutine(
                SendRequest(
                    UnityWebRequest.Post(uri, form),
                    headers,
                    json: json != null ? JsonConvert.SerializeObject(json) : null,
                    callback: callback
                )
            );
        }

        public Coroutine Put<T>(string uri, Dictionary<string, string> headers = null, object json = null, Action<Response<T>> callback = null)
        {
            return StartCoroutine(
                SendRequest(
                    uri,
                    UnityWebRequest.kHttpVerbPUT,
                    headers: headers,
                    json: json != null ? JsonConvert.SerializeObject(json) : null,
                    callback: callback
                )
            );
        }

        public Coroutine Patch<T>(string uri, Dictionary<string, string> headers = null, object json = null, Action<Response<T>> callback = null)
        {
            return StartCoroutine(
                SendRequest(
                    uri,
                    "PATCH",
                    headers: headers,
                    json: json != null ? JsonConvert.SerializeObject(json) : null,
                    callback: callback
                )
            );
        }

        public Coroutine Delete<T>(string uri, Dictionary<string, string> headers = null, object json = null, Action<Response<T>> callback = null)
        {
            return StartCoroutine(
                SendRequest(
                    uri,
                    UnityWebRequest.kHttpVerbDELETE,
                    headers: headers,
                    json: json != null ? JsonConvert.SerializeObject(json) : null,
                    callback: callback
                )
            );
        }

        // N.B. se capita che unity risolve un url all'ip sbagliato chiamare questo metodo
        // https://stackoverflow.com/questions/7277582/how-do-i-clear-system-net-client-dns-cache/47707658#47707658
        //[DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        //static extern UInt32 DnsFlushResolverCache();

        public IEnumerator SendRequest<T>(string uri, string verb, Dictionary<string, string> headers, string json = null, Action<Response<T>> callback = null)
        {
            return SendRequest(new UnityWebRequest(uri, method: verb), headers, json, callback);
        }

        public IEnumerator SendRequest<T>(UnityWebRequest request, Dictionary<string, string> headers, string json = null, Action<Response<T>> callback = null)
        {
            //try
            //{
            //    // N.B. se capita che unity risolve un url all'ip sbagliato
            //    DnsFlushResolverCache();
            //}
            //catch (Exception ex)
            //{

            //}

            using (request)
            {
                request.SetRequestHeader("Accept", "application/json");
                request.useHttpContinue = false;
                request.downloadHandler = new DownloadHandlerBuffer();

                if (headers?.Count > 0)
                {
                    foreach (var entry in headers)
                        request.SetRequestHeader(entry.Key, entry.Value);
                }

                if (json != null)
                {
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.uploadHandler.contentType = "application/json";
                }

                yield return request.SendWebRequest();

                Response<T> response = new Response<T>();
                response.request = request;
                response.uri = request.url;
                response.exception = request.error != null ? new WebException(request.error) : null;
#if UNITY_2020_1_OR_NEWER
                response.isNetworkError = request.result == UnityWebRequest.Result.ConnectionError;
#else
                response.isNetworkError = request.isNetworkError;
#endif
                response.statusCode = request.responseCode;
                response.rawData = request.downloadHandler.text;

#if UNITY_2020_1_OR_NEWER
                bool isHttpError = request.result == UnityWebRequest.Result.ProtocolError;
#else
                bool isHttpError = request.isHttpError;
#endif

                if (!response.isNetworkError && !isHttpError && !string.IsNullOrEmpty(response.rawData))
                {
                    try
                    {
                        response.data = JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
                    }
                    catch (JsonReaderException ex)
                    {
                        response.exception = ex;
                    }
                    catch (ArgumentException ex)
                    {
                        response.exception = ex;
                    }
                    catch (Exception ex)
                    {
                        response.exception = ex;
                    }
                }

                callback?.Invoke(response);
            }
        }
    }
}
