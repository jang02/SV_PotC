using System.Collections.Generic;

namespace PartOfTheCommunity.Framework
{
    /// <summary>Tracked data for an NPC.</summary>
    internal class CharacterInfo
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The NPC name.</summary>
        public string Name { get; }

        /// <summary>Whether the NPC is male.</summary>
        public bool IsMale { get; }

        /// <summary>Whether the NPC owns a shop.</summary>
        public bool IsShopOwner { get; set; }

        /// <summary>Whether the player talked to this NPC today.</summary>
        public bool HasTalked { get; set; }

        /// <summary>Whether the player gifted this NPC today.</summary>
        public bool ReceivedGift { get; set; }

        /// <summary>Whether the player shopped at the NPC's store today.</summary>
        public bool HasShopped { get; set; }

        /// <summary>The number of NPCs this character saw the player talk to nearby.</summary>
        public int NearbyTalksSeen { get; set; }

        /// <summary>The NPC's relationships with other NPCs.</summary>
        public IList<CharacterRelationship> Relationships { get; } = new List<CharacterRelationship>();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="isMale">Whether the NPC is male.</param>
        /// <param name="name">The NPC name.</param>
        public CharacterInfo(string name, bool isMale)
        {
            this.Name = name;
            this.IsMale = isMale;
        }

        /// <summary>Add a relationship to another NPC.</summary>
        /// <param name="relationship">The target character's relationship to the original character (like 'Mother').</param>
        /// <param name="character">The target character.</param>
        public void AddRelationship(string relationship, CharacterInfo character)
        {
            this.Relationships.Add(new CharacterRelationship(relationship, character));
        }
    }
}
