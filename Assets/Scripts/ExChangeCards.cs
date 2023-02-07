using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class ExChangeCards : MonoBehaviour
{
    public static ExChangeCards Instance;
   
    public List<Values> valuesToChoose;
    public List<Values> valuesToRemove;

    public GameObject addBtn;
    public GameObject removeBtn;

    public List<CardUI> cardsToSteal;
    private List<Card> cardsToReturn;
    public TextMeshProUGUI text;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }


    public void SetPosition()
    {
        RectTransform trans_ = gameObject.GetComponent<RectTransform>();
        trans_.anchoredPosition = new Vector2(-12, 31);
    }

    public void SetPosition2()
    {
        RectTransform trans_ = gameObject.GetComponent<RectTransform>();
        trans_.anchoredPosition = new Vector2(600, 600);
    }


    public void AddToSelected(CardUI _cardUI)
    {
        cardsToSteal.Add(_cardUI);
    }
    public void RemoveSelected(CardUI _cardUI)
    {
        cardsToSteal.Remove(_cardUI);
    }

    // btn click
    public void SentCards()
    {
       // cardsToSteal.ForEach(c => c.Deselect());
        if (GameManager.gM.state == State.Stealing || GameManager.gM.state == State.StealingVize)
        {
            StealCards();
        } else if (GameManager.gM.state == State.Returning || GameManager.gM.state == State.ReturningVize)
        {
            ReturnCards();
        }
    }

    public void ResetSelection()
    {
        for (int i = cardsToSteal.Count -1; i >= 0; i--)
        {
            cardsToSteal[i].Deselect();
        }
    }

    public void StealCards()
    {
        List<Values> vals_ = new List<Values>();
        cardsToSteal.ForEach(c => vals_.Add(c.value));
        GameManager.gM.GetCards(vals_);
        if (cardsToSteal.Count == 2)
        {
            GameManager.gM.DisplayRequestedCardsServerRpc((int)cardsToSteal[0].value, (int)cardsToSteal[1].value);
        } else if(cardsToSteal.Count == 1)
        {
            GameManager.gM.DisplayRequestedCardsServerRpc((int)cardsToSteal[0].value, -1);
        } else if(cardsToSteal.Count == 0)
        {

        }
        ResetSelection();
    }
    public void ReturnCards()
    {
        List<Values> vals_ = new List<Values>();
        cardsToReturn = GameManager.gM.GetLocalPlayer().GetSelectedCards();
        cardsToReturn.ForEach(c => vals_.Add(c.value));
        GameManager.gM.ReturnCards(vals_);
        string text_ = "Hier" + cardsToReturn.Count;
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
