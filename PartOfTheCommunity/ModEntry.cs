using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;
using System.Collections.Generic;
using StardewValley.Menus;
using StardewValley.Characters;
using StardewValley.Events;
using StardewValley.Locations;

namespace SB_PotC
{

    public class ModEntry : Mod
    {
        string[] storeOwners = new string[] { "Pierre", "Gus", "Clint", "Marnie", "Robin", "Sandy", "Willy" };

        public const int hasTalked = 0;
        public const int recievedGift = 1;
        public const int relationsGifted = 2;
        public const int timesWitnessed = 3;

        public SerializableDictionary<string, SerializableDictionary<string, string>> characterRelationships;
        public SerializableDictionary<string, int[]> witnessCount;
        public SerializableDictionary<string, bool> hasShoppedinStore;
        public bool hasEnteredEvent;
        public int currentNumberOfCompletedBundles;
        public uint currentNumberOfCompletedDailyQuests;
        public bool hasRecentlyCompletedQuest;
        public int daysAfterCompletingLastDailyQuest;
        public int currentUniqueItemsShipped;
        public bool allInitiated;
        ModConfig config;

        public override void Entry(IModHelper helper)
        {

            if (this.characterRelationships == null || this.witnessCount == null)
            {
                if (this.characterRelationships == null)
                    this.characterRelationships = new SerializableDictionary<string, SerializableDictionary<string, string>>();
                if (this.witnessCount == null)
                    this.witnessCount = new SerializableDictionary<string, int[]>();
            }
            GameEvents.UpdateTick += this.ModUpdate;
            SaveEvents.AfterLoad += this.setVariables;
            SaveEvents.BeforeSave += this.EndOfDayUpdate;
            SaveEvents.AfterSave += this.saveConfigFile;
            hasShoppedinStore = new SerializableDictionary<string, bool>();
            hasRecentlyCompletedQuest = false;
            daysAfterCompletingLastDailyQuest = -1;
            allInitiated = false;
        }

        private void saveConfigFile(object sender, EventArgs e)
        {
            Helper.WriteJsonFile($"{Constants.SaveFolderName}/config.json", config);
        }

        private void setVariables(object sender, EventArgs e)
        {
            config = Helper.ReadJsonFile<ModConfig>($"{Constants.SaveFolderName}/config.json") ?? new ModConfig();
            foreach (string name in Game1.player.friendships.Keys)
            {
                CheckRelationshipData(name);
                witnessCount.Add(name, new int[4]);
            }
            currentNumberOfCompletedBundles = (Game1.getLocationFromName("CommunityCenter") as CommunityCenter).numberOfCompleteBundles();
            if (!config.hasGottenInitialUjimaBonus)
            {
                foreach (string storeOwner in storeOwners)
                {
                    if (Game1.getCharacterFromName(storeOwner) == null) continue;
                    Game1.player.changeFriendship((config.ujimaBonus * currentNumberOfCompletedBundles), Game1.getCharacterFromName(storeOwner));
                }
                Monitor.Log(string.Format("You have gained {0} friendship from all store owners for completing {1} {2}",
                    (20 * currentNumberOfCompletedBundles), (currentNumberOfCompletedBundles), currentNumberOfCompletedBundles > 1 ? "Bundles" : "Bundle" ), LogLevel.Info);
                config.hasGottenInitialUjimaBonus = true;
            }
            currentUniqueItemsShipped = Game1.player.basicShipped.Count;
            if (!config.hasGottenInitialKuumbaBonus)
            {
                int friendshipPoints = config.kuumbaBonus * currentUniqueItemsShipped;
                Utility.improveFriendshipWithEveryoneInRegion(Game1.player, friendshipPoints, 2);
                Monitor.Log(string.Format("Gained {0} friendship for shipping {1} unique {2}"
                    , friendshipPoints, currentUniqueItemsShipped, currentUniqueItemsShipped != 1? "items" : "item"), LogLevel.Info);
                config.hasGottenInitialKuumbaBonus = true;
            }
            currentNumberOfCompletedDailyQuests = Game1.stats.questsCompleted;
            allInitiated = true;
        }

        public static List<Character> areThereCharactersWithinDistance(Vector2 tileLocation, int tilesAway, GameLocation environment)
        {
            List<Character> charactersWithinDistance = new List<Character>();
            foreach (Character character in environment.characters)
            {
                if (character != null && (double)Vector2.Distance(character.getTileLocation(), tileLocation) <= (double)tilesAway)
                    charactersWithinDistance.Add(character);
            }
            return charactersWithinDistance;
        }

        /*********
        ** Private methods
        *********/
        private void ModUpdate(object sender, EventArgs e)
        {
            if (!allInitiated) return;
            if (Game1.player == null) return;
            foreach (string name in Game1.player.friendships.Keys.ToArray())
            {
                if (Game1.getCharacterFromName(name, false) == null) continue;
                // if the NPC was divorced by the player, nothing occurs
                if (Game1.player.isDivorced() && Game1.player.spouse.Equals(name)) continue;
                //check if Player gave NPC a gift
                if (Game1.player.friendships[name][3] == 1)
                {
                    if (!witnessCount.ContainsKey(name))
                        witnessCount.Add(name, new int[4]);
                    if (witnessCount[name][recievedGift] < 1)
                    {
                        CheckRelationshipData(name);
                        // if the gift made the reciever decrease their friendship, do nothing, else
                        foreach (string relation in this.characterRelationships[name].Keys.ToArray())
                        {
                            if (string.IsNullOrEmpty(relation)) continue;
                            if (Game1.getCharacterFromName(relation, false) == null) continue;
                            if (!witnessCount.ContainsKey(relation))
                                witnessCount.Add(relation, new int[4]);
                            if (Game1.player.isDivorced() && Game1.getCharacterFromName(relation, false).divorcedFromFarmer) continue;
                            CheckRelationshipData(relation);
                            if (witnessCount[relation][relationsGifted] < this.characterRelationships[relation].Count)
                            {
                                witnessCount[relation][relationsGifted]++;
                            }
                        }
                        witnessCount[name][recievedGift] = 1;
                    }
                }
                //check if player is talking to a NPC
                if (Game1.player.hasTalkedToFriendToday(name))
                {
                    if (!witnessCount.ContainsKey(name))
                        witnessCount.Add(name, new int[4]);
                    if (witnessCount[name][hasTalked] < 1)
                    {
                        CheckRelationshipData(name);
                        List<Character> charactersWithinDistance = areThereCharactersWithinDistance(Game1.player.getTileLocation(), 20, Game1.player.currentLocation);
                        foreach (Character characterWithinDistance in charactersWithinDistance)
                        {
                            if (characterWithinDistance.name == name) continue;
                            if (characterWithinDistance == null) continue;
                            if ((Game1.player.isDivorced() == true) && Game1.player.spouse.Equals(characterWithinDistance.name))
                            {
                                characterWithinDistance.doEmote(12, true);
                            }
                            else
                            {
                                if (!witnessCount.ContainsKey(characterWithinDistance.name))
                                {
                                    witnessCount.Add(characterWithinDistance.name, new int[4]);
                                }
                                witnessCount[characterWithinDistance.name][3]++;
                                if(witnessCount[characterWithinDistance.name][3] != 0 && (witnessCount[characterWithinDistance.name][3] & (witnessCount[characterWithinDistance.name][3] - 1 )) == 0)
                                {
                                    characterWithinDistance.doEmote(32, true);
                                    Game1.player.changeFriendship(config.witnessBonus, (characterWithinDistance as NPC));
                                    this.Monitor.Log(String.Format("{0} saw you taking to a {1}. +{2} Friendship: {0}", characterWithinDistance.name, name, config.witnessBonus), LogLevel.Info);
                                }
                            }
                        }
                        witnessCount[name][hasTalked] = 1;
                    }
                }
            }
            //Check if the player is actively shopping
            //TODO: Add the Bus/Pam
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is ShopMenu)
            {
                Item heldItem = this.Helper.Reflection.GetPrivateValue<Item>(Game1.activeClickableMenu as ShopMenu, "heldItem");
                if (heldItem != null)
                {
                    String shopOwner = "";
                    switch (Game1.currentLocation.name)
                    {
                        case "SeedShop":
                            shopOwner = "Pierre";
                            break;
                        case "AnimalShop":
                            shopOwner = "Marnie";
                            break;
                        case "Blacksmith":
                            shopOwner = "Clint";
                            break;
                        case "FishShop":
                            shopOwner = "Willy";
                            break;
                        case "ScienceHouse":
                            shopOwner = "Robin";
                            break;
                        case "Saloon":
                            shopOwner = "Gus";
                            break;
                        case "Mine":
                            shopOwner = "Dwarf";
                            break;
                        case "SandyHouse":
                            shopOwner = "Sandy";
                            break;
                        case "Sewer":
                            shopOwner = "Krobus";
                            break;
                    }
                    if (!string.IsNullOrEmpty(shopOwner))
                    {
                        if (!hasShoppedinStore.ContainsKey(shopOwner))
                            hasShoppedinStore.Add(shopOwner, false);
                        if ((Game1.getCharacterFromName(shopOwner, false) != null) && hasShoppedinStore[shopOwner] == false)
                        {
                            Game1.player.changeFriendship(config.ujamaaBonus, Game1.getCharacterFromName(shopOwner, false));
                            this.Monitor.Log(String.Format("{0}: Pleasure doing business with you!", shopOwner), LogLevel.Info);
                            hasShoppedinStore[shopOwner] = true;
                        }
                    }
                }
            }
            // Check if the Player has entered a festival
            if (Game1.currentLocation != null && Game1.currentLocation.currentEvent != null && Game1.player.currentLocation.currentEvent.isFestival && hasEnteredEvent == false)
            {
                Utility.improveFriendshipWithEveryoneInRegion(Game1.player, config.umojaBonusFestival, 2);
                foreach (String name in Game1.player.friendships.Keys.ToArray())
                {
                    NPC character = Game1.getCharacterFromName(name);
                    if (character != null && character.currentLocation == Game1.currentLocation)
                    {
                        if ((Game1.player.isDivorced() == true) && Game1.player.spouse.Equals(character.name))
                        {
                            character.doEmote(12, true);
                        }
                        else
                        {
                            character.doEmote(32, true);
                        }
                    }
                }
                Monitor.Log(string.Format("The Villagers Are glad you came!"), LogLevel.Info);
                hasEnteredEvent = true;
            }
            // Check if the Player is getting married or having a baby
            if ((Game1.weddingToday || Game1.farmEvent is BirthingEvent) && hasEnteredEvent == false)
            {
                CheckRelationshipData(Game1.player.spouse);
                SerializableDictionary<string, string> relationships = characterRelationships[Game1.player.spouse];
                foreach (string relation in relationships.Keys.ToArray())
                {
                    if (Game1.getCharacterFromName(relation) == null) continue;
                    if (!relationships[relation].ToLower().Contains("friend"))
                    {
                        Game1.player.changeFriendship(config.umojaBonusMarry, Game1.getCharacterFromName(relation));
                        Monitor.Log(string.Format("{0}: Married into the Family, recieved +{1} friendship", relation, config.umojaBonusMarry), LogLevel.Info);
                    }
                    else
                    {
                        Game1.player.changeFriendship(config.umojaBonusMarry / 2, Game1.getCharacterFromName(relation));
                        Monitor.Log(string.Format("{0}: Married a friend, recieved +{1} friendship", relation, config.umojaBonusMarry /2), LogLevel.Info);
                    }
                }
                hasEnteredEvent = true;
            }
            //Check if the Player recently completed a daily quest
            if ( allInitiated  && Game1.stats.questsCompleted > currentNumberOfCompletedDailyQuests)
            {
                daysAfterCompletingLastDailyQuest = 0;
                currentNumberOfCompletedDailyQuests = Game1.stats.questsCompleted;
                hasRecentlyCompletedQuest = true;
            }
        }

        private void EndOfDayUpdate(object sender, EventArgs e)
        {
            //Gifting, Talking
            foreach (string name in witnessCount.Keys.ToArray())
            {
                if (Game1.getCharacterFromName(name, false) == null) continue;
                if (witnessCount[name][1] > 0)
                {
                    foreach (string relation in characterRelationships[name].Keys.ToArray())
                    {
                        if (string.IsNullOrEmpty(relation)) continue;
                        if (Game1.getCharacterFromName(relation, false) == null) continue;
                        if (witnessCount[relation][2] > 0)
                        {
                            Game1.player.changeFriendship(config.storytellerBonus * witnessCount[relation][2], Game1.getCharacterFromName(relation, false));
                            Monitor.Log(string.Format("{0}: Friendship raised {1} for Gifting to someone {0} loves:", relation, config.storytellerBonus * witnessCount[relation][2]), LogLevel.Info);
                            witnessCount[relation][2] = 0;
                        }
                    }
                    witnessCount[name][1] = 0;
                    witnessCount[name][3] = 0;
                }
                if (((Game1.player.isMarried()  && Game1.player.spouse == name) || (Game1.getCharacterFromName(name) is Child && (Game1.getCharacterFromName(name) as Child).isChildOf(Game1.player))) && witnessCount[name][0] == 1)
                {
                    foreach (string relation in characterRelationships[name].Keys.ToArray())
                    {
                        if (string.IsNullOrEmpty(relation)) continue;
                        if (Game1.getCharacterFromName(relation, false) == null) continue;
                        if (characterRelationships[name][relation] != "Friend" && characterRelationships[name][relation] != "Wartorn")
                        {
                            Game1.player.changeFriendship(config.umojaBonus, Game1.getCharacterFromName(relation, false));
                            Monitor.Log(string.Format("{0}: Friendship raised {1} for loving your family:", relation, config.umojaBonus), LogLevel.Info);

                        }
                    }
                }
                witnessCount[name][0] = 0;
            }
            //Check if New Bundles were completed today
            if (currentNumberOfCompletedBundles < (Game1.getLocationFromName("CommunityCenter") as CommunityCenter).numberOfCompleteBundles())
            {
                int newNumberOfCompletedBundles = (Game1.getLocationFromName("CommunityCenter") as CommunityCenter).numberOfCompleteBundles();
                foreach (string storeOwner in storeOwners)
                {
                    if(Game1.getCharacterFromName(storeOwner) != null)
                        Game1.player.changeFriendship((config.ujimaBonus * newNumberOfCompletedBundles), Game1.getCharacterFromName(storeOwner));
                }
                Monitor.Log(string.Format("You have gained {0} friendship from all store owners for completing {1} Bundle today",
                    (20 * (newNumberOfCompletedBundles - currentNumberOfCompletedBundles)), (newNumberOfCompletedBundles - currentNumberOfCompletedBundles)), LogLevel.Info);
                currentNumberOfCompletedBundles = newNumberOfCompletedBundles;
            }
            //Update the Daily Quest Counters
            if (daysAfterCompletingLastDailyQuest < 3 && hasRecentlyCompletedQuest == true)
            {
                int friendshipPoints = config.ujimaBonus / (int)Math.Pow(2, daysAfterCompletingLastDailyQuest);
                Utility.improveFriendshipWithEveryoneInRegion(Game1.player, friendshipPoints, 2);
                Monitor.Log(string.Format("Gained {0} friendship for recent daily quest completion", friendshipPoints), LogLevel.Info);

            }
            else
            {
                if (daysAfterCompletingLastDailyQuest >= 3)
                {
                    hasRecentlyCompletedQuest = false;
                }
            }
            if (!Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.currentSeason))
            {
                daysAfterCompletingLastDailyQuest++;
            }
            //Check if any new items were shipped
            if(Game1.player.basicShipped.Count > currentUniqueItemsShipped)
            {
                int friendshipPoints = config.kuumbaBonus * (Game1.player.basicShipped.Count - currentUniqueItemsShipped);
                Utility.improveFriendshipWithEveryoneInRegion(Game1.player, friendshipPoints, 2);
                Monitor.Log(string.Format("Gained {0} friendship for shipping items", friendshipPoints), LogLevel.Info);
                currentUniqueItemsShipped = Game1.player.basicShipped.Count;
            }
            //Resetting Miscellaneous Flags
            foreach (string name in hasShoppedinStore.Keys.ToArray())
            {
                hasShoppedinStore[name] = false;
            }
            if (hasEnteredEvent == true) hasEnteredEvent = false;
        }

        private void CheckRelationshipData(string name)
        {
            if (!characterRelationships.ContainsKey(name))
            {
                characterRelationships.Add(name, new SerializableDictionary<string, string>());
                this.Monitor.Log(String.Format("New Entry: {0}", name), LogLevel.Info);
                switch (name)
                {
                    case "Vincent":
                    case "Sam":
                        characterRelationships[name].Add("Jodi", "Mother");
                        characterRelationships[name].Add("Kent", "Father");
                        characterRelationships[name].Add(name == "Vincent" ? "Alex" : "Vincent", "Brother");
                        if (name.Equals("Vincent"))
                        {
                            characterRelationships[name].Add("Jas", "Friend");
                        }
                        else
                        {
                            characterRelationships[name].Add("Abigail", "Friend");
                            characterRelationships[name].Add("Sebastian", "Friend");
                            characterRelationships[name].Add("Penny", "Friend");
                        }
                        return;
                    case "Maru":
                    case "Sebastian":
                        characterRelationships[name].Add("Robin", "Mother");
                        characterRelationships[name].Add("Demetrius", name == "Maru" ? "Father" : "Step-Father");
                        characterRelationships[name].Add(name == "Maru" ? "Sebastian" : "Maru", "Half-" + name == "Maru" ? "Brother" : "Sister");
                        if (name.Equals("Sebastian"))
                        {
                            characterRelationships[name].Add("Abigail", "Friend");
                            characterRelationships[name].Add("Sam", "Friend");
                        }
                        else
                        {
                            characterRelationships[name].Add("Penny", "Friend");
                        }
                        return;
                    case "Dwarf":
                    case "Krobus":
                        characterRelationships[name].Add(name == "Dwarf" ? "Krobus" : "Dwarf", "Wartorn");
                        return;
                    case "Jodi":
                    case "Kent":
                        characterRelationships[name].Add(name == "Jodi" ? "Kent" : "Jodi", name == "Jodi" ? "Husband" : "Wife");
                        characterRelationships[name].Add("Sam", "Son");
                        characterRelationships[name].Add("Vincent", "Son");
                        characterRelationships[name].Add("Caroline", "Friend");
                        return;
                    case "Emily":
                        characterRelationships[name].Add("Haley", "Sister");
                        characterRelationships[name].Add("Sandy", "Friend");
                        characterRelationships[name].Add("Gus", "Friend");
                        characterRelationships[name].Add("Clint", "Friend");
                        characterRelationships[name].Add("Shane", "Friend");
                        return;
                    case "Abigail":
                        characterRelationships[name].Add("Pierre", "Father");
                        characterRelationships[name].Add("Caroline", "Mother");
                        characterRelationships[name].Add("Sebastian", "Friend");
                        characterRelationships[name].Add("Sam", "Friend");
                        return;
                    case "Caroline":
                        characterRelationships[name].Add("Pierre", "Husband");
                        characterRelationships[name].Add("Abigail", "Daughter");
                        characterRelationships[name].Add("Jodi", "Friend");
                        characterRelationships[name].Add("Kent", "Friend");
                        return;
                    case "Alex":
                        characterRelationships[name].Add("George", "Grandfather");
                        characterRelationships[name].Add("Evelyn", "Grandmother");
                        characterRelationships[name].Add("Haley", "Friend");
                        return;
                    case "Demetrius":
                        characterRelationships[name].Add("Robin", "Wife");
                        characterRelationships[name].Add("Maru", "Daughter");
                        characterRelationships[name].Add("Sebastian", "Step-Son");
                        return;
                    case "Jas":
                        characterRelationships[name].Add("Marnie", "Aunt");
                        characterRelationships[name].Add("Shane", "Godfather");
                        characterRelationships[name].Add("Vincent", "Friend");
                        return;
                    case "Marnie":
                        characterRelationships[name].Add("Shane", "Nephew");
                        characterRelationships[name].Add("Jas", "Neice");
                        characterRelationships[name].Add("Lewis", "Frind");
                        return;
                    case "Penny":
                        characterRelationships[name].Add("Pam", "Mother");
                        characterRelationships[name].Add("Sam", "Friend");
                        characterRelationships[name].Add("Maru", "Friend");
                        return;
                    case "Robin":
                        characterRelationships[name].Add("Demetrius", "Husband");
                        characterRelationships[name].Add("Maru", "Daughter");
                        characterRelationships[name].Add("Sebastian", "Son");
                        return;
                    case "Shane":
                        characterRelationships[name].Add("Marnie", "Aunt");
                        characterRelationships[name].Add("Jas", "Goddaughter");
                        characterRelationships[name].Add("Emily", "Friend");
                        return;
                    case "Elliott":
                        characterRelationships[name].Add("Willy", "Friend");
                        characterRelationships[name].Add("Leah", "Friend");
                        return;
                    case "Evelyn":
                        characterRelationships[name].Add("George", "Husband");
                        characterRelationships[name].Add("Alex", "Son");
                        return;
                    case "George":
                        characterRelationships[name].Add("Evelyn", "Husband");
                        characterRelationships[name].Add("Alex", "Son");
                        return;
                    case "Gus":
                        characterRelationships[name].Add("Pam", "Friend");
                        characterRelationships[name].Add("Emily", "Friend");
                        return;
                    case "Haley":
                        characterRelationships[name].Add("Emily", "Sister");
                        characterRelationships[name].Add("Alex", "Friend");
                        return;
                    case "Pam":
                        characterRelationships[name].Add("Penny", "Daughter");
                        characterRelationships[name].Add("Gus", "Friend");
                        return;
                    case "Pierre":
                        characterRelationships[name].Add("Caroline", "Wife");
                        characterRelationships[name].Add("Abigail", "Daughter");
                        return;
                    case "Clint":
                        characterRelationships[name].Add("Emily", "Admire");
                        return;
                    case "Leah":
                        characterRelationships[name].Add("Elliott", "Friend");
                        return;
                    case "Lewis":
                        characterRelationships[name].Add("Marnie", "Frind");
                        return;
                    case "Sandy":
                        characterRelationships[name].Add("Emily", "Friend");
                        return;
                    case "Willy":
                        characterRelationships[name].Add("Elliott", "Friend");
                        return;
                    default:
                        break;
                }
                //Check for Relationships of your children
                if (Game1.getCharacterFromName(name) is Child && (Game1.getCharacterFromName(name) as Child).isChildOf(Game1.player))
                {
                    CheckRelationshipData(Game1.player.spouse);
                    characterRelationships[name].Add(Game1.player.spouse, Utility.isMale(Game1.player.spouse) ? "Father" : "Mother");
                    characterRelationships[Game1.player.spouse].Add(name, Utility.isMale(name) ? "Son" : "Daughter");
                    foreach (string relation in characterRelationships[Game1.player.spouse].Keys.ToArray())
                    {
                        if (relation == name) continue;
                        this.CheckRelationshipData(relation);
                        switch (characterRelationships[Game1.player.spouse][relation])
                        {
                            case "Grandfather":
                                characterRelationships[name].Add(relation, "Great-Grandfather");
                                characterRelationships[relation].Add(name, Utility.isMale(name) ? "Great-Grandson" : "Great-Granddaughter");
                                break;
                            case "Grandmother":
                                characterRelationships[name].Add(relation, "Great-Grandmother");
                                characterRelationships[relation].Add(name, Utility.isMale(name) ? "Great-Grandson" : "Great-Granddaughter");
                                break;
                            case "Father":
                            case "Step-Father":
                                characterRelationships[name].Add(relation, "Grandfather");
                                characterRelationships[relation].Add(name, Utility.isMale(name) ? "Grandson" : "Granddaughter");
                                break;
                            case "Mother":
                                characterRelationships[name].Add(relation, "Grandmother");
                                characterRelationships[relation].Add(name, Utility.isMale(name) ? "Grandson" : "Granddaughter");
                                break;
                            case "Brother":
                            case "Step-Brother":
                            case "Half-Brother":
                                characterRelationships[name].Add(relation, "Uncle");
                                characterRelationships[relation].Add(name, Utility.isMale(name) ? "Nephew" : "Niece");
                                break;
                            case "Sister":
                            case "Step-Sister":
                            case "Half-Sister":
                                characterRelationships[name].Add(relation, "Aunt");
                                characterRelationships[relation].Add(name, Utility.isMale(name) ? "Nephew" : "Niece");
                                break;
                            case "Niece":
                            case "Nephew":
                                characterRelationships[name].Add(relation, "Cousin");
                                characterRelationships[relation].Add(name, "Cousin");
                                break;
                            case "Son":
                            case "Daughter":
                                characterRelationships[name].Add(relation, Utility.isMale(relation) ? "Brother" : "Sister");
                                characterRelationships[relation].Add(name, Utility.isMale(name) ? "Brother" : "Sister");
                                break;
                            default:
                                break;
                        }
                    }
                    // Check for the other children. They might be from another marriage
                    foreach (Child getChildren in Game1.player.getChildren())
                    {
                        if (getChildren.name != name && !characterRelationships[name].ContainsKey(getChildren.name))
                        {
                            characterRelationships[name].Add(getChildren.name, "Half-" + (Utility.isMale(getChildren.name) ? "Brother" : "Sister"));
                            characterRelationships[getChildren.name].Add(name, "Half-" + (Utility.isMale(getChildren.name) ? "Brother" : "Sister"));
                        }
                    }
                }
            }
        }

        public bool hasPlayerKissedWife()
        {
            return this.Helper.Reflection.GetPrivateValue<bool>(Game1.player.getSpouse(), "hasBeenKissedToday");
        }
        //When the Farmer's child is 0-1 years old, this is in lieu of talking to the tyke.
        public bool hasPlayerTossChild()
        {
            return true;
        }
    }
}