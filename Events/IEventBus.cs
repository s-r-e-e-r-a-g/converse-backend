using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Converse.Events
{
    public interface IEventBus
    {
        void Subscribe<TEvent>(Func<TEvent, Task> handler);
        Task Publish<TEvent>(TEvent eventData);
    }
}
