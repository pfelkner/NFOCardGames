using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    //----------------------- config start //-----------------------
    public static GameManager gM;

    [Header("SetUp")]
    public GameObject cardEmptyPrefab;
    public GameObject deckGO;

    public List<Player> players;
    public Player localPlayer;
    int index;
    int playerCount;

    public NetworkVariable<ulong> currentPlayerId = new NetworkVariable<ulong>(1000, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkClient currentPlayerNetworkClient;

    [Header("DeckInfo")]
    public List<Colors> colorsAvaliable;
    public List<Values> valuesAvaliable;

    public TMPro.TextMeshProUGUI text;

    [Header("Rules")]
    public int maximumCardsInHand;

    //[HideInInspector]
    public static List<Card> createdCardsList = new List<Card>();

    public NetworkVariable<int> lastCardPlayedValue = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> lastCardPlayedAmount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    public NetworkVariable<NetworkCard> netWorkCard = new NetworkVariable<NetworkCard>();
    public NetworkVariable<int> rnd = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField]
    public List<NetworkCard> networkDeck = new List<NetworkCard>();

    IDictionary<int, ulong> placements = new Dictionary<int, ulong>();

    public List<Player> playersFinished = new List<Player>();
    public struct NetworkCard : INetworkSerializable
    {
        public NetworkCard(int col, int val) {
            color = col;
            value = val;
            Debug.Log($"Created NetworkCard {(Values)value} of {(Colors)color}");
        }
        public int color;
        public int value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref value);
        }

        public override string ToString()
        {
            return $"{(Values)value} of {(Colors)color}";
        }
    }

    public struct NetworkColors : INetworkSerializable
    {

        public NetworkColors(bool cl, bool sp, bool he, bool di)
        {
            club = cl;
            spade = sp;
            heart = he;
            diamond = di;
        }

        public bool club;
        public bool spade;
        public bool heart;
        public bool diamond;



        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref club);
            serializer.SerializeValue(ref spade);
            serializer.SerializeValue(ref heart);
            serializer.SerializeValue(ref diamond);
        }

    }

    private void Awake()
    {
        if (gM == null) gM = this;
    }

    public override void OnNetworkSpawn()
    {
        lastCardPlayedValue.OnValueChanged += (int prevVal, int newVal) =>
        {
            UIManager.Instance.ChangeTextForPlayerValue((Values)newVal);
            SpriteHolder.sP.cardsValue = newVal;
        };
        lastCardPlayedAmount.OnValueChanged += (int prevVal, int newVal) =>
        {
            UIManager.Instance.ChangeTextForPlayerInt(newVal);
            SpriteHolder.sP.cardsAmount = newVal;
        };
      
        rnd.OnValueChanged += ShuffleWithRandomClientRpc;
        currentPlayerId.Value = 69420;
        lastCardPlayedValue.Value = 0;
        index = 0;
    }

    public override void OnNetworkDespawn()
    {
        rnd.OnValueChanged -= ShuffleWithRandomClientRpc;
    }

    //----------------------- Set Up ------------------------


    // All possible cards in a deck get created
    [ClientRpc]
    public void InitDeckClientRpc()
    {
        Debug.Log("Init deck called");
        colorsAvaliable.ForEach(col => valuesAvaliable.ForEach( val => networkDeck.Add(new NetworkCard((int)col,(int)val))));
    }

    // starts the shuffling process
    public void InitShuffle()
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
        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is shuffling");
        NetworkCard temp = networkDeck[newValue];
        networkDeck[newValue] = networkDeck[0];
        networkDeck[0] = temp;
    }

    // Upon being called the networkvariable (is updated across the network) random is being set to a ranadom value;
    // whenever this value changes the attacked listener triggers a method
    public void SetRandom()
    {
        rnd.Value = Random.Range(1, networkDeck.Count);
        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} randomized. New value: {rnd}");
    }

    //----------------------- Handling player order ------------------------


    [ServerRpc(RequireOwnership =false)]
    public void SetFirstPlayerServerRpc()
    {
        currentPlayerNetworkClient = NetworkManager.Singleton.ConnectedClientsList[0];
        currentPlayerId.Value = currentPlayerNetworkClient.ClientId;
        Debug.Log($"Current plaxer initially set to Player {currentPlayerId.Value}");
    }


    [ServerRpc(RequireOwnership = false)]
    public void NextPlayerServerRpc()
    {
        Debug.Log("Called NextPlayerTestServerRpc");
        if (index + 1 < NetworkManager.Singleton.ConnectedClientsList.Count)
            index++;
        else
            index = 0;

        currentPlayerId.Value = NetworkManager.Singleton.ConnectedClientsList[index].ClientId;
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

    // TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO
    //----------------------- Game/Round End  ------------------------

    //[ServerRpc(RequireOwnership = false)]
    //public void SetWinnerServerRpc(ulong id, int placement)
    //{
    //    switch (placement)
    //    {
    //        case 1:
    //            presiId.Value = id;
    //            break;
    //        case 2:
    //            vizePId.Value = id;
    //            break;
    //        case 3:
    //            vizeIAd.Value = id;
    //            break;
    //        case 4:
    //            arschId.Value = id;
    //            break;
    //        default:
    //            // code block
    //            break;
    //    }
    //}
    [ServerRpc(RequireOwnership =false)]
    public void HandleCardsToSpwawnServerRpc(NetworkColors cols)
    {
        Debug.LogWarning("HandleCardsClient");
        SpriteHolder.sP.SetCardsBackClientRpc();
        SpriteHolder.sP.SetCardInMiddleClientRpc(lastCardPlayedAmount.Value, lastCardPlayedValue.Value, cols);
    }



    [ServerRpc(RequireOwnership = false)]
    public void IsEndTurnServerRpc()
    {
        
       
        if (IsGameOver())
        {
            //UIManager.Instance.SetEndText($"{playersFinished[0]} wins");
        }
    }

    private bool IsGameOver()
    {
        return players.Count <= 1;
    }

    public void SetPlayerCount()
    {
        playerCount = players.Count;
    }

    [ServerRpc]
    private void ResetServerRpc()
    {
        placements.Clear();
        lastCardPlayedValue.Value = 0;
    }

    [ServerRpc]
    private void RewardWinnersServerRpc()
    {
        //get first place from placements
        // ask winner what they wish for
        // request cards from target
        // give cards to current place from placements
        // return card/s to target
        //---------- presi----------
        ulong winnerId = placements[1];
        ulong arschId = placements.Values.Last();
        GetWishClientRpc(arschId, winnerId, TargetId(winnerId));


        //---------- vize ----------
        ulong vizeId = placements[2];
    }

    [ClientRpc]
    private void GetWishClientRpc(ulong targetId, ulong senderId, ClientRpcParams rpcParams)
    {
        // ask for two cards (select via mouse click)
        // then confirm choice via button
        // requesting cards by picking them and submitting your choice should ideally be its own scene,
        // as this differs greatly from the game scene but in itself is very much alike for all players
        int wish1 = 9; // ace
        int wish2 = 8; // king


        SubmitWishServerRpc(wish1, wish2, targetId, senderId);
    }

    [ServerRpc]
    private void SubmitWishServerRpc(int wish1, int wish2, ulong targetId, ulong senderId)
    {
        RequestCardsClientRpc(wish1, wish2, senderId, TargetId(targetId));
    }

    [ClientRpc]
    private void RequestCardsClientRpc(int wish1, int wish2, ulong senderId, ClientRpcParams clientRpcParams)
    {
        Card card1 = localPlayer.checkHand(wish1);
        Card card2 = localPlayer.checkHand(wish2);

        NetworkCard c1 = new NetworkCard((int)card1.color, (int)card1.value);
        NetworkCard c2 = new NetworkCard((int)card2.color, (int)card2.value);

        // TODO check here if maybe we need to send the cards to the server first, to send it to the clients
        GiveCardsClientRpc(c1, c2, TargetId(senderId));
    }

    [ClientRpc]
    private void RequestCardsClientRpc(int wish, ulong senderId, ClientRpcParams clientRpcParams)
    {
        Card card1 = localPlayer.checkHand(wish);

        NetworkCard nc = new NetworkCard((int)card1.color, (int)card1.value);

        // TODO check here if maybe we need to send the cards to the server first, to send it to the clients
        GiveCardsClientRpc(nc, TargetId(senderId));
    }

    [ClientRpc]
    private void GiveCardsClientRpc(NetworkCard nc, ClientRpcParams clientRpcParams)
    {
        // call a function in player to instantiate cards
        // return card to the one wh gave a card
    }

    [ClientRpc]
    private void GiveCardsClientRpc(NetworkCard c1, NetworkCard c2, ClientRpcParams clientRpcParams)
    {
        throw new System.NotImplementedException();
    }

    public void AddPlayer(Player player)
    {
        //TODO check via debug if the local player assignmenet works
        Debug.Log($"Player {player.name} is local player: {player.IsLocalPlayer}");
        players.Add(player);
        if (player.IsLocalPlayer)
            localPlayer = player;
    }
    private ClientRpcParams TargetId(ulong id)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { id } }
        };
    }

}


