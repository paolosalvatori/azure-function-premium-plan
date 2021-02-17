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
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.ServiceBus;
#endregion

namespace Microsoft.Azure.Samples
{
    public class RequestReceiver
    {
        #region Private Constants
        private const string IpifyUrl = "https://api.ipify.org";
        private const string Unknown = "UNKNOWN";
        private const string Empty = "EMPTY";
        #endregion


        #region Private Instance Fields
        private readonly HttpClient httpClient;
        #endregion

        #region Private Static Fields
        private static string queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName", EnvironmentVariableTarget.Process);
        #endregion

        #region Public Constructor
        public RequestReceiver(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        } 
        #endregion

        [FunctionName("ProcessRequest")]
        public async Task Run([ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnectionString")] Message message,
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

                // Log message
                log.LogInformation($"Started '{executionContext.FunctionName}' " + 
                                   $"(Running, Id={executionContext.InvocationId}) " +
                                   $"A message with Id={message.MessageId ?? Empty} " + 
                                   $"has been received from the {queueName} queue");

                // Initialize data
                var messageId = string.IsNullOrEmpty(message.MessageId) ? 
                                Guid.NewGuid().ToString() : 
                                message.MessageId;
                var text = Encoding.UTF8.GetString(message.Body) ?? Empty;
                var publicIpAddress = Unknown;
                
                // Retrieve the public IP from Ipify site
                try
                {
                    var response = await httpClient.GetAsync(IpifyUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        publicIpAddress = await response.Content.ReadAsStringAsync();

                        // Log message
                        log.LogInformation($"Running '{executionContext.FunctionName}' " +
                                           $"(Running, Id={executionContext.InvocationId}) " +
                                           $"Call to {IpifyUrl} returned {publicIpAddress}");
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"Error '{executionContext.FunctionName}' " +
                                     $"(Running, Id={executionContext.InvocationId}) " +
                                     $"An error occurred while calling {IpifyUrl}: {ex.Message}");
                }

                // Initialize message
                var customMessage = new CustomMessage
                {
                    Id = messageId,
                    Message = text,
                    Properties = new Dictionary<string, object>(message.UserProperties),
                    PublicIpAddress = publicIpAddress
                };

                // Store the message to Cosmos DB
                await items.AddAsync(customMessage);
                log.LogInformation($"Completed '{executionContext.FunctionName}' " +
                                   $"(Running, Id={executionContext.InvocationId}) "+
                                   $"The message with Id={message.MessageId ?? Empty} " +
                                   $"has been successfully stored to Cosmos DB");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed '{executionContext.FunctionName}' " +
                                 $"(Running, Id={executionContext.InvocationId}) {ex.Message}");
                throw;
            }
        }
    }
}
