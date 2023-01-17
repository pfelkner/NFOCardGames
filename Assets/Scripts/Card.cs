using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Card : MonoBehaviour
{
    public Colors color;
    public Values value;


   
    public Player cardOwner;


    // visuals
    public SpriteRenderer colorRenderer;
    public SpriteRenderer valueRenderer;

  


    private void OnEnable()
    {
        Invoke("SetToSprite", 1f);
    }

    private void OnMouseDown()
    {
        transform.position = new Vector2(0, 1f);
    }

    public void SetToSprite()
    {
        int inx = (int)color;
        colorRenderer.sprite = SpriteHolder.sP.colorSprites[inx];
        inx = (int)value;
        valueRenderer.sprite = SpriteHolder.sP.valueSprites[inx];
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
