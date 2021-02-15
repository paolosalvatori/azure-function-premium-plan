#region Copyright
//=======================================================================================
// This sample is supplemental to the technical guidance published on the community
// blog at https://github.com/paolosalvatori. 
// 
// Author: Paolo Salvatori
//=======================================================================================
// Copyright © 2021 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
//=======================================================================================
#endregion

#region Using Directives
using System.Collections.Generic;
using Newtonsoft.Json;
#endregion

namespace BaboNatGw
{
    public class CustomMessage
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public Dictionary<string, object> Properties { get; set; }

        [JsonProperty(PropertyName = "publicIpAddress")]
        public string PublicIpAddress { get; set; }
    }
}