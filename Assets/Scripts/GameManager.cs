using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEditor.Progress;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    public static GameManager gM;

    [Header("Network")]
    public NetworkVariable<ulong> currentPlayerId = new NetworkVariable<ulong>(1000, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> lastCardPlayedValue = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> lastCardPlayedAmount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> rnd = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public List<NetworkCard> networkDeck = new List<NetworkCard>();

    [Header("Setup")]
    public GameObject cardEmptyPrefab;
    public GameObject deckGO;

    [Header("Control")]
    public List<Player> players;
    public List<ulong> playerIds = new List<ulong>();

    [Header("DeckInfo")]
    [SerializeField] private List<Colors> colorsAvaliable;
    [SerializeField] private List<Values> valuesAvaliable;

    [Header("Rules")]
    public int maximumCardsInHand;

    private Dictionary<int, ulong> placements = new Dictionary<int, ulong>();
    private int currenPlayerIndex;
    private int playerCount;

    private void Awake()
    {
        if (gM == null) gM = this;
    }

    public override void OnNetworkSpawn()
    {
        InitHandlers();
        InitVariables();
        
    }

    public override void OnNetworkDespawn()
    {
        lastCardPlayedValue.OnValueChanged -= OnLastCardPlayedValueChanged;
        lastCardPlayedAmount.OnValueChanged -= OnLastCardPlayedAmountChanged;
        rnd.OnValueChanged -= ShuffleWithRandomClientRpc;
    }

    private void InitVariables()
    {
        currentPlayerId.Value = 69420;
        lastCardPlayedValue.Value = 0;
        currenPlayerIndex = 0;
    }

    public void InitHandlers()
    {
        lastCardPlayedValue.OnValueChanged += OnLastCardPlayedValueChanged;
        lastCardPlayedAmount.OnValueChanged += OnLastCardPlayedAmountChanged;

        rnd.OnValueChanged += ShuffleWithRandomClientRpc;
    }

    private void OnLastCardPlayedValueChanged(int prevVal, int newVal)
    {
        UIManager.Instance.ChangeTextForPlayerValue((Values) newVal);
        SpriteHolder.sh.cardsValue = newVal;
    }

    private void OnLastCardPlayedAmountChanged(int prevVal, int newVal)
    {
        UIManager.Instance.ChangeTextForPlayerInt(newVal);
        SpriteHolder.sh.cardsAmount = newVal;
    }


    //----------------------- Set Up ------------------------


    // All possible cards in a deck get created
    [ClientRpc]
    public void InitDeckClientRpc()
    {
        colorsAvaliable.ForEach(col => valuesAvaliable.ForEach( val => networkDeck.Add(new NetworkCard((int)col,(int)val))));
    }

    // starts the shuffling process
    [ServerRpc]
    public void InitShuffleServerRpc()
    {
        for (int i = 0; i < 100; i++)
        {
            SetRandom();
        }
    }

    // Is attached to networkvariable random (of type int); on change the first card in the deck is stwiched with the card at position of the new radnom value
    [ClientRpc]
    private void ShuffleWithRandomClientRpc(int previousValue, int newValue)
    {
        Debug.LogWarning("ShuffleWithRandomClientRpc");
        NetworkCard temp = networkDeck[newValue];
        networkDeck[newValue] = networkDeck[0];
        networkDeck[0] = temp;
    }

    // Upon being called the networkvariable (is updated across the network) random is being set to a ranadom value;
    // whenever this value changes the attacked listener triggers a method
    public void SetRandom()
    {
        rnd.Value = Random.Range(1, networkDeck.Count);
    }

    //----------------------- Handling player order ------------------------


    [ServerRpc(RequireOwnership =false)]
    public void SetFirstPlayerServerRpc()
    {
        Debug.Log("SetFirstPlayerServerRpc");
        NetworkClient currentPlayerNetworkClient;
        if (placements.Count == 0)
        {
            Debug.Log($"This condition is only met in the first round");
            currentPlayerNetworkClient = NetworkManager.Singleton.ConnectedClientsList[0];
            currentPlayerId.Value = currentPlayerNetworkClient.ClientId;
        }
        else
        {
            Debug.Log($"Placements count {placements.Count}");
            currentPlayerId.Value = placements[placements.Count];
            currenPlayerIndex = playerIds.FindIndex(id => id == currentPlayerId.Value);
            Debug.LogWarning("############## index of currentplayer is " + currenPlayerIndex);
            playerIds.ForEach( item => Debug.Log("##############" + item + "##############"));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NextPlayerServerRpc()
    {
        currentPlayerId.Value = playerIds[GetNextplayerId()];

        Player player = GetPlayerById(currentPlayerId.Value);
        if (player.IsDone())
        {
            NextPlayerServerRpc();
        }
        ResetCardsInMiddle();
    }

    private int GetNextplayerId()
    {
        currenPlayerIndex--;
        if (currenPlayerIndex < 0)
            currenPlayerIndex = playerIds.Count - 1;
        return currenPlayerIndex;
    }

    //
    private void ResetCardsInMiddle()
    {
        if (SpriteHolder.sh.cardGos.Count <= 0) return;
        
        if (SpriteHolder.sh.cardGos[0].GetComponent<Card>().ownerId == currentPlayerId.Value)
        {
            SpriteHolder.sh.ResetCardsInMiddleClientRpc();
        }
    }

    //----------------------- Update round state ------------------------

    [ServerRpc(RequireOwnership = false)]
    public void SetLastCardServerRpc(int value)
    {
        lastCardPlayedValue.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetLastAmountServerRpc(int value)
    {
        lastCardPlayedAmount.Value = value;
    }

    //----------------------- Game/Round End  ------------------------

    [ServerRpc(RequireOwnership =false)]
    public void HandleCardsToSpwawnServerRpc(NetworkColors cols)
    {
        SpriteHolder.sh.ResetCardsInMiddleClientRpc();
        SpriteHolder.sh.SetCardInMiddleClientRpc(lastCardPlayedAmount.Value, lastCardPlayedValue.Value, cols);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckGameOverServerRpc()
    {
        if (playerIds.Count > 1) return;
        SetPlacementServerRpc();
        EndRoundClientRpc();
        PrepareNextGameServerRpc();
    }

    [ClientRpc]
    private void EndRoundClientRpc()
    {
        foreach (var player_ in players)
        {
            player_.cardsInHand.ForEach(_c => Destroy(_c.gameObject));
            player_.cardsInHand.Clear();
        }
        SpriteHolder.sh.ResetCardsInMiddleClientRpc();
    }

    public void SetPlayerCount()
    {
        playerCount = players.Count;
    }

    public ClientRpcParams TargetId(ulong id)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { id } }
        };
    }

    [ServerRpc]
    public void PrepareNextGameServerRpc()
    {
        Debug.Log($"PrepareNextGameServerRpc on {NetworkManager.Singleton.LocalClientId}; Amount of clients: {NetworkManager.Singleton.ConnectedClientsIds.Count}");
        playerIds = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        ResetLastPlayed();
        InitShuffleServerRpc();
        GetPlayerById(currentPlayerId.Value).DealCards();
        SetFirstPlayerServerRpc();
        GetPlayerById(placements[1]).ExchangeCards();
        placements.Clear();
    }

    private void ResetLastPlayed()
    {
        lastCardPlayedValue.Value = 0;
        lastCardPlayedAmount.Value = 0;
    }
   

    public void RequestCard(List<Values> _vals, bool _flag)
    {
        // erste mal is true, der pr�si will karten , das zweite mal false , der pr�si gibt karten
        // der unterschied ist nur dass die empf�nger sender getauscht werden
        ulong targetId_;
        ulong senderId_;
        if (_flag)
        {
            targetId_ = placements[placements.Count];
            senderId_ = placements[1];
        }
        else
        {
            targetId_ = placements[1];
            senderId_ = placements[placements.Count];
        }
       
        int valOne_ = (int)_vals[0];
        int valTwo_ = (int)_vals[1];



        Debug.Log("Target ID: " + targetId_);

        if(IsOwner)
            GetPlayerById(targetId_).StealCardsFromPlayerToSenderClientRpc(valOne_, valTwo_, senderId_, TargetId(targetId_));

        // finde arsch
        // values entpacken
        // arsch karten entnehmen 
    }
 

    //Utils

    public Dictionary<int,ulong> GetPlacement()
    {
        return placements;
    }

    public static Player GetPlayerById(ulong id)
    {
        return NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(id).GetComponent<Player>();
    }

    [ServerRpc(RequireOwnership =false)]
    public void SetPlacementServerRpc()
    {
        int placement = placements.Count +1;
        placements.Add(placement, currentPlayerId.Value);
        LogPlacements();
    }

    public void LogPlacements()
    {
        foreach(KeyValuePair<int, ulong> entry in placements)
        {
            Debug.Log($"Placement {entry.Key} : ID {entry.Value}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    internal void RemovePlayerIdServerRpc(ulong _id)
    {
        playerIds.Remove(_id);
        Debug.LogWarning($"RemovePlayerServerRpc: count after remove = {NetworkManager.Singleton.ConnectedClientsIds.Count}");
    }

    public void HandlePlayerDone()
    {
        SetPlacementServerRpc();
        RemovePlayerIdServerRpc(currentPlayerId.Value);
    }
}


