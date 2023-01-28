using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ExChangeCards : MonoBehaviour
{
    public static ExChangeCards Instance;
   
    public List<Values> valuesToChoose;
    public List<Values> valuesToRemove;

    public GameObject addBtn;
    public GameObject removeBtn;

    public List<CardUI> selectedCards;
    public TextMeshProUGUI text;

    public int counter;


    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
 

    public void SetButtons(bool _flag)
    {
        addBtn.SetActive(_flag);
        removeBtn.SetActive(_flag);
    }


    public void AddToSelected(CardUI _cardUI)
    {
        selectedCards.Add(_cardUI);
    }
    public void RemoveSelected(CardUI _cardUI)
    {
        selectedCards.Remove(_cardUI);
    }

    public void SentCards()
    {
       // selectedCards.ForEach(c => c.Deselect());
        if (counter == 0)
        {
            CardsToSteal();
        } else
        {
            CardsToGet();
        }
    }

    private void ResetSelection()
    {
        for (int i = selectedCards.Count -1; i >= 0; i--)
        {
            selectedCards[i].Deselect();
        }
    }

    public void CardsToSteal()
    {
        List<Values> vals_ = new List<Values>();
        selectedCards.ForEach(c => vals_.Add(c.value));
        GameManager.gM.GetCards(vals_);
        counter++;
        ResetSelection();
    }
    public void CardsToGet()
    {
        List<Values> vals_ = new List<Values>();
        selectedCards.ForEach(c => vals_.Add(c.value));
        GameManager.gM.ReturnCards(vals_);
        counter = 0;
        gameObject.SetActive(false);
        ResetSelection();
    }
}
