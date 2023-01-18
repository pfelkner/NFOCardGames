using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public Colors color;
    public Values value;


   
    public Player cardOwner;


    // visuals
    public SpriteRenderer colorRenderer;
    public SpriteRenderer valueRenderer;

    private Vector2 oGPos;
    private bool isSelected;


    


    private void OnEnable()
    {
        Invoke("SetToSprite", 1f);

    }

    private void OnMouseDown()
    {

        

        if (!isSelected && cardOwner.IsValidCard(this))
        {
            SelectCard();
        }
        else if (isSelected)
        {
            DeSelectCard();
        }

    }
    //invoked
    public void SetToSprite()
    {
        int inx = (int)color;
        colorRenderer.sprite = SpriteHolder.sP.colorSprites[inx];
        inx = (int)value;
        valueRenderer.sprite = SpriteHolder.sP.valueSprites[inx];
    }

    private void SelectCard()
    {
        oGPos = transform.position;
        transform.position = oGPos + Vector2.up*2;
        isSelected = true;
        cardOwner.selectedCards.Add(this);
    }

    public void DeSelectCard()
    {
        transform.position = oGPos;
        cardOwner.selectedCards.Remove(this);
        isSelected = false;
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
