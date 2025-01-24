using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using Shadows.Client.Services.Service;
using Shadows.Common;
using System.Collections.Concurrent;

namespace Shadows.Client
{
    internal static class Program
    {
        private const string _logDirectory = "Logs";
        private const string _logFileName = "log-startup.txt";
        private const string _appSettingsFileName = "appsettings.json";
        private const string _startUpInfoMessage = "App starts with arguments: {0}";

        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string _startUpLogPath = Path.Combine(_logDirectory, _logFileName);
            var loggerConfig = new LoggerConfiguration()
               .WriteTo.Console()
               .WriteTo.File(_startUpLogPath)
               .MinimumLevel.Verbose();

            using var logger = loggerConfig.CreateLogger();
            logger.Information(string.Format(_startUpInfoMessage, string.Join(", ", args)));

            var host = CreateHostBuilder(args).Build();
            ServiceProvider = host.Services;

            Application.Run(ServiceProvider.GetRequiredService<MainForm>());
        }
        public static IServiceProvider ServiceProvider { get; private set; }

        static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration(s => s.SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile(_appSettingsFileName, optional: false, reloadOnChange: true)
                                .AddEnvironmentVariables())
                .ConfigureAppConfiguration((hostingContext, config) => ConfigureApp(args, config))
                .ConfigureLogging(CreateLogger)
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                });
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<MainForm>();
        }

        private static void CreateLogger(HostBuilderContext hostingContext, ILoggingBuilder logging)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(hostingContext.Configuration)
                .CreateLogger();
            logging.AddSerilog(Log.Logger);
            logging.AddErrorNotifyLogger();
        }

        private static void ConfigureApp(string[] args, IConfigurationBuilder config)
        {
            if (args != null) config.AddCommandLine(args);

        }
    }

    public static class StartupExtensions
    {
        public static ILoggingBuilder AddErrorNotifyLogger(
            this ILoggingBuilder builder,
            Action<ClientErrorNotifyLoggerConfiguration> configure)
        {
            builder.AddErrorNotifyLogger();
            builder.Services.Configure(configure);

            return builder;
        }

        public static ILoggingBuilder AddErrorNotifyLogger(
        this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, ClientErrorNotifyLoggerProvider>());

            LoggerProviderOptions.RegisterProviderOptions
                <ClientErrorNotifyLoggerConfiguration, ClientErrorNotifyLoggerProvider>(builder.Services);

            return builder;
        }
    }

    public class ClientErrorNotifyLoggerConfiguration
    {
        public int EventId { get; set; }

        public List<LogLevel> LogLevels { get; set; } =
        [
            LogLevel.Error,
            LogLevel.Critical
        ];
    }

    public sealed class ClientErrorNotifyLoggerProvider : ILoggerProvider
    {
        private readonly IDisposable _onChangeToken;
        private ClientErrorNotifyLoggerConfiguration _currentConfig;
        private readonly ConcurrentDictionary<string, ClientErrorNotifyLogger> _loggers = new ConcurrentDictionary<string, ClientErrorNotifyLogger>();


        public ClientErrorNotifyLoggerProvider(
            IOptionsMonitor<ClientErrorNotifyLoggerConfiguration> config)
        {
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            var errorNotifyService = new ClientErrorNotifyService(new HttpClientService());
            var logger = _loggers.GetOrAdd(categoryName, name => new ClientErrorNotifyLogger(name, errorNotifyService, GetCurrentConfig));
            return logger;
        }

        private ClientErrorNotifyLoggerConfiguration GetCurrentConfig() => _currentConfig;

        public void Dispose()
        {
            _loggers.Clear();
            _onChangeToken.Dispose();
        }
    }

    public class ClientErrorNotifyLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly string _name;
        private IClientErrorNotifyService _errorNotifyService;
        private readonly Func<ClientErrorNotifyLoggerConfiguration> _getCurrentConfig;

        public ClientErrorNotifyLogger(string name, IClientErrorNotifyService errorNotifyService,
            Func<ClientErrorNotifyLoggerConfiguration> getCurrentConfig)
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

            ClientErrorNotifyLoggerConfiguration config = _getCurrentConfig();
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

    public class ClientErrorNotifyService(HttpClientService httpClientService) : IDisposable, IClientErrorNotifyService
    {
        private readonly HttpClientService _httpClientService = httpClientService;

        public async Task Send(string message, Services.Interface.MessageLevelEnum? level = Services.Interface.MessageLevelEnum.Error, string? title = null)
        {
            await _httpClientService.SendErrorMessage(message, level, title);
        }

        public void Dispose()
        {

        }
    }

    public interface IClientErrorNotifyService
    {
        void Dispose();
        Task Send(string message, Services.Interface.MessageLevelEnum? level = null, string? title = null);
    }
}