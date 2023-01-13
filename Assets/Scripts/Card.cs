using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public Colors color;
    public Values value;


   
    public PlayerOwn cardOwner;



    private void OnMouseDown()
    {
        cardOwner.SetLastCardValue((int)value);
        
        transform.position = new Vector2(0, 1f);
       
 
    }
    public void SetCard( Colors newCol, Values newVal)
    {
        this.color = newCol;
        this.value = newVal;
    }

    public void SetPatent(PlayerOwn player, Transform parent)
    {
        this.transform.transform.parent = parent;
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
    fünf,
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
