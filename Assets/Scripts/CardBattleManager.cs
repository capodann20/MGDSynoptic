using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Analytics;


public class CardBattleManager : NetworkBehaviour
{
    public NetworkList<int> HostHand = new NetworkList<int>();
    public NetworkList<int> ClientHand = new NetworkList<int>();

    [SerializeField] private int maxHandSize = 5;

    [SerializeField] private float restartDelay = 3f;
    private bool restartScheduled = false;

    public static CardBattleManager Instance {  get; private set; }

    public NetworkVariable<int> HostHP = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server );

    public NetworkVariable<int> ClientHP = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> CurrentTurnClientId = new NetworkVariable<ulong>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> MatchStarted = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> MatchEnded = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            HostHand.Clear();
            ClientHand.Clear();

            HostHand.Add((int)CardType.Strike);
            HostHand.Add((int)CardType.Heal);
            HostHand.Add((int)CardType.HeavyStrike);

            ClientHand.Add((int)CardType.Strike);
            ClientHand.Add((int)CardType.Heal);
            ClientHand.Add((int)CardType.HeavyStrike);

            HostHP.Value = 10;
            ClientHP.Value = 10;
            CurrentTurnClientId.Value = NetworkManager.ServerClientId;
            MatchStarted.Value = true;
            MatchEnded.Value = false;

            SendMatchStartEvent();
        }
    }

    private void SendMatchStartEvent()
    {
        var evt = new MatchStartEvent
        {
            players = NetworkManager.ConnectedClientsIds.Count
        };
        AnalyticsService.Instance.RecordEvent(evt);
    }
    [Rpc(SendTo.Server)]
    public void PlayCardRpc(int cardIndex, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!CanPlayerAct(senderId))
            return;

        NetworkList<int> hand = 
            senderId == NetworkManager.ServerClientId ? HostHand : ClientHand;

        if (cardIndex < 0 || cardIndex >= hand.Count)
        {
            Debug.Log("invalid card index");
            return;
        }
        CardType card = (CardType)hand[cardIndex];
        ApplyCardEffect(senderId, card);
        hand.RemoveAt(cardIndex);
        AdvanceTurn();
        SendCardPlayedEvent(senderId, card);
    }
    private void SendCardPlayedEvent(ulong playerId, CardType card)
    {
        var evt = new CardPlayedEvent
        {
            player = playerId.ToString(),
            card = card.ToString()
        };

        AnalyticsService.Instance.RecordEvent(evt);
    }
    private void ApplyCardEffect(ulong senderId, CardType card)
    {
        switch (card)
        {
            case CardType.Strike:
                DealDamage(senderId, 2);
                break;
            case CardType.Heal:
                HealPlayer(senderId, 2);
                break;
            case CardType.HeavyStrike:
                DealDamage( senderId, 4);
                break;
        }
    }
    private void DealDamage(ulong senderId, int amount)
    {
        if (senderId == NetworkManager.ServerClientId)
            ClientHP.Value -= amount;
        else
            HostHP.Value -= amount;

        CheckMatchEnd();
    }
    private void HealPlayer(ulong senderId, int amount)
    {
        if (senderId == NetworkManager.ServerClientId)
            HostHP.Value = Mathf.Min(10, HostHP.Value + amount);
        else
            ClientHP.Value = Mathf.Min(10, ClientHP.Value + amount);
    }
    private CardType GetRandomCard()
    {
        int rand = Random.Range(0, 100);
        

        if (rand < 45)
            return CardType.Strike;
        else if (rand < 85)
            return CardType.Heal;
        else
            return CardType.HeavyStrike;

    }
    
    private void DrawCard(ulong playerId)
    {
        NetworkList<int> hand =
            playerId == NetworkManager.ServerClientId ? HostHand : ClientHand;
        if (hand.Count >= maxHandSize)
            return;

        CardType newCard = GetRandomCard();

        hand.Add((int)newCard);
        Debug.Log($"player {playerId} drew {newCard}");


    }

    private bool CanPlayerAct(ulong senderId)
    {
        if (!MatchStarted.Value)
        {
            Debug.Log($"rejected {senderId} : match not started");
            return false;
        }
        if (MatchEnded.Value)
        {
            Debug.Log($"rejected {senderId} : match ended");
            return false;

        }
        if (senderId != CurrentTurnClientId.Value)
        {
            Debug.Log($"rejected {senderId} : is not your turn");
            return false;
        }

        return true;
    }

    private void AdvanceTurn()
    {
        if (MatchEnded.Value) return;

        ulong nextPlayer = NetworkManager.ServerClientId;

        if (CurrentTurnClientId.Value == NetworkManager.ServerClientId)
        {
            foreach (var client in NetworkManager.ConnectedClientsIds)
            {
                if (client != NetworkManager.ServerClientId)
                {
                    nextPlayer = client;
                    break;
                }
            }
        }
        CurrentTurnClientId.Value = nextPlayer;
        DrawCard(nextPlayer);
        
    }

    private void CheckMatchEnd()
    {
        if (HostHP.Value <= 0 || ClientHP.Value <= 0)
        {
            MatchEnded.Value = true;

            if (!restartScheduled && IsServer)
            {
                restartScheduled = true;
                StartCoroutine(RestartMatchAfterDelay());
            }
        }

        SendMatchEnded();
    }

    private void SendMatchEnded()
    {
        var evt = new MatchEndEvent
        {
            result = HostHP.Value <= 0 ? "client_win" : "host_win"
        };

        AnalyticsService.Instance.RecordEvent(evt);
    }

    private System.Collections.IEnumerator RestartMatchAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        ResetMatch();
    }

    private void ResetMatch()
    {
        if (!IsServer) return;

        restartScheduled = false;

        HostHand.Clear();
        ClientHand.Clear();

        HostHand.Add((int)CardType.Strike);
        HostHand.Add((int)CardType.Heal);
        HostHand.Add((int)CardType.HeavyStrike);

        ClientHand.Add((int)CardType.Strike);
        ClientHand.Add((int)CardType.Heal);
        ClientHand.Add((int)CardType.HeavyStrike);

        HostHP.Value = 10;
        ClientHP.Value = 10;
        CurrentTurnClientId.Value = NetworkManager.ServerClientId;
        MatchStarted.Value = true;
        MatchEnded.Value = false;

        Debug.Log("Match Restarted");
    }

    //Analytics

}
