using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Util
{
    [Header("DeckInfo")]
    [SerializeField] static List<Colors> colorsAvaliable = new List<Colors> { Colors.club, Colors.spade, Colors.heart, Colors.diamond };
    [SerializeField] static List<Values> valuesAvaliable = new List<Values> { Values.sieben, Values.acht, Values.neun, Values.zehn, Values.jack, Values.queen, Values.king, Values.ass};


    public static List<NetworkCard> GetNetworkDeck()
    {
        List<NetworkCard> deck_ = new List<NetworkCard>();
        colorsAvaliable.ForEach(col => valuesAvaliable.ForEach(val => deck_.Add(new NetworkCard((int)col, (int)val))));
        return deck_;
    }
}
