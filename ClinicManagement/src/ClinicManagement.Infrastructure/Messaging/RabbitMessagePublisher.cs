﻿using System;
using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using ClinicManagement.Core.Interfaces;
using Microsoft.Extensions.ObjectPool;
using PluralsightDdd.SharedKernel;
using PluralsightDdd.SharedKernel.Interfaces;
using RabbitMQ.Client;

namespace ClinicManagement.Infrastructure.Messaging
{
    public class RabbitMessagePublisher : IMessagePublisher
    {
        private readonly DefaultObjectPool<IModel> _objectPool;

        public RabbitMessagePublisher(IPooledObjectPolicy<IModel> objectPolicy)
        {
            _objectPool = new DefaultObjectPool<IModel>(objectPolicy, Environment.ProcessorCount * 2);
        }

        public void Publish(IApplicationEvent applicationEvent)
        {
            Guard.Against.Null(applicationEvent, nameof(applicationEvent));

            var channel = _objectPool.Get();

            object message = (object)applicationEvent;
            try
            {
                string exchangeName = MessagingConstants.Exchanges.FRONTDESK_CLINICMANAGEMENT_EXCHANGE;
                channel.ExchangeDeclare(exchangeName, "direct", true, false, null);

                var sendBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(
                  exchange: exchangeName,
                  routingKey: "entity-changes",
                  basicProperties: properties,
                  body: sendBytes);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _objectPool.Return(channel);
            }
        }
    }
}
