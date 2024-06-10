using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Shadows.Common
{
    /// <summary>
    /// Options
    /// </summary>
    public class CommonOptions
    {
        /// <summary>
        /// ConnectionString
        /// </summary>
        public string ConnectionString { get; set; }
       
        /// <summary>
        /// options for error' notify lib
        /// </summary>
        public ErrorNotifyOptions ErrorNotifyOptions { get; set; }
    }

    public enum ResponseEnum
    {
        OK = 0,
        Error = 1,
        NeedAuth = 2
    }

    public class Response<TResp> where TResp : class
    {
        public ResponseEnum ResponseCode { get; set; }
        public TResp ResponseBody { get; set; }
    }

    public static class HttpApiHelper
    {
        public static StringContent SerializeRequest<TReq>(this TReq entity)
        {
            var json = JsonConvert.SerializeObject(entity);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            return data;
        }

        public static async Task<Response<TResp>> ParseResponse<TResp>(this HttpResponseMessage result) where TResp : class
        {
            if (result != null && result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadAsStringAsync();
                return new Response<TResp>()
                {
                    ResponseCode = ResponseEnum.OK,
                    ResponseBody = JObject.Parse(response).ToObject<TResp>()
                };
            }
            if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new Response<TResp>()
                {
                    ResponseCode = ResponseEnum.NeedAuth
                };
            }
            return new Response<TResp>()
            {
                ResponseCode = ResponseEnum.Error
            };
        }

        public static async Task<Response<IEnumerable<T>>> ParseResponseArray<T>(this HttpResponseMessage result) where T : class
        {
            if (result != null && result.IsSuccessStatusCode)
            {
                var ret = new List<T>();
                var response = await result.Content.ReadAsStringAsync();
                foreach (var item in JArray.Parse(response))
                {
                    ret.Add(item.ToObject<T>());
                }
                return new Response<IEnumerable<T>>()
                {
                    ResponseCode = ResponseEnum.OK,
                    ResponseBody = ret
                };
            }
            if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new Response<IEnumerable<T>>()
                {
                    ResponseCode = ResponseEnum.NeedAuth
                };
            }
            return new Response<IEnumerable<T>>()
            {
                ResponseCode = ResponseEnum.Error
            };
        }
    }

    public class ErrorNotifyService : IDisposable, IErrorNotifyService
    {
        private bool isConnected = false;
        private bool isAuth = false;
        private bool isDisposed = false;
        private bool _sendMessage = false;

        private string _server;
        private string _login;
        private string _password;
        private string _feedback;
        private string _defaultTitle;

        private object _lockObject = new object();
        private bool isLock = false;

        private string _token { get; set; }

        public ErrorNotifyService(ErrorNotifyOptions options)
        {
            if (options.SendError)
            {
                if (!string.IsNullOrEmpty(options.Server))
                {
                    _sendMessage = true;
                    _server = options.Server;
                    _login = options.Login;
                    _password = options.Password;
                    _feedback = options.FeedbackContact;
                    _defaultTitle = options.DefaultTitle;

                    Task.Factory.StartNew(CheckConnect, TaskCreationOptions.LongRunning);
                }
                else
                {
                    Console.WriteLine($"ErrorNotifyService error: Options.Server not set");
                }
            }
        }

        private async Task<bool> Auth()
        {
            bool _isLocked = false;
            lock (_lockObject)
            {
                if (isLock)
                {
                    _isLocked = true;
                }
                else
                {
                    isLock = true;
                }
            }
            if (_isLocked)
            {
                for (int i = 0; i < 60; i++)
                {
                    if (!isLock)
                    {
                        break;
                    }
                    await Task.Delay(1000);
                }
                if (!isLock)
                {
                    if (isAuth) return true;
                    if (isConnected) return false;
                }
                else
                {
                    Console.WriteLine($"ErrorNotifyService: Error in Auth method: cant wait for auth with lock");
                    return false;
                }
            }

            var result = await Execute(client =>
                client.PostAsync($"{_server}/api/v1/client/auth", new ErrorNotifyClientIdentity()
                {
                    Login = _login,
                    Password = _password
                }.SerializeRequest()), "Post", s => s.ParseResponse<ErrorNotifyClientIdentityResponse>(), false);
            if (result.ResponseCode == ResponseEnum.Error)
            {
                if (isConnected)
                {
                    Console.WriteLine($"ErrorNotifyService: Error in Auth method: wrong login or password");
                    _sendMessage = false;
                }
                return false;
            }
            _token = result.ResponseBody.Token;
            isAuth = true;
            lock (_lockObject)
            {
                isLock = false;
            }
            return true;
        }

        public async Task Send(string message, MessageLevelEnum level = MessageLevelEnum.Error, string title = null)
        {
            if (_sendMessage)
            {
                var result = await Execute(client =>
                {
                    var request = new HttpRequestMessage()
                    {
                        Headers = {
                            { HttpRequestHeader.Authorization.ToString(), $"Bearer {_token}" },
                            { HttpRequestHeader.ContentType.ToString(), "application/json" },
                        },
                        RequestUri = new Uri($"{_server}/api/v1/message/send"),
                        Method = HttpMethod.Post,
                        Content = new MessageCreator()
                        {
                            Description = message,
                            FeedbackContact = _feedback,
                            Level = (int)level,
                            Title = title ?? _defaultTitle
                        }.SerializeRequest()
                    };

                    return client.SendAsync(request);
                }, "Send", s => s.ParseResponse<MessageCreator>());

                if (result.ResponseCode == ResponseEnum.Error)
                {
                    Console.WriteLine($"ErrorNotifyService: Error in Send method: cant send message error");
                }
            }
        }

        private async Task<Response<T>> Execute<T>(
            Func<HttpClient, Task<HttpResponseMessage>> action,
            string method,
            Func<HttpResponseMessage, Task<Response<T>>> parseMethod, bool needAuth = true) where T : class
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    if (isConnected)
                    {
                        var result = await action(client);
                        var resp = await parseMethod(result);
                        if (resp.ResponseCode == ResponseEnum.NeedAuth)
                        {
                            if (needAuth && await Auth())
                            {
                                result = await action(client);
                                resp = await parseMethod(result);
                            }
                            else
                            {
                                return new Response<T>()
                                {
                                    ResponseCode = ResponseEnum.Error
                                };
                            }
                        }
                        return resp;
                    }
                    Console.WriteLine($"Error in {method}: server not connected");
                    return new Response<T>()
                    {
                        ResponseCode = ResponseEnum.Error
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in {method}: {ex.Message}; StackTrace: {ex.StackTrace}");
                    return new Response<T>()
                    {
                        ResponseCode = ResponseEnum.Error
                    };
                }
            }
        }

        private async Task CheckConnect()
        {
            while (!isDisposed)
            {
                isConnected = await CheckConnectOnce(_server);
                await Task.Delay(1000);
            }
        }

        private async Task<bool> CheckConnectOnce(string server)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var check = await client.GetAsync($"{server}/api/v1/common/ping");
                    var result = check != null && check.IsSuccessStatusCode;
                    Console.WriteLine($"Ping result: server {server} {(result ? "connected" : "disconnected")}");
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CheckConnect: {ex.Message}; StackTrace: {ex.StackTrace}");
                    return false;
                }
            }
        }

        public void Dispose()
        {
            isDisposed = true;
        }
    }

    public enum MessageLevelEnum
    {
        Issue = 0,
        Warning = 1,
        Error = 10
    }

    public class ErrorNotifyMessage
    {
        public string Message { get; set; }
        public string Title { get; set; }
        public MessageLevelEnum MessageLevel { get; set; }
    }

    public class ErrorNotifyClientIdentity
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class ErrorNotifyClientIdentityResponse
    {
        public string Token { get; set; }
        public string UserName { get; set; }
    }

    public class MessageCreator
    {
        public int Level { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FeedbackContact { get; set; }
    }

    public class ErrorNotifyLogger : ILogger
    {
        private readonly string _name;
        private IErrorNotifyService _errorNotifyService;
        private readonly Func<ErrorNotifyLoggerConfiguration> _getCurrentConfig;

        public ErrorNotifyLogger(string name, IErrorNotifyService errorNotifyService,
            Func<ErrorNotifyLoggerConfiguration> getCurrentConfig)
        {
            _errorNotifyService = errorNotifyService;
            _getCurrentConfig = getCurrentConfig;
            _name = name;
        }

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel)
        {
            return _getCurrentConfig().LogLevels.Contains(logLevel);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ErrorNotifyLoggerConfiguration config = _getCurrentConfig();
            if (config.EventId == 0 || config.EventId == eventId.Id)
            {
                try
                {
                    _errorNotifyService
                        .Send($"Message: {exception.Message} StackTrace: {exception.StackTrace}")
                        .ContinueWith(s => {
                            if (s.Exception != null)
                            {
                                Console.WriteLine($"ErrorNotify exception: {s.Exception.Message} {s.Exception.StackTrace}");
                            }
                        })
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ErrorNotify exception: {ex.Message} {ex.StackTrace}");
                }
            }
        }
    }

    public class ErrorNotifyLoggerConfiguration
    {
        public int EventId { get; set; }

        public ErrorNotifyOptions Options { get; set; }

        public List<LogLevel> LogLevels { get; set; } = new List<LogLevel>()
        {
            LogLevel.Error,
            LogLevel.Critical
        };
    }

    public sealed class ErrorNotifyLoggerProvider : ILoggerProvider
    {
        private readonly IDisposable _onChangeToken;
        private ErrorNotifyLoggerConfiguration _currentConfig;
        private readonly ConcurrentDictionary<string, ErrorNotifyLogger> _loggers = new ConcurrentDictionary<string, ErrorNotifyLogger>();


        public ErrorNotifyLoggerProvider(
            IOptionsMonitor<ErrorNotifyLoggerConfiguration> config)
        {
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
        }

        public ILogger CreateLogger(string categoryName)
        {
            var errorNotifyService = new ErrorNotifyService(_currentConfig.Options);
            var logger = _loggers.GetOrAdd(categoryName, name => new ErrorNotifyLogger(name, errorNotifyService, GetCurrentConfig));
            return logger;
        }

        private ErrorNotifyLoggerConfiguration GetCurrentConfig() => _currentConfig;

        public void Dispose()
        {
            _loggers.Clear();
            _onChangeToken.Dispose();
        }
    }
}
