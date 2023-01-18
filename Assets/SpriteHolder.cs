using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteHolder : MonoBehaviour
{
    public static SpriteHolder sP;

    public List<Sprite> colorSprites = new List<Sprite>();
    public List<Sprite> valueSprites = new List<Sprite>();

    private void Awake()
    {
        if (sP == null) sP = this;   
    }

  
    public void SetSpritesPosition(Player player)
    {
        float padding = 0;
        float offSet = 0;
        
            Debug.Log($"{player.name} is local: {player.IsLocalPlayer}");
            if (player.IsLocalPlayer)
            {

                for (int i = 0; i < player.cardsInHand.Count; i++)
                {
                    player.cardsInHand[i].transform.position = new Vector2(-5f+padding,-1.5f+offSet);
                    padding += 3f;
                }
                offSet += 3;

            }

    }



}
