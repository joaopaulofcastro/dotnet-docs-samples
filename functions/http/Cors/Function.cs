﻿// Copyright 2020 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// [START functions_http_cors]
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;

namespace Cors
{
    // For more information about CORS and CORS preflight requests, see
    // https://developer.mozilla.org/en-US/docs/Glossary/Preflight_request.
    public class Function : IHttpFunction
    {
        public async Task HandleAsync(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            // Set CORS headers
            //   Allows GETs from any origin with the Content-Type
            //   header and caches preflight response for 3600s

            response.Headers.Append("Access-Control-Allow-Origin", "*");
            if (HttpMethods.IsOptions(request.Method))
            {
                response.Headers.Append("Access-Control-Allow-Methods", "GET");
                response.Headers.Append("Access-Control-Allow-Headers", "Content-Type");
                response.Headers.Append("Access-Control-Max-Age", "3600");
                response.StatusCode = (int) HttpStatusCode.NoContent;
                return;
            }

            await response.WriteAsync("CORS headers set successfully!");
        }
    }
}
// [END functions_http_cors]
