using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Common.Extensions;

using MarginTrading.AccountsManagement.IntegrationTests.Settings;

namespace MarginTrading.AccountsManagement.IntegrationTests.Infrastructure
{
    public static class RabbitUtil
    {
        private static readonly RabbitMqService _rabbitMqService = new RabbitMqService();

        private static readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _messagesHistory =
            new ConcurrentDictionary<Type, ConcurrentBag<object>>();

        private static readonly ConcurrentDictionary<Type, ImmutableList<Listener>> _listeners =
            new ConcurrentDictionary<Type, ImmutableList<Listener>>();

        public static Task<T> WaitForCqrsMessage<T>(Func<T, bool> predicate)
            where T : class
        {
            var listener = new Listener<T>(predicate, new TaskCompletionSource<T>());

            _listeners.AddOrUpdate(typeof(T),
                k => ImmutableList.Create<Listener>(listener),
                (k, l) => l.Add(listener));

            var sutableOldMessage = _messagesHistory.GetValueOrDefault(typeof(T))?.Cast<T>().FirstOrDefault(predicate);
            if (sutableOldMessage != null)
            {
                CompleteListener(sutableOldMessage, listener);

                _listeners.AddOrUpdate(typeof(T),
                    k => ImmutableList<Listener>.Empty,
                    (k, l) => l.Remove(listener));
            }

            return listener.TaskCompletionSource.Task.WithTimeout((int) TimeSpan.FromSeconds(10).TotalMilliseconds);
        }

        public static void ListenCqrsMessages<T>(string connectionString, string exchange)
            where T : class
        {
            var routingKey = typeof(T).Name;
            _rabbitMqService.Subscribe(new RabbitConnectionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = exchange,
                RoutingKey = routingKey,
            }, false, MessageHandler, _rabbitMqService.GetMsgPackDeserializer<T>());
        }

        private static Task MessageHandler<T>(T message)
        {
            _messagesHistory.GetOrAdd(typeof(T), t => new ConcurrentBag<object>()).Add(message);

            var listeners = Array.Empty<Listener<T>>();
            _listeners.AddOrUpdate(typeof(T), ImmutableList<Listener>.Empty, (key, old) =>
            {
                listeners = old
                    .OfType<Listener<T>>()
                    .Where(l => l.Predicate(message))
                    .ToArray();
                return old.RemoveAll(l => listeners.Contains(l));
            });

            // Note that the async continuations, attached to the TaskCompletionSource.Task,
            // could execute synchronously inside of TrySetResult()
            var tasks = listeners.Select(l => CompleteListener(message, l));

            return Task.WhenAll(tasks);
        }

        private static Task<bool> CompleteListener<T>(T message, Listener<T> l)
        {
            return Task.Run(() => l.TaskCompletionSource.TrySetResult(message));
        }

        private class Listener
        {
        }

        private class Listener<T> : Listener
        {
            public Func<T, bool> Predicate { get; }
            public TaskCompletionSource<T> TaskCompletionSource { get; }

            public Listener(Func<T, bool> predicate, TaskCompletionSource<T> taskCompletionSource)
            {
                Predicate = predicate;
                TaskCompletionSource = taskCompletionSource;
            }
        }
    }
}