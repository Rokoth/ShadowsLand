using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadows.Client.Services.Interface
{
    public interface IHttpClientService
    {
        Task SendErrorMessage(string message, MessageLevelEnum? level, string? title);
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
}
