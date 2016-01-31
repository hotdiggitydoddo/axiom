using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axiom.Core
{
    public class PostOffice : PriorityQueue<Message>
    {
        private static PostOffice _instance;
        private Dictionary<string, List<Module>> _onMessage;

        public PostOffice()
        {
            _onMessage = new Dictionary<string, List<Module>>();
        }

        public static PostOffice Instance
        {
            get { return _instance ?? (_instance = new PostOffice()); }
        }

        public void RegisterForMessage(string messageName, Module module)
        {
            if (_onMessage.ContainsKey(messageName))
                _onMessage[messageName].Add(module);
            else
                _onMessage.Add(messageName, new List<Module> { module });
        }

        public void AddMessage(Message msg)
        {
            msg.DispatchTime = DateTime.Now.AddSeconds(msg.Delay);
            Enqueue(msg);
        }

        public void DispatchMessages()
        {
            while (Count() > 0)

                while (Peek().DispatchTime <= DateTime.Now)
                {
                    var msg = Dequeue();

                    if (_onMessage.ContainsKey(msg.MessageType))
                        _onMessage[msg.MessageType].ForEach(x => x.HandleMessage(msg));

                    if (msg.Recurring)
                    {
                        msg.DispatchTime = DateTime.Now.AddSeconds(msg.Delay);
                        Enqueue(msg);
                    }
                    break;
                }
        }
    }
}
