using System;

namespace ASP.NETCoreAPIAuthSchemeUsingJWTs.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}