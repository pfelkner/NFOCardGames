using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public List<Card> cardsInHand;
    private List<int> cardValues = new List<int>();
    [SerializeField]
    private NetworkVariable<Values> value = new NetworkVariable<Values>();
    [SerializeField]
    private NetworkVariable<Colors> color = new NetworkVariable<Colors>();
    //[SerializeField]
    //private Card cardInHand = new Card();
    [SerializeField]
    public static List<int> cards = new List<int>();

    private void OnEnable()
    {
        GameManager.gM.players.Add(this);
    }

    private void Update()
    {
        // testing
        if (Input.GetKeyDown(KeyCode.T) && IsServer)
        {
            //DealCards();
            DealCard();

        }
        if (Input.GetKeyDown(KeyCode.U) && IsServer)
        {
            //DealCards();
            DealCard();

        }
    }

    private void DealCard()
    {
        if (IsServer)
            UpdateServer();
        else if (IsClient && IsOwner)
            UpdateClientRpc();
    }

    private void UpdateServer()
    {
        
        int rnd = UnityEngine.Random.Range(0, 8);
        cards.Add(rnd);
        //int rnd2 = UnityEngine.Random.Range(0, 8);
        //cards.Add(rnd2);

        Debug.Log($"Updating Server with card values: {rnd}");
        //foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
        //{
        //    Debug.Log($"Updating client with Id: {uid}");
        //    NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>().value.Value = (Values)cards[index];
        //    NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>().color.Value = (Colors)3; ;
        //    index++;
        //}

        color.Value = (Colors)3;
        value.Value = (Values)rnd;
    }

    [ClientRpc]
    private void UpdateClientRpc()
    {
        Debug.Log($"Owner: {OwnerClientId} is Updating Client");
        UpdateClientServerRpc(3, (int)value.Value);
        GameManager.gM.text.text = $"Card color: {color.Value}; Card value: {value.Value}";
    }

    [ServerRpc]
    public void UpdateClientServerRpc(int newColor, int newValue)
    {
        Debug.Log("Updating Client from Server with color "+ (Colors)newColor+" and value "+ (Values)newValue);
        value.Value = (Values)newValue;
        color.Value = (Colors)newColor;
        GameManager.gM.text.text = $"Card color: {color.Value}; Card value: {value.Value}";
    }




    //--------------------------------------------------------------------------------

    [ClientRpc]
    public void UpdatePlayersClientRpc(int card)
    {
        //this.players = players;
        Debug.Log(card);
        cardValues.Add(card);

    }

    public void GetCards(int maxcards)
    {
        while (cardsInHand.Count < maxcards)
        {
            Debug.Log(OwnerClientId + " has: " + cardsInHand + " cards");
            Debug.Log("Amount of cards left in deck: " + GameManager.createdCardsList.Count);
            cardsInHand.Add(GameManager.createdCardsList[0]);
            //UpdatePlayersClientRpc((int)createdCardsList[0].value);
            UpdatePlayersClientRpc((int)GameManager.createdCardsList[0].value);
            GameManager.createdCardsList.RemoveAt(0);
        }
        SetPlayer(cardsInHand);
        CreateCardsInHandClientRpc();
    }


    public void SetPlayer(List<Card> list)
    {
        //cardsInHand = list;
        List<Card> tmp = new List<Card>();
        float spacing = 0;
        foreach(Card card in list)
        {
            GameObject created = Instantiate(card.gameObject,new Vector2(-3+spacing,-3),Quaternion.identity);
            created.GetComponent<Card>().SetPatent(this, this.transform);
            spacing++;
            tmp.Add(created.GetComponent<Card>());
        }
        cardsInHand = tmp;
    }
    
    public void SetLastCardValue(int valueChange)
    {
        if (IsOwner && !IsServer)
        {
            UpdateLatestPlayedServerRpc(valueChange);
        } else if (IsOwner && IsServer)
        {
            GameManager.gM.lastCardPlayedValue.Value = valueChange;
        }
    }

    [ServerRpc]
    private void UpdateLatestPlayedServerRpc(int valueChange)
    {
        GameManager.gM.lastCardPlayedValue.Value = valueChange;
        UpdateLatestPlayedClientRpc(valueChange);
    }

    [ClientRpc]
    private void UpdateLatestPlayedClientRpc(int valueChange)
    {
        GameManager.gM.lastCardPlayedValue.Value = valueChange;
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

    private void CheckCard(Player _player)
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

    public void DropCard(Card card)
    {
        Debug.Log("Destroying card");
        cardsInHand.Remove(card);
        GameManager.createdCardsList.Add(card);
        Destroy(card.gameObject);
    }

    [ClientRpc]
    public void CreateCardsInHandClientRpc()
    {
        cardsInHand.Clear();
        foreach(int c in cardValues)
        {
            Debug.Log("Creating card for: val:" + c);
            Card newCard = GameManager.gM.CreateCardClient(0, c);
            cardsInHand.Add(newCard);
        }
        SetPlayer(cardsInHand);
    }

}
