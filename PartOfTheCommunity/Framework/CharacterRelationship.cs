namespace PartOfTheCommunity.Framework
{
    /// <summary>Tracked data for an NPC relationship.</summary>
    internal class CharacterRelationship
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The target character.</summary>
        public CharacterInfo Character { get; set; }

        /// <summary>The target character's relationship to the original character (like 'Mother').</summary>
        public string Relationship { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="relationship">The target character's relationship to the original character (like 'Mother').</param>
        /// <param name="character">The target character.</param>
        public CharacterRelationship(string relationship, CharacterInfo character)
        {
            this.Relationship = relationship;
            this.Character = character;
        }
    }
}
