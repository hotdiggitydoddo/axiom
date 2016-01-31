using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axiom.Core
{
    public abstract class Component
    {
        public Entity Owner { get; set; }

        public virtual bool OkMessage(Message message) { return true; }
    }

    public interface IUpdateable
    {
        void Update(double dt);
    }
}
