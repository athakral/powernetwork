using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PowerNetwork.Core.Helpers
{
    public static class DomainHelper
    {
        public static string GetSubDomain(this HttpRequest request)
        {
            var hostParts = request.Host.ToString().Split('.');
            return hostParts.Length > 3 ? hostParts[0] : "gnf";
        }
    }
}
