using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems
{
    public abstract class SharedAgentIdCardSystem : EntitySystem
    {
    }

    [Serializable, NetSerializable]
    public enum AgentIDCardUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class AgentIDCardBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string CurrentName { get; }
        public string CurrentJob { get; }
        public string CurrentJobIconId { get; }
        public uint? CurrentNumber { get; }

        public AgentIDCardBoundUserInterfaceState(string currentName, string currentJob, string currentJobIconId, uint? currentNumber = null)
        {
            CurrentName = currentName;
            CurrentJob = currentJob;
            CurrentJobIconId = currentJobIconId;
            CurrentNumber = currentNumber;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AgentIDCardNumberChangedMessage : BoundUserInterfaceMessage
    {
        public uint Number { get; }

        public AgentIDCardNumberChangedMessage(uint number)
        {
            Number = number;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AgentIDCardNameChangedMessage : BoundUserInterfaceMessage
    {
        public string Name { get; }

        public AgentIDCardNameChangedMessage(string name)
        {
            Name = name;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AgentIDCardJobChangedMessage : BoundUserInterfaceMessage
    {
        public string Job { get; }

        public AgentIDCardJobChangedMessage(string job)
        {
            Job = job;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AgentIDCardJobIconChangedMessage : BoundUserInterfaceMessage
    {
        public ProtoId<JobIconPrototype> JobIconId { get; }

        public AgentIDCardJobIconChangedMessage(ProtoId<JobIconPrototype> jobIconId)
        {
            JobIconId = jobIconId;
        }
    }
}
