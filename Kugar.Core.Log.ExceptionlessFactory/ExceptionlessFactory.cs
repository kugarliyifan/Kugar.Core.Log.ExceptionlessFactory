using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exceptionless;
using Exceptionless.Json;
using Exceptionless.Models;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;

namespace Kugar.Core
{
    public class ExceptionlessFactory: ILoggerFactory
    {
        private ExceptionlessLogger _logger = null;
        private ExceptionlessClient _client = null;

        /// <summary>
        /// Exceptionless 构造函数
        /// </summary>
        /// <param name="serviceUrl">ExceptionLess服务器地址</param>
        /// <param name="apiKey">ExceptionLess的ApiKey</param>
        public ExceptionlessFactory(string serviceUrl,string apiKey)
        {
            if (string.IsNullOrWhiteSpace(serviceUrl))
            {
                throw new ArgumentNullException(nameof(serviceUrl));
            }

            if (!serviceUrl.StartsWith("http",true,CultureInfo.CurrentCulture))
            {
                serviceUrl = "http://" + serviceUrl;
            }

            //if (string.IsNullOrWhiteSpace(apiKey))
            //{
            //    apiKey = CustomConfigManager.Default.AppSettings.GetValueByName("ExceptionLessApiKey");
            //}

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            _client=new ExceptionlessClient();

            _client.Configuration.Enabled = true;
            _client.Configuration.ApiKey = apiKey;
            _client.Configuration.ServerUrl = serviceUrl;
            _client.Configuration.RemovePlugin("Exceptionless.Plugins.Default.EnvironmentInfoPlugin");
            _client.Configuration.UseTraceLogger();
            _client.Configuration.UseReferenceIds();
            _client.Configuration.UseInMemoryStorage();
            //ExceptionlessClient.Default.RegisterWebApi(GlobalConfiguration.Configuration);
            _client.Startup();

            _logger=new ExceptionlessLogger(_client);
        }

        public ExceptionlessClient Client => _client;

        public EventBuilder CreateEvent() => _client?.CreateEvent();

        public ILogger GetLogger(string loggerName)
        {
            return _logger;
        }

        public ILogger Default => _logger;
    }

    public class ExceptionlessLogger : LoggerBase
    {
        private ExceptionlessClient _logger = null;
        private object[] emptyArgs = new object[0];

        public ExceptionlessLogger(ExceptionlessClient client)
        {
            _logger = client;
        }

        public EventBuilder CreateEvent()
        {
            var builder = _logger?.CreateEvent();

            builder.SetSource("Kugar.Core.Log.ExceptionlessFactory");

            return _logger?.CreateEvent();
        }

        protected override void DebugInternal(string message, KeyValuePair<string, object>[] extData)
        {
            commonData(Event.KnownTypes.Log, extData)
                .SetMessage(message)
                .Submit();
        }

        protected override void TraceInternal(string message, KeyValuePair<string, object>[] extData)
        {
            commonData(Event.KnownTypes.Log, extData)
                .SetMessage(message)
                .Submit();
        }

        protected override void WarnInternal(string message,Exception error, KeyValuePair<string, object>[] extData)
        {
            commonData(Event.KnownTypes.Error,extData)
                .SetMessage(message)
                .Submit();
        }

        protected override void ErrorInternal(string message, Exception error, KeyValuePair<string, object>[] extData)
        {
            commonData(Event.KnownTypes.Error,extData)
                .SetMessage(message)
                .SetException(error)
                .Submit();
        }

        private EventBuilder commonData(string type, KeyValuePair<string, object>[] extData)
        {
            var eb = CreateEvent();

            if (extData.HasData())
            {
                foreach (var pair in extData)
                {
                    eb.SetProperty(pair.Key, pair.Value);
                }
            }

            return eb.SetType(type);
        }
    }
}
