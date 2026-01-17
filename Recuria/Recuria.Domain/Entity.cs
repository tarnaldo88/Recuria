using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    public abstract class Entity
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void RaiseDomainEvent(IDomainEvent @event)
        {
            _domainEvents.Add(@event);
        }

        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
