using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaturalApi.Reporter
{
    public interface INaturalReporter
    {
        void OnRequestSent(ApiRequestSpec request);
        void OnResponseReceived(IApiResultContext response);
        void OnAssertionPassed(string message, ApiResultContext response);
        void OnAssertionFailed(string message, ApiResultContext response);
    }


}
