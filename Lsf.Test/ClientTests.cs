using System.Threading.Tasks;
using Lsf.Client;
using NUnit.Framework;

namespace Lsf.Test
{
    public class ClientTests
    {
        public static LsfHttpClient Client()
        {
            return new LsfHttpClientImpl("https://lsf.ovgu.de");
        }

        [Test]
        public async Task TestCookieAuthentication()
        {
            var client = Client();

            var result = await client.Authenticate("xxx");

            Assert.IsTrue(result);
        }

        [Test]
        public async Task TestPasswordAuthentication()
        {
            var client = Client();

            var result = await client.Authenticate("xxx", "xxx");

            Assert.IsTrue(result);
        }
    }
}