using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using IFramework;

namespace AOT
{







    public static class AOTHelper
    {
        public class HttpPost : AsyncTask
        {
            private const int timeout = 10;
            private const int retryCount = 3;
            public HttpPost(string url, Dictionary<string, string> formFields)
            {
                this.formFields = formFields;
                this.url = url;
                Game.BindUpdate(Done);
            }
            private int _retry;
            private UnityWebRequest request;
            private Dictionary<string, string> formFields;
            private string url;

            public string text { get; private set; }
            public string error { get; private set; }
            public bool succeed { get; private set; }

            private void Done()
            {
                if (request == null)
                {
                    request = UnityWebRequest.PostWwwForm(url, string.Empty);
                    if (formFields != null)
                    {
                        foreach (var item in formFields)
                        {
                            request.SetRequestHeader(item.Key, item.Value);

                        }
                    }
                    request.timeout = timeout;
                    request.SendWebRequest();
                }
                if (request.isDone)
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        this.text = request.downloadHandler.text;
                        this.error = request.error;
                        this.succeed = true;

                        Game.UnBindUpdate(Done);
                        SetResult();
                    }
                    else
                    {
                        _retry++;
                        if (_retry >= retryCount)
                        {
                            this.error = request.error;
                            this.succeed = false;
                            Game.UnBindUpdate(Done);
                            SetResult();
                        }
                    }
                    request.Dispose();
                    request = null;

                }
            }

            public T GetObjFromJson<T>() where T : class
            {
                return JsonUtility.FromJson<T>(this.text);
            }
        }

        public static HttpPost Post(string url, Dictionary<string, string> formFields)
        {
            return new HttpPost($"{AOTDefine.G.GateUrl}/{url}", formFields);
        }
        public static HttpPost PostWithoutBaseUrl(string url, Dictionary<string, string> formFields)
        {
            return new HttpPost(url, formFields);
        }
    }


}
