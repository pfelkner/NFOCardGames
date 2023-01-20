using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static GameManager;

public class SpriteHolder : NetworkBehaviour
{
    public static SpriteHolder sP;

    public List<Sprite> colorSprites = new List<Sprite>();
    public List<Sprite> valueSprites = new List<Sprite>();

    public GameObject cardInMiddle;

    public int cardsAmount;

    public int cardsValue;

    public List<GameObject> goS;

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
    [ClientRpc]
    public void SetCardInMiddleClientRpc(int amount, int value, NetworkColors col)
    {
        Debug.LogWarning($"Inside set in middle {GameManager.gM.lastCardPlayedAmount.Value};");
        List<int> colors = new List<int> {col.club,col.spade, col.heart,col.diamond};
        for (int i = 0; i < amount; i++)
        {   
            GameObject go = Instantiate(cardInMiddle, new Vector2(7.4f+i+0.5f, 4f), Quaternion.identity);
            Card card = go.GetComponent<Card>();
            if (colors[i] >= 0)
            {
                card.color = (Colors)colors[i];
            }
            
            Debug.LogWarning($"cast{(Values)cardsValue} :{cardsValue}");
            card.value = (Values)value;
            goS.Add(go);
        }
    }
    [ClientRpc]
    public void SetCardsBackClientRpc()
    {
        for (int i = 0; i < goS.Count; i++)
        {
            Destroy(goS[i]);
        }
        goS.Clear();
    }


} //
