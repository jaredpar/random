using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Roslyn.Jenkins.Tests
{
    public class UniqueJobIdTests
    {
        [Fact]
        public void RoundTrip()
        {
            foreach (var platform in Enum.GetValues(typeof(Platform)).Cast<Platform>())
            {
                var id = new UniqueJobId(new JobId(42, platform), DateTime.UtcNow);
                var key = id.Key;
                var id2 = UniqueJobId.TryParse(key);
                Assert.True(id2.HasValue);
                Assert.Equal(id, id2.Value);
                Assert.Equal(id.Key, id2.Value.Key);
            }
        }
    }
}
