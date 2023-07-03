using Dan200.Core.Main;
using System;
using System.IO;

namespace Dan200.Core.Physics
{
    [Flags]
    internal enum CollisionGroup
    {
        Environment = 1,
        Prop = 2,
        Player = 4,
        NPC = 8,
		PlayerTrigger = 16,
        PropTrigger = 32,
        Connector = 64,
        EditorSelectable = 128,
    }

    internal static class CollisionGroupExtensions
    {
        public static CollisionGroup GetColliders(this CollisionGroup _group)
        {
            switch (_group)
            {
                case CollisionGroup.Environment:
                    return CollisionGroup.Prop | CollisionGroup.Player | CollisionGroup.NPC;
                case CollisionGroup.Prop:
                    return CollisionGroup.Environment | CollisionGroup.Prop | CollisionGroup.Player | CollisionGroup.NPC | CollisionGroup.PropTrigger;
                case CollisionGroup.Player:
                    return CollisionGroup.Environment | CollisionGroup.Prop | CollisionGroup.Player | CollisionGroup.NPC | CollisionGroup.PlayerTrigger;
                case CollisionGroup.NPC:
                    return CollisionGroup.Environment | CollisionGroup.Prop | CollisionGroup.Player | CollisionGroup.NPC;
                case CollisionGroup.PlayerTrigger:
                    return CollisionGroup.Player;
                case CollisionGroup.PropTrigger:
                    return CollisionGroup.Prop;
                case CollisionGroup.Connector:
                    return 0;
                case CollisionGroup.EditorSelectable:
                    return 0;
                default:
                    App.Assert(false, "No colliders specified for collision group " + _group);
                    return 0;
            }
        }

        static CollisionGroupExtensions()
        {
            // Save some headaches by ensuring colliders are symetrical
            foreach (CollisionGroup group1 in Enum.GetValues(typeof(CollisionGroup)))
            {
                var group1Colliders = group1.GetColliders();
                foreach (CollisionGroup group2 in Enum.GetValues(typeof(CollisionGroup)))
                {
                    var group2Colliders = group2.GetColliders();
                    if (((group1Colliders & group2) != 0) && ((group2Colliders & group1) == 0))
                    {
                        throw new InvalidDataException("Collision group " + group1 + " collides with " + group2 + " but the inverse is not true!");
                    }
                }
            }
        }
    }
}
