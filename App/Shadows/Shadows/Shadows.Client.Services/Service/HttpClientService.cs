using Shadows.Client.Services.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadows.Client.Services.Service
{
    public class HttpClientService : IHttpClientService
    {
        public Task SendErrorMessage(string message, MessageLevelEnum? level, string? title)
        {
            throw new NotImplementedException();
        }
    }
}
