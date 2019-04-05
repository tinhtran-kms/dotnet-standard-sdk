/**
* Copyright 2018 IBM Corp. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
*/

using System.Collections.Generic;
using IBM.Cloud.SDK.Core;
using Newtonsoft.Json;

namespace IBM.Watson.Discovery.v1.Model
{
    /// <summary>
    /// Object containing result information that was returned by the query used to create this log entry. Only returned
    /// with logs of type `query`.
    /// </summary>
    public class LogQueryResponseResultDocuments : BaseModel
    {
        /// <summary>
        /// Array of log query response results.
        /// </summary>
        [JsonProperty("results", NullValueHandling = NullValueHandling.Ignore)]
        public List<LogQueryResponseResultDocumentsResult> Results { get; set; }
        /// <summary>
        /// The number of results returned in the query associate with this log.
        /// </summary>
        [JsonProperty("count", NullValueHandling = NullValueHandling.Ignore)]
        public long? Count { get; set; }
    }

}
