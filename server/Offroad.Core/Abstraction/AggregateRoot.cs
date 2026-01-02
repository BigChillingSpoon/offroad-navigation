using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Offroad.Core.Abstraction
{
    public abstract class AggregateRoot<TId>
    {
        public TId Id { get; protected set; }

        private readonly List<IDomainEvent> _domainEvents = new();

        protected AggregateRoot() { }

        protected AggregateRoot(TId id)
        {
            Id = id;
        }

        public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

        public void ClearDomainEvents() => _domainEvents.Clear();

        protected void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }
    }
}
