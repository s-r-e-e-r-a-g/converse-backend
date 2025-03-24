using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Converse.Models;

namespace Converse.Events
{
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();

        public void Subscribe<TEvent>(Func<TEvent, Task> handler)
        {
            if (!_handlers.ContainsKey(typeof(TEvent)))
                _handlers[typeof(TEvent)] = new List<Func<object, Task>>();

            _handlers[typeof(TEvent)].Add(eventData => handler((TEvent)eventData));
        }

        public async Task Publish<TEvent>(TEvent eventData)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var eventHandlers))
            {
                foreach (var handler in eventHandlers)
                {
                    await handler(eventData);
                }
            }
        }
    }
    public class MessageMarkedAsReadEvent(string arg1, string arg2);
    public class MessageDeliveredEvent(string arg1);
    public class MessageSavedEvent(MessageData arg1);
    public class GroupMessageSentEvent(string arg1, string arg2, string arg3);
    public class MessageSentEvent(string arg1, string arg2, string arg3);

}