// Learn more : https://mirror-networking.com/docs/Guides/DataTypes.html#scriptable-objects
using System;
using UnityEngine;
using Mirror;
using System.Collections.Generic;

[Serializable]
public partial struct CardInfo
{
    // A uniqueID (unique identifier) used to help identify which ScriptableCard is which when we acess ScriptableCard data.
    // If any ScriptableCards share the same uniqueID, Unity will return a bunch of errors.
    public string cardID;
    public int amount; // Used for deck building only. Serves no purpose once the card is in the game / on the board.

    public int strength => data is CreatureCard ? ((CreatureCard)data).strength : 0;
    public int health => data is CreatureCard ? ((CreatureCard)data).health : 0;
    public int price => data is CreatureCard ? ((CreatureCard)data).cost : 0;

    public CardInfo(ScriptableCard data, int amount = 1)
    {
        cardID = data.CardID;
        this.amount = amount;
    }

    public ScriptableCard data
    {
        get
        {
            // Return ScriptableCard from our cached list, based on the card's uniqueID.
            return ScriptableCard.Cache[cardID];
        }
    }

    public Sprite image => data.image;
    public string name => data.cardName; // Scriptable Card name (name of the file)
    public string cost => data.cost.ToString();
    public string description => data.description;

    public List<Target> acceptableTargets => ((CreatureCard)data).acceptableTargets;
}

// Card List
public class SyncListCard : SyncList<CardInfo> { }