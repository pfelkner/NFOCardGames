using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card
{

    public Colors color;
    public Values value;

    public Card(Colors col, Values val)
    {
        color = col;
        value = val;
    }

}


public enum Colors
{
    club,
    spade,
    heart,
    diamond
}
public enum Values
{
    sechs,
    sieben,
    acht,
    neun,
    zehn,
    jack,
    queen,
    king,
    ass
}
