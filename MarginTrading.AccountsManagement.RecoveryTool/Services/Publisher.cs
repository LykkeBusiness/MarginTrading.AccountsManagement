// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Common;

using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;

using Microsoft.Extensions.Logging;

using RabbitMQ.Client;

namespace MarginTrading.AccountsManagement.RecoveryTool.Services
{

    public sealed class Publisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _publishingChannel;
        private readonly ILogger<Publisher> _logger;

        public Publisher(string rabbitMqConnectionString, ILogger<Publisher> logger)
        {
            _logger = logger;
            
            var factory = new ConnectionFactory
            {
                Uri = new Uri(rabbitMqConnectionString, UriKind.Absolute),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(60)
            };

            _connection = factory.CreateConnection();

            _publishingChannel = _connection.CreateModel();
        }

        public void Publish<T>(T command, string routingKey) where T: class
        {
            IBasicProperties properties = _publishingChannel.CreateBasicProperties();
            properties.Type = typeof(T).Name;

            var serializer = new MessagePackMessageSerializer<T>();
            var message = serializer.Serialize(command);

            _publishingChannel.BasicReturn += (sender, args) =>
            {
                // not sure if we can use published message body here
                T returnedMessage = null;
                if (args.Body.Length > 0)
                {
                    var deserializer = new MessagePackMessageDeserializer<T>();
                    returnedMessage = deserializer.Deserialize(args.Body.ToArray());
                }

                var returnedMessageString = returnedMessage?.ToJson() ?? "empty";
                _logger.LogError("Message was not published. Reason: {ReplyText}, code: {ReplyCode}, body : {Message)}", 
                    args.ReplyText,
                    args.ReplyCode,
                    returnedMessageString);
            };

            _publishingChannel.BasicPublish("", routingKey, true, properties, message);
        }

        public void Dispose()
        {
            _publishingChannel?.Close();
            _publishingChannel?.Dispose();

            _connection?.Close();
            _connection?.Dispose();
        }
    }
}