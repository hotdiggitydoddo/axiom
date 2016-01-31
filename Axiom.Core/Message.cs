using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axiom.Core
{
    public class Message : IComparable<Message>
    {
        public DateTime DispatchTime { get; set; }
        public string[] Args { get; set; }
        public bool Recurring { get; set; }
        public int Source { get; set; }
        public int Target { get; set; }
        public int Tool { get; set; }
        public string MessageType { get; set; }
        public string SourceMsg { get; set; }
        public string TargetMsg { get; set; }
        public string OthersMsg { get; set; }
        public object Value { get; set; }
        public double Delay { get; set; }


        public Message() { }
        public Message(int source, int target, int tool, string messageType, double delay = 0, bool recurring = false, params string[] args)
        {
            Source = source;
            Target = target;
            Tool = tool;
            MessageType = messageType;
            Delay = delay;
            Recurring = recurring;
            Args = args;
        }

        public int CompareTo(Message other)
        {
            if (DispatchTime < other.DispatchTime) return -1;
            if (DispatchTime > other.DispatchTime) return 1;
            return 0;
        }
    }

    [Flags]
    public enum MessageType
    {
        Attack,
        Cast,
        Move,
        Say,
        Yell,
        EnterRoom,
        ExitRoom,
        AdjustHealth,
        AddedComponent,
        RemovedComponent,
        AddedToWorld,
        RemovedFromWorld,
        AddedToRoom,
        RemovedFromRoom
    }
}
