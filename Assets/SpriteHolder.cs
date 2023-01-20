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

    public List<Colors> ParseNetworkCols(NetworkColors cols)
    {
        List<Colors> colors = new List<Colors>();
        if (cols.club)
            colors.Add(Colors.club);
        if (cols.spade)
            colors.Add(Colors.spade);
        if (cols.heart)
            colors.Add(Colors.heart);
        if (cols.diamond)
            colors.Add(Colors.diamond);

        return colors;
    }

    [ClientRpc]
    public void SetCardInMiddleClientRpc(int amount, int value, NetworkColors col)
    {
        //Debug.LogWarning($"Inside set in middle {GameManager.gM.lastCardPlayedAmount.Value};");
        Debug.LogWarning($"###### {ParseNetworkCols(col).Count == amount}########");
        List<Colors> test = ParseNetworkCols(col);
        for (int i = 0; i < amount; i++)
        {   
            GameObject go = Instantiate(cardInMiddle, new Vector2(7.4f+i+0.5f, 4f), Quaternion.identity);
            Card card = go.GetComponent<Card>();

            card.color = test[i];
           
            
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
