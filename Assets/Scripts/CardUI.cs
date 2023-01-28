using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerDownHandler
{
    public Values value;
    public bool isSelected;


    private Vector2 orginalPos;

    private void SelectCard()
    {
        int cardsToSwap_;
        if (GameManager.gM.cardsExchanged.Value == -1)
            cardsToSwap_ = GameManager.gM.GetWishesAmount();
        else
            cardsToSwap_ = GameManager.gM.cardsExchanged.Value;
        //int num_ =GameManager.gM.GetWishesAmount();
        if (ExChangeCards.Instance.selectedCards.Count >= cardsToSwap_) return;
        RectTransform trans_ = gameObject.GetComponent<RectTransform>();
        orginalPos = trans_.anchoredPosition;
        trans_.anchoredPosition = orginalPos + Vector2.up * 30;
        ExChangeCards.Instance.AddToSelected(this);
        isSelected = true;
    }

    public void Deselect()
    {
        RectTransform trans_ = gameObject.GetComponent<RectTransform>();
        trans_.anchoredPosition =orginalPos;
        ExChangeCards.Instance.RemoveSelected(this);
        isSelected = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isSelected)
            SelectCard();
        else if (isSelected)
            Deselect();
    }
}
