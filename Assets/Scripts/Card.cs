using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public Colors color;
    public Values value;


   
    public Player cardOwner;

    //public Card(Colors color, Values value, Player player)
    //{
    //    this.color = color;
    //    this.value = value;
    //    cardOwner = player;
    //}


    private void OnMouseDown()
    {
        
        
        transform.position = new Vector2(0, 1f);

        
        //Destroy(this);
       
 
    }
    public void SetCard( Colors newCol, Values newVal)
    {
        this.color = newCol;
        this.value = newVal;
    }

    public void SetPatent(Player player, Transform parent)
    {
        transform.parent = parent;
        cardOwner = player;
    }

    internal void SetCard(Colors diamond, Values king, Player player)
    {
        this.color = diamond;
        this.value = king;
        cardOwner = player;
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
    fuenf,
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
