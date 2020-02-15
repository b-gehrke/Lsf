using System.Threading.Tasks;
using Lsf.Grading.Parser;
using NUnit.Framework;

namespace Lsf.Test
{
    public class GradingTests
    {
        [Test]
        public async Task TestGradeParsing()
        {
            var client = ClientTests.Client();
            await client.Authenticate("xxx", "xxx");

            var parser = new GradingParser(client);

            var g = await parser.GetGradesForAllDegrees();

            Assert.IsNotEmpty(g);
        }
    }
}