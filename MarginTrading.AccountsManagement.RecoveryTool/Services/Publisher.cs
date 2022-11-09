// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using Lykke.RabbitMqBroker.Publisher.Serializers;

using RabbitMQ.Client;

namespace MarginTrading.AccountsManagement.RecoveryTool.Services;

public class Publisher : IDisposable
{
    private IConnection _connection;
    private IModel _publishingChannel;

    public Publisher(string rabbitMqConnectionString)
    {
        var factory = new ConnectionFactory {Uri = new Uri(rabbitMqConnectionString, UriKind.Absolute)};
        _connection = factory.CreateConnection();

        _publishingChannel = _connection.CreateModel();
    }

    public async Task Publish<T>(T command, string routingKey)
    {
        IBasicProperties properties = _publishingChannel.CreateBasicProperties();

        var serializer = new MessagePackMessageSerializer<T>();
        var message = serializer.Serialize(command);

        _publishingChannel.BasicPublish("",
            routingKey, properties, message);
    }

    public void Dispose()
    {
        _publishingChannel?.Close();
        _publishingChannel?.Dispose();

        _connection?.Close();
        _connection?.Dispose();
    }
}