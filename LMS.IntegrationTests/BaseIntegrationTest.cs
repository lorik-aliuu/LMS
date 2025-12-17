using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.IntegrationTests
{
    public class BaseIntegrationTest : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly HttpClient Client;

        protected BaseIntegrationTest(CustomWebApplicationFactory factory)
        {
            Client = factory.CreateClient();
        }
    }
}
