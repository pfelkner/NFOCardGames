using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static GameManager;
using System.Linq;

public class Player : NetworkBehaviour
{
    public List<Card> cardsInHand;
    [SerializeField]
    public List<NetworkCard> networkHand = new List<NetworkCard>();
    public GameObject cardPrefab;

    public List<Card> selectedCards = new List<Card>();

    public override void OnNetworkSpawn()
    {
        gameObject.name = $"Player {gM.players.Count + 1}";
        GameManager.gM.players.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        GameManager.gM.players.Remove(this);
    }


    private void Update()
    {
        UIManager.Instance.SetIsCurrentPlayerText(IsCurrentPlayer());
        
        // testing
        if (Input.GetKeyDown(KeyCode.T) && IsOwner)
        {
            GameManager.gM.playerIds = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
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
        if (Input.GetKeyDown(KeyCode.L) && IsOwner)
        {
            GameManager.gM.LogPlacements();
        }

        if (Input.GetKeyDown(KeyCode.Space) && IsClient && IsOwner)
        {
            TakeTurn();
        }
    }


    [ClientRpc]
    private void SpawnCardsClientRpc()
    {
        float spacing = 0f;
        List<NetworkCard> newHand = SortNetworkCards(networkHand);
        // crate cards locally
        foreach (NetworkCard networkCard in newHand)
        {
            CreateCardInHand(networkCard);
            spacing++;
        }
        SpriteHolder.sh.SetSpritesPosition(this);
    }
    private List<NetworkCard> SortNetworkCards(List<NetworkCard> _netHand)
    {
       return _netHand.OrderBy(card => card.value).ToList();
    }
    private void CreateCardInHand(NetworkCard _netCard)
    {
        GameObject go = Instantiate(cardPrefab, new Vector2(-10f, -10f), Quaternion.identity);
        Card currentCard = go.GetComponent<Card>();

        currentCard.value = (Values)_netCard.value;
        currentCard.color = (Colors)_netCard.color;
        go.name = currentCard.value + " of " + currentCard.color;

        currentCard.cardOwner = this;

        cardsInHand.Add(currentCard);
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

        foreach (Player player in GameManager.gM.players)
        {
            player.networkHand.Clear();

            for (int i = 0; i < GameManager.gM.maximumCardsInHand; i++)
            {
                player.networkHand.Add(GameManager.gM.networkDeck[deckIndex]);
                deckIndex++;
            }
        }
    }

    [ClientRpc]
    public void StealCardsFromPlayerToSenderClientRpc(int _valOne, int _valTwo, ulong _senderId, ClientRpcParams  _targetId)
    {
        Debug.Log("steal " + _valOne + "and" + _valTwo +"from"+_targetId+ "to"+ _senderId +"Shoult only be called in target client");
        // sind im arschloch
        NetworkCard newValOne_ = new NetworkCard();
        NetworkCard newValTwo_ = new NetworkCard();

       
        newValOne_ = networkHand.First(c => c.value == _valOne);
        // if no match -> error
        networkHand.Remove(newValOne_);
        Card c1 = cardsInHand.Find(c => (int)c.value == newValOne_.value && (int)c.color == newValOne_.color);
        cardsInHand.Remove(c1);
        //Destroy(c1.gameObject);
        c1.gameObject.transform.position = new Vector2(-15, -15);
    
        newValTwo_ = networkHand.First(c => c.value == _valTwo);
        networkHand.Remove(newValTwo_);
        Card c2 = cardsInHand.Find(c => (int)c.value == newValTwo_.value && (int)c.color == newValTwo_.color);
        cardsInHand.Remove(c2);
        //Destroy(c2.gameObject);
        c2.gameObject.transform.position = new Vector2(-15, -15);

        HandleStolenCardsServerRpc(newValOne_, newValTwo_, _senderId);
    }

    [ServerRpc(RequireOwnership =false)]
    public void HandleStolenCardsServerRpc(NetworkCard _newCardOne,NetworkCard _newCardTwo,ulong _senderId)
    {
        GetPlayerById(_senderId).GiveCardsBackClientRpc(_newCardOne, _newCardTwo, GameManager.gM.TargetId(_senderId));
    }

    [ClientRpc]
    public void GiveCardsBackClientRpc(NetworkCard _newValOne, NetworkCard _newValTwo, ClientRpcParams _PraesiId)
    {
        networkHand.Add(_newValOne);
        networkHand.Add(_newValTwo);

        cardsInHand.ForEach(c => Destroy(c.gameObject));

        SpawnCardsClientRpc();
    }

    #region Gamelogic

    public bool IsValidCard(Card card)
    {
        return (int)card.value > GameManager.gM.lastCardPlayedValue.Value || HansBomb();
    }
    public bool HansBomb()
    {
        return cardsInHand.GroupBy(x => x.value).Any(g => g.Count() > 3);
    }

    public void TakeTurn()
    {
        if (!IsClient || !IsOwner || !IsCurrentPlayer()) return;

        if (selectedCards.Count == 0 || IsDone()) // pass
        {
            PassTurn();
            return;
        }
        // alredy checked for higher only need to check if equal
        if ((!AreEqualValue() || !AreEqualCount())&& !HansBomb())
        {
            CancelMove();
            return;
        }   
        else
            PlayCards();
        
        if (IsDone())
            GameManager.gM.HandlePlayerDone();

        EndTurn();
    }

    private void EndTurn()
    {
        GameManager.gM.NextPlayerServerRpc();
        GameManager.gM.CheckGameOverServerRpc(); 
    }

    private void PlayCards()
    {
        GameManager.gM.SetLastCardServerRpc((int)selectedCards[0].value);
        GameManager.gM.SetLastAmountServerRpc(selectedCards.Count);
        GameManager.gM.HandleCardsToSpwawnServerRpc(GetSelectedColors(selectedCards));
        SoundManager.Instance.PlaySound();

        RemovePlayedcards();
    }

    private void RemovePlayedcards()
    {
        selectedCards.ForEach(card => HandlePlayerCard(card));
        selectedCards.Clear();
    }

    private void PassTurn()
    {
        GameManager.gM.SetLastAmountServerRpc(0);
        GameManager.gM.SetLastCardServerRpc(0);
        GameManager.gM.NextPlayerServerRpc();
        SoundManager.Instance.CheckSound();
    }

    private void CancelMove()
    {
        selectedCards.ForEach(card => card.Deselect()); ;
        selectedCards.Clear();
        SoundManager.Instance.CancelSound();
    }

    // ----------------------- Utils -----------------------

    public void HandlePlayerCard(Card _card)
    {
        cardsInHand.Remove(_card);
        Destroy(_card.gameObject);
    }

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

    public bool IsCurrentPlayer()
    {
        return GameManager.gM.currentPlayerId.Value == NetworkManager.Singleton.LocalClientId;
    }

    public bool IsDone()
    {
        if (cardsInHand.Count <= 0) return true;
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
    #endregion

    #region Utils
    private void LogCards()
    {
        Debug.Log($"Player has the following {networkHand.Count} cards in Hand:");
        foreach (NetworkCard networkCard in networkHand)
        {
            Debug.Log("LocalClientId: "+NetworkManager.Singleton.LocalClientId+" "+networkCard.ToString());
            
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
    #endregion
}
