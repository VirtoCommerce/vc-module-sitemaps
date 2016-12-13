using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace VirtoCommerce.SitemapsModule.Web.Common
{
    public class FileResult : IHttpActionResult
    {
        private Stream _stream;

        public FileResult(Stream stream)
        {
            _stream = stream;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(_stream)
            };

            return Task.FromResult(response);
        }
    }
}