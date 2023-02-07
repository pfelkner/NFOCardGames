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
    public ulong ownerId;

    // visuals
    public SpriteRenderer colorRenderer;
    public SpriteRenderer valueRenderer;
    public SpriteRenderer cardRenderer;



    private Vector2 oGPos;
    private bool isSelected;

    public bool isPlayed;

    private void OnEnable()
    {
        Invoke("SetToSprite", 0.3f);
    }

    private void OnMouseDown()
    {
        if (isPlayed || (!GameManager.gM.GetLocalPlayer().IsCurrentPlayer())) return;

        if (!isSelected && cardOwner.IsValidCard(this))
            SelectCard();
        else if (isSelected)
            Deselect();
    }
    //invoked
    public void SetToSprite()
    {
        cardRenderer.enabled = true;
        colorRenderer.sprite = SpriteHolder.sh.colorSprites[(int)color];
        valueRenderer.sprite = SpriteHolder.sh.valueSprites[(int)value];
    }

 

    private void SelectCard()
    {
        oGPos = transform.position;
        transform.position = oGPos + Vector2.up*2;
        isSelected = true;
        cardOwner.selectedCards.Add(this);
    }

    public void Deselect()
    {
        transform.position = oGPos;
        isSelected = false;
        cardOwner.selectedCards.Remove(this);
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
