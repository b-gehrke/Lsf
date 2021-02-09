using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Lsf.Grading.Models;
using Newtonsoft.Json;

namespace Lsf.Grading.Services.Notifiers
{
    public class CallbackUrlNotifier : INotifier
    {
        
        private readonly HttpClient _sslValidatingClient= new();
        private readonly HttpClient _sslNoValidatingClient= new(new HttpClientHandler {ServerCertificateCustomValidationCallback = (_, _, _, _) => true});
        private readonly Config _config;

        public record Config
        {
            public string CallbackUrl { get; }
            public string Method { get; } = "GET";
            public bool ValidateSslCertificate { get; } = true;
        }

        private readonly HttpClient _client;

        public CallbackUrlNotifier(Config config)
        {
            _config = config;

            _client = config.ValidateSslCertificate ? _sslValidatingClient : _sslNoValidatingClient;
        }

        public async Task NotifyChange(IEnumerable<Degree> degrees)
        {
            var settings = new JsonSerializerSettings
            {
                FloatFormatHandling = FloatFormatHandling.DefaultValue
            };

            var msg = new HttpRequestMessage(new HttpMethod(_config.Method), _config.CallbackUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(degrees, settings), Encoding.UTF8,
                    "application/json")
            };


            await _client.SendAsync(msg);
        }

        public Task NotifyError(string message)
        {
            return _client.SendAsync(new HttpRequestMessage(new HttpMethod(_config.Method), _config.CallbackUrl)
            {
                Content = JsonContent.Create(new {Error = message})
            });
        }
    }
}