using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaturalApi.Reporter
{
    public sealed class NullReporter : INaturalReporter
    {
        public void OnAssertionFailed(string message, ApiResultContext response)
        {

        }

        public void OnAssertionPassed(string message, ApiResultContext response)
        {

        }

        public void OnRequestSent(ApiRequestSpec request)
        {

        }

        public void OnResponseReceived(IApiResultContext response)
        {
            // no-op
        }
    }

}
