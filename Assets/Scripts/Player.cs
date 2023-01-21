using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using static UnityEngine.CullingGroup;
using static GameManager;
using UnityEditor.PackageManager;
using Unity.VisualScripting;
using System.Linq;
using UnityEditor.Experimental.GraphView;

public class Player : NetworkBehaviour
{
    public List<Card> cardsInHand;
    [SerializeField]
    public List<NetworkCard> networkHand = new List<NetworkCard>();
    public GameObject cardPrefab;

    public List<Card> selectedCards = new List<Card>();

    public int cardsInHandCounter;

    private void OnEnable()
    {
        gameObject.name = $"Player {gM.players.Count+1}";
        GameManager.gM.players.Add(this);
        UIManager.Instance.endTurnBtn.onClick.AddListener(OnTurnEnd);

        cardsInHandCounter = GameManager.gM.maximumCardsInHand;
    }

    private void OnDisable()
    {
       // UIManager.Instance.endTurnBtn.onClick.RemoveListener(OnTurnEnd);
    }

    public override void OnNetworkSpawn()
    {
      //  GameManager.gM.currentPlayerId.OnValueChanged += StartTurnClientRpc;
    }

    public override void OnNetworkDespawn()
    {
        GameManager.gM.currentPlayerId.OnValueChanged -= StartTurnClientRpc;
    }


    private void Update()
    {
        UIManager.Instance.SetIsCurrentPlayerText(IsCurrentPlayer());
           

        // testing
        if (Input.GetKeyDown(KeyCode.T) && IsOwner)
        {
            GameManager.gM.InitDeckClientRpc();
            GameManager.gM.InitShuffle();
            DealCards();
            GameManager.gM.SetFirstPlayerServerRpc();

        }
        if (Input.GetKeyDown(KeyCode.U) && IsOwner)
        {
            LogCards();
        }
        if (Input.GetKeyDown(KeyCode.I) && IsOwner)
        {
            LogDeck();
        }

        if (Input.GetKeyDown(KeyCode.V) && IsClient && IsOwner)
        {
            OnTurnEnd();
        }
    }


    [ClientRpc]
    private void SpawnCardsClientRpc()
    {
        float spacing = 0f;
        List<NetworkCard> newHand = networkHand.OrderBy(card => card.value).ToList();
        // crate cards locally
        foreach (NetworkCard networkCard in newHand)
        {
            GameObject go = Instantiate(cardPrefab, new Vector2(-10f, -10f), Quaternion.identity);
            Card currentCard = go.GetComponent<Card>();

            currentCard.value = (Values)networkCard.value;
            currentCard.color = (Colors)networkCard.color;
            go.name = currentCard.value + " of " + currentCard.color;

            currentCard.cardOwner = this;

            cardsInHand.Add(currentCard);
            spacing++;
        }
       
        SpriteHolder.sP.SetSpritesPosition(this);
    }

    internal void DealCards()
    {
        if (IsOwner)
        {
            UpdateHandClientRpc();
        }
        foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameManager.GetPlayerById(uid).SpawnCardsClientRpc();
           
        }        
    }

    [ClientRpc]
    private void UpdateHandClientRpc()
    {
        int deckIndex = 0;
        //foreach (var clientid in NetworkManager.Singleton.ConnectedClientsIds)
        foreach (Player player in GameManager.gM.players)
        {
            
            for (int i = 0; i < GameManager.gM.maximumCardsInHand; i++)
            {
                player.networkHand.Add(GameManager.gM.networkDeck[deckIndex]);
                deckIndex++;

            }
        }
    }


    // this will contain clicking cards and checking wchich cards are viable to play
    [ClientRpc]
    private void StartTurnClientRpc(ulong previousValue, ulong newValue)
    {
        if (newValue == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"Player {newValue} is now current Player");
        }
        else
            Debug.Log($"Not {NetworkManager.Singleton.LocalClientId}'s turn");
    }

    public bool IsValidCard(Card card)
    {
        return (int)card.value > GameManager.gM.lastCardPlayedValue.Value || HansBomb();
        
    }
    public bool HansBomb()
    {
        return cardsInHand.GroupBy(x => x.value).Any(g => g.Count() > 3);
    }

    // btn press
    public void OnTurnEnd()
    {
        if (!IsClient || !IsOwner || !IsCurrentPlayer()) return;


        if (selectedCards.Count == 0 || IsDone()) // pass
        {
            GameManager.gM.SetLastAmountServerRpc(0);
            GameManager.gM.SetLastCardServerRpc(0);
            GameManager.gM.NextPlayerServerRpc();
            SoundManager.Instance.CheckSound();
            return;
        }

        // alredy checked for higher only need to check if equal
        if ((!AreEqualValue() || !AreEqualCount())&& !HansBomb())
        {
            selectedCards.ForEach(card => card.Deselect()); ;
            selectedCards.Clear();
            SoundManager.Instance.CancelSound();
            return;
           
        }   
        else
        {
            GameManager.gM.SetLastCardServerRpc((int)selectedCards[0].value);
            GameManager.gM.SetLastAmountServerRpc(selectedCards.Count);
            GameManager.gM.HandleCardsToSpwawnServerRpc(GetSelectedColors(selectedCards));
            SoundManager.Instance.PlaySound();
        }
        // destroy cards locally
        // remove card so the game logic knows when player ready
        cardsInHandCounter-= selectedCards.Count;
        if (IsDone())
        {
            //SpriteHolder.sP.SetWinLooseImageClientRpc();
            GameManager.gM.SetPlacement();
        }
        selectedCards.ForEach(card => card.gameObject.SetActive(false));
        selectedCards.Clear();
        

        if (GameManager.gM.IsGameOver())
        {
            GameManager.gM.PrepareNextGame();
            return;
        }

        // setting new player
        GameManager.gM.NextPlayerServerRpc();
    }

    // ----------------------- Utils -----------------------

    private NetworkColors GetSelectedColors(List<Card> _cards)
    {
        NetworkColors cols = new NetworkColors();
        foreach (Card card  in _cards)
        {
            switch (card.color)
            {
                case Colors.club:
                    cols.club = true;
                    break;
                case Colors.spade:
                    cols.spade = true;
                    break;
                case Colors.heart:
                    cols.heart = true;
                    break;
                case Colors.diamond:
                    cols.diamond = true;
                    break;
            }
        }
        return cols;
    }
   
    private bool AreEqualValue()
    {
        return selectedCards.All(c => c.value == selectedCards[0].value);
    }

    // checks if the count is either equal to middle; also returns true if middle is empty
    private bool AreEqualCount()
    {
        return selectedCards.Count == GameManager.gM.lastCardPlayedAmount.Value
            || GameManager.gM.lastCardPlayedAmount.Value == 0;
    }

    private void LogCards()
    {
        Debug.Log($"Player has the following {networkHand.Count} cards in Hand:");
        foreach (NetworkCard networkCard in networkHand)
        {
            Debug.Log("LocalClientId: "+NetworkManager.Singleton.LocalClientId+" "+networkCard.ToString());
            Debug.Log("of "+NetworkObject.NetworkObjectId);
            
        }
    }

    private void LogDeck()
    {
        Debug.Log($"Deck consists of the following {networkHand.Count} cards:");
        foreach (NetworkCard networkCard in GameManager.gM.networkDeck)
        {
            Debug.Log("LocalClientId: " + NetworkManager.Singleton.LocalClientId + " " + networkCard.ToString());
        }
    }

    public bool IsCurrentPlayer()
    {
        return GameManager.gM.currentPlayerId.Value == NetworkManager.Singleton.LocalClientId;
    }

    public bool IsDone()
    {
        if (cardsInHandCounter <= 0) return true;
        return false;
    }

    public Card checkHand(int wish)
    {
        Card tmp = null;
        foreach (var card in cardsInHand)
        {
            if ((int)card.value == wish)
            {
                tmp = card;
                cardsInHand.Remove(card);
                return tmp;
            }
        }
        return tmp;
    }
}
