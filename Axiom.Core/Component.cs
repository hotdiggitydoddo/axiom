using System;

namespace Axiom.Core
{
    public abstract class Component : IComparable<Component>
    {
        public bool Alive { get; set; }
        public Entity Entity { get; set; }
        public int Priority { get; protected set; }

        public int CompareTo(Component other)
        {
            return other.Priority.CompareTo(Priority);
        }

        public abstract void FireEvent(Event e);

        public virtual void Init(Entity e)
        {
            Entity = e;
            Alive = true;
        }

        public virtual void Update(float dt) { }
    }
}
