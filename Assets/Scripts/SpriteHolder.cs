using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static GameManager;

public class SpriteHolder : NetworkBehaviour
{
    public static SpriteHolder sh;

    public List<Sprite> colorSprites = new List<Sprite>();
    public List<Sprite> valueSprites = new List<Sprite>();

    public GameObject cardPrefab;

    public int cardsAmount;

    public int cardsValue;

    public List<GameObject> cardGos;

    public List<Sprite> winLooseImageSprites;
    public SpriteRenderer winLooseImage;

    [Range(0,3)]
    public float spacing;

    public NetworkVariable<int> winnerCounter = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        if (sh == null) sh = this;   
    }

  
    public void SetSpritesPosition(Player player)
    {
        float padding = 0;
        float offSet = 0;
        
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
        List<Colors> newColors = ParseNetworkCols(col);
        for (int i = 0; i < amount; i++)
        {   
            GameObject go = Instantiate(cardPrefab, new Vector2(4.4f+i+spacing+(Random.Range(0.5f,0.5f)), 4f+(Random.Range(-0.3f, 0.5f))), Quaternion.Euler(0.0f, 0.0f, Random.Range(-10f, 10f)));
            Card card = go.GetComponent<Card>();

            card.valueRenderer.sortingOrder -= i;
            card.colorRenderer.sortingOrder -= i;
            card.cardRenderer.sortingOrder -= i;

            card.isPlayed = true;

            card.color = newColors[i];

            card.ownerId = GameManager.gM.currentPlayerId.Value;
            card.value = (Values)value;
            cardGos.Add(go);
        }
    }

    [ClientRpc]
    public void ResetCardsInMiddleClientRpc()
    {
        for (int i = 0; i < cardGos.Count; i++)
        {
            Destroy(cardGos[i]);
        }
        cardGos.Clear();
    }

    [ClientRpc]
    public void SetWinLooseImageClientRpc()
    {
        Dictionary<int,ulong> dict_ = GameManager.gM.GetPlacements();
        winLooseImage.sprite = winLooseImageSprites[winnerCounter.Value];
        winnerCounter.Value++;
    }

} //
