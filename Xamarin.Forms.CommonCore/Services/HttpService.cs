﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ModernHttpClient;
using System.IO;

namespace Xamarin.Forms.CommonCore
{
    
    public class HttpService : IHttpService, IDisposable
    {
        private HttpClient httpClient;
		private HttpMessageHandler handler;
        private JsonSerializer _serializer;

        public string json;

        public WebClient GetWebClient()
        {
            var client = new WebClient();
            if (CoreSettings.TokenBearer != null)
                client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + CoreSettings.TokenBearer.access_token);
            return client;
        }

        public WebDownloadClient GetWebDownloadClient()
        {
            var client = new WebClient();
            if (CoreSettings.TokenBearer != null)
                client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + CoreSettings.TokenBearer.access_token);
            return new WebDownloadClient() { Client = client };
        }


        public HttpClient Client
        {
            get
            {
                if (httpClient == null)
                {
                    _serializer = new JsonSerializer();

#if __IOS__
                   
                    switch (CoreSettings.Config.HttpSettings.IOSHttpHandler)
                    {
                        case "ModernHttpClient":
                            handler = new NativeMessageHandler()
                            {
                                AllowAutoRedirect = CoreSettings.Config.HttpSettings.HttpAllowAutoRedirect,
                                Credentials = CoreSettings.HttpCredentials
                            };

                            break;
                        case "CFNetwork":
                            handler = new CFNetworkHandler()
                            {
                                AllowAutoRedirect = CoreSettings.Config.HttpSettings.HttpAllowAutoRedirect
                            };
                            break;
                        case "NSURLSession":
                            handler = new NSUrlSessionHandler()
                            {
                                AllowAutoRedirect = CoreSettings.Config.HttpSettings.HttpAllowAutoRedirect,
                                Credentials = CoreSettings.HttpCredentials
                            };
                            break;
                        default:
                            handler = new HttpClientHandler()
                            {
                                AllowAutoRedirect = CoreSettings.Config.HttpSettings.HttpAllowAutoRedirect,
                                Credentials = CoreSettings.HttpCredentials
                            };

                            break;
                    }

#elif __ANDROID__
			switch (CoreSettings.Config.HttpSettings.AndroidHttpHandler)
			{
				case "ModernHttpClient":
					handler = new NativeMessageHandler()
					{
						AllowAutoRedirect = CoreSettings.Config.HttpSettings.HttpAllowAutoRedirect,
						Credentials = CoreSettings.HttpCredentials
					};

					break;
				case "AndroidClientHandler":
					handler = new Xamarin.Android.Net.AndroidClientHandler()
					{
						AllowAutoRedirect = CoreSettings.Config.HttpSettings.HttpAllowAutoRedirect
					};
					break;
				default:
					handler = new HttpClientHandler()
					{
						AllowAutoRedirect = CoreSettings.Config.HttpSettings.HttpAllowAutoRedirect,
						Credentials = CoreSettings.HttpCredentials
					};
					break;
			}
#else
			handler = new HttpClientHandler();
#endif


                    if (CoreSettings.Config.HttpSettings.HttpTimeOut > 0)
                    {
                        httpClient = new HttpClient(handler, true) { Timeout = new TimeSpan(0, 0, CoreSettings.Config.HttpSettings.HttpTimeOut) };
                    }
                    else
                    {
                        httpClient = new HttpClient(handler, true);
                    }

                    if (CoreSettings.TokenBearer != null)
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + CoreSettings.TokenBearer.access_token);
                }

                //httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

                return httpClient;
            }
        }
        public async Task<StringResponse> FormPost(string url, HttpContent content)
        {
            var response = new StringResponse() { };

            if (!CoreSettings.IsConnected)
            {
                response.Success = false;
                response.Error = new ApplicationException("Network Connection Error");
                return response;
            }


            try
            {
                await new SynchronizationContextRemover();

                var postResponse = await Client.PostAsync(url, content).ConfigureAwait(false);
                postResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                postResponse.EnsureSuccessStatusCode();

                var raw = await postResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (raw != null)
                {
                    response.Response = raw;
                    response.Success = true;
                }


            }
            catch (Exception ex)
            {
                ex.ConsoleWrite();
                response.Error = ex;
            }
            return response;
        }

        public async Task<StringResponse> GetRaw(string url)
        {
            var response = new StringResponse() { };

            if (!CoreSettings.IsConnected)
            {
                response.Success = false;
                response.Error = new ApplicationException("Network Connection Error");
                return response;
            }
            try
            {
                await new SynchronizationContextRemover();

                using (var srvResponse = await Client.GetAsync(url).ConfigureAwait(false))
                {
                    var jsonResult = await srvResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    response.Success = true;
                }

            }
            catch (Exception ex)
            {
                ex.ConsoleWrite();
                response.Error = ex;
            }

            return response;
        }
        public async Task<GenericResponse<T>> Get<T>(string url) where T : class, new()
        {
            var response = new GenericResponse<T>() { };

            if (!CoreSettings.IsConnected)
            {
                response.Success = false;
                response.Error = new ApplicationException("Network Connection Error");
                return response;
            }

            try
            {
                await new SynchronizationContextRemover();

                using (var srvResponse = await Client.GetAsync(url).ConfigureAwait(false))
                {
                    if(CoreSettings.Config.HttpSettings.DisplayRawJson){
						json = await GetStringContent<T>(srvResponse).ConfigureAwait(false);
						response.Response = await DeserializeObject<T>(json).ConfigureAwait(false);
						response.Success = true;
						json = string.Empty;
					}
                    else{
						srvResponse.EnsureSuccessStatusCode();
						response.Response = await DeserializeStream<T>(srvResponse);
						response.Success = true;
                    }
                }

            }
            catch (Exception ex)
            {
                ex.ConsoleWrite();
                response.Error = ex;
                response.MetaData = json;
            }

            return response;
        }
        public async Task<GenericResponse<T>> Post<T>(string url, object obj) where T : class, new()
        {
            var response = new GenericResponse<T>() { };

            if (!CoreSettings.IsConnected)
            {
                response.Success = false;
                response.Error = new ApplicationException("Network Connection Error");
                return response;
            }

            try
            {
                await new SynchronizationContextRemover();

                var data = JsonConvert.SerializeObject(obj);
                using (var srvResponse = await Client.PostAsync(url, new StringContent(data, Encoding.UTF8, "application/json")).ConfigureAwait(false))
                {
                    if (CoreSettings.Config.HttpSettings.DisplayRawJson)
                    {
                        json = await GetStringContent<T>(srvResponse).ConfigureAwait(false);
                        response.Response = await DeserializeObject<T>(json).ConfigureAwait(false);
                        response.Success = true;
                        json = string.Empty;
                    }
                    else{
						srvResponse.EnsureSuccessStatusCode();
						response.Response = await DeserializeStream<T>(srvResponse);
						response.Success = true;
                    }
                }

            }
            catch (Exception ex)
            {
                ex.ConsoleWrite();
                response.Error = ex;
                response.MetaData = json;
            }

            return response;

        }
        public async Task<GenericResponse<T>> Put<T>(string url, object obj) where T : class, new()
        {
            var response = new GenericResponse<T>() { };

            if (!CoreSettings.IsConnected)
            {
                response.Success = false;
                response.Error = new ApplicationException("Network Connection Error");
                return response;
            }

            try
            {
                await new SynchronizationContextRemover();

                var data = JsonConvert.SerializeObject(obj);
                using (var srvResponse = await Client.PutAsync(url, new StringContent(data, Encoding.UTF8, "application/json")))
                {
                    if (CoreSettings.Config.HttpSettings.DisplayRawJson)
                    {
                        json = await GetStringContent<T>(srvResponse);
                        response.Response = await DeserializeObject<T>(json);
                        response.Success = true;
                        json = string.Empty;
                    }
                    else
                    {
						srvResponse.EnsureSuccessStatusCode();
                        response.Response = await DeserializeStream<T>(srvResponse);
						response.Success = true;
                    }
                }

            }
            catch (Exception ex)
            {
                ex.ConsoleWrite();
                response.Error = ex;
                response.MetaData = json;
            }

            return response;
        }

        public async Task<string> GetStringContent<T>(HttpResponseMessage response) where T : class, new()
        {
            var jsonResult = await response.Content.ReadAsStringAsync();
            if (CoreSettings.Config.HttpSettings.DisplayRawJson)
            {
                Console.WriteLine();
                Console.WriteLine();
                var name = typeof(T).Name;
                if (name == "List`1")
                {
                    var types = typeof(T).GetGenericArguments();
                    if (types != null && types.Length > 0)
                    {
                        var obj = types[0];
                        name = "Collection of " + obj.Name;
                    }

                }
                Console.WriteLine($"*-*-*-*-*-*-*-*-*-*-*-*- {name} - HTTP STRING RESULT *-*-*-*-*-*-*-*-*-*-*-*-*-");
                var formatted = await FormattedJson(jsonResult);
                Console.WriteLine(formatted);
                Console.WriteLine("*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-");
                Console.WriteLine();
                Console.WriteLine();
            }
            return jsonResult;
        }

        private Task<string> FormattedJson(string jsonResult)
        {
            return Task.Run(() =>
            {
                var obj = JsonConvert.DeserializeObject(jsonResult);
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            });
        }

        private Task<T> DeserializeObject<T>(string content) where T : class, new()
        {
            return Task.Run(() =>
            {
                return JsonConvert.DeserializeObject<T>(content);
            });
        }

		private Task<T> DeserializeStream<T>(HttpResponseMessage response)
		{
            return Task.Run(async () =>
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        using (var json = new JsonTextReader(reader))
                        {
                            return _serializer.Deserialize<T>(json);
                        }
                    }
                }
            });
		}

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(json))
            {
                json = null;
            }

            if (httpClient != null)
            {
                httpClient.Dispose();
                handler.Dispose();
                _serializer = null;
            }

        }
    }

}

