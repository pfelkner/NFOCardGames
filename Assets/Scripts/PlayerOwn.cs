using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PlayerOwn : NetworkBehaviour
{
    public List<Card> cardsInHand;

    private void OnEnable()
    {
        GameManager.gM.players.Add(this);
    }


    public void SetPlayer(List<Card> list)
    {
        cardsInHand = list;
        float spacing = 0;
        foreach(Card card in list)
        {
            GameObject created = Instantiate(card.gameObject,new Vector2(-3+spacing,-3),Quaternion.identity);
            created.GetComponent<Card>().SetPatent(this, this.transform);
            spacing++;
        }
      
    }
    
    public void SetLastCardValue(int valueChange)
    {
        if (IsOwner)
        {

        }
    }
    public List<Card> PlayerMove(Card topCard, int reps)
    {
        // returns list of played cards | emptyList = "pass"

        List<Card> cardsToPlay = new List<Card>();
        int indexOfCard = GetIndexOfHigher(topCard);
        if (indexOfCard >= 0)
        {
            if(reps >= 2)
            {
                List<Card> temp = CheckCountePart(cardsInHand[indexOfCard]);
                if(temp.Count == reps)
                {
                    cardsToPlay = new List<Card>(temp);
                    foreach (var item in cardsToPlay)
                    {
                        cardsInHand.Remove(item);
                    }
                }

            }
            else
            {
                cardsToPlay.Add(cardsInHand[indexOfCard]);
                cardsInHand.RemoveAt(indexOfCard);
            }

            if (cardsInHand.Count <= 0)
            {
                Debug.Log(transform.name + " won!");
            }
            Debug.Log("played" + cardsInHand[0].gameObject.name);
            return cardsToPlay;
        } else {
            Debug.Log("pass");
            return new List<Card>();
        }
    }

    private int GetIndexOfHigher(Card topCard)
    {
        int inx = 0;
        foreach (Card item in cardsInHand)
        {
            if ((int)item.value >(int)topCard.value)
            {
                return inx;
            }
            else inx++;

        }
        return -1;
    }
    private List<Card> CheckCountePart(Card _topcard)
    {
      return cardsInHand.FindAll(card=>(int)card.value  == (int)_topcard.value);
    }

    private void CheckCard(PlayerOwn _player)
    {
        GameManager _gM = GameManager.gM;
        Card topCard = GameManager.gM.lastCardPlayed[0];
        List<Card> moveCards = _player.PlayerMove(topCard, _gM.lastCardPlayed.Count);
        if (moveCards.Count == 0) return;
         _gM.lastCardPlayed.Clear();
        foreach (var item in moveCards)
        {
           _gM.playedCardList.Add(item);
           _gM.lastCardPlayed.Add(item);
        }
    }

    public void ChangeLastPlayed(int value)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // server
            GameManager.gM.lastCardPlayedValue.Value = value;
        }
        else
        {
            // client
            SubmitLastCardValueServerRPC();
        }
    }

    [ServerRpc]
    public void SubmitLastCardValueServerRPC()
    {
       // GameManager.gM.lastCardPlayedValue.Value = 
    }

}
