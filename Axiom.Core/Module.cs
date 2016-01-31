using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axiom.Core
{
    public abstract class Module
    {
        public Module(params string[] relevantMessages)
        {
            foreach (var relevantMessage in relevantMessages)
                PostOffice.Instance.RegisterForMessage(relevantMessage, this);
        }

        public virtual void HandleMessage(Message message) { }

        public virtual void OnEntityAdded(Entity entity) { }
    }
}
