using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CardBattleUI : MonoBehaviour
{
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text localHPText;
    [SerializeField] private TMP_Text EnemyHPText;
    [SerializeField] private TMP_Text statusText;

    [SerializeField] private Button[] cardButtons;
    [SerializeField] private TMP_Text[] cardsText;


    // Update is called once per frame
    private void Update()
    {
        if (CardBattleManager.Instance == null || !CardBattleManager.Instance.IsSpawned)
            return;
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool isHost = localId == NetworkManager.ServerClientId;

        int myHP = isHost ? CardBattleManager.Instance.HostHP.Value : CardBattleManager.Instance.ClientHP.Value;
        int enemyHP = isHost ? CardBattleManager.Instance.ClientHP.Value : CardBattleManager.Instance.HostHP.Value;

        localHPText.text = $"your HP: {myHP} ";
        EnemyHPText.text = $"Enemy HP: {enemyHP}";

        bool myTurn = CardBattleManager.Instance.CurrentTurnClientId.Value == localId;
        turnText.text = myTurn ? "Your turn" : " Enemy turn";

        bool canAct = myTurn && !CardBattleManager.Instance.MatchEnded.Value;

        var hand = isHost ? CardBattleManager.Instance.HostHand : CardBattleManager.Instance.ClientHand;
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if ( i < hand.Count)
            {
                CardType card = (CardType)hand [i];

                cardsText[i].text = card.ToString();
                cardButtons[i].gameObject.SetActive (true);
                cardButtons[i].interactable = canAct;

                int index = i;

                cardButtons[i].onClick.RemoveAllListeners();
                cardButtons[i].onClick.AddListener(() => OnCardPressed(index));
            }
            else
            {
                cardButtons[i].gameObject.SetActive(false);
            }
        }

        if (CardBattleManager.Instance.MatchEnded.Value)
        {
            if (myHP <= 0)
                statusText.text = "you Lose";
            else if (enemyHP <= 0)
                statusText.text = "you Win";
            else
                statusText.text = "Match ended";
        }
        else
        {
            statusText.text = canAct ? "choose a card" : "Waiting for opponent....";

        }
        
        
    }
    public void OnCardPressed(int index)
    {
        Debug.Log($"card {index} pressed");

        if (CardBattleManager.Instance == null)
            return;

        CardBattleManager.Instance.PlayCardRpc(index);
    }

    
}
