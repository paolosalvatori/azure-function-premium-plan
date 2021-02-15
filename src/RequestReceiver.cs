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
using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.ServiceBus;
using System.Linq;
#endregion

namespace BaboNatGw
{
    public static class RequestReceiver
    {
        #region Private Constants
        private const string IpifyUrl = "https://api.ipify.org";
        private const string Unknown = "UNKNOWN";
        #endregion

        #region Private Static Fields
        // Create a single, static HttpClient
        private static Lazy<HttpClient> lazyClient = new Lazy<HttpClient>();
        private static HttpClient httpClient = lazyClient.Value; 
        #endregion

        [FunctionName("ProcessRequest")]
        public static async Task Run([ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnectionString")] Message message,
                                     [CosmosDB(databaseName: "%CosmosDbName%", collectionName:"%CosmosDbCollectionName%", ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<CustomMessage> items,
                                      ILogger log,
                                      ExecutionContext executionContext)
        {
            try
            {
                // Validate the incoming message
                if (message == null)
                {
                    return;
                }

                // Initialize data
                var messageId = string.IsNullOrEmpty(message.MessageId) ? Guid.NewGuid().ToString() : message.MessageId;
                var text = Encoding.UTF8.GetString(message.Body);
                var publicIpAddress = Unknown;

                // Retrieve the public IP from Ipify site
                try
                {
                    var response = await httpClient.GetAsync(IpifyUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        publicIpAddress = await response.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"An error occurred while processing message with id=[{messageId}]: {ex.Message}");
                }

                // Initialize message
                var customMessage = new CustomMessage
                {
                    Id = messageId,
                    Message = text,
                    Properties = new System.Collections.Generic.Dictionary<string, object>(message.UserProperties),
                    PublicIpAddress = publicIpAddress
                };

                // Store the message to Cosmos DB
                await items.AddAsync(customMessage);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
