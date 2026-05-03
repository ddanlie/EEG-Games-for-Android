using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnswerPanelTap : MonoBehaviour, IPointerClickHandler
{

    [SerializeField] private EEGGameAttentionalBlink gameInstance;
    public void OnPointerClick(PointerEventData eventData)
    {
        string panelName = eventData.pointerClick.name;
        Debug.Log(panelName);
        if (panelName.Equals("yesButtonPanel"))
        {
            gameInstance.OnYesButtonClick();
            Debug.Log("YES CLICKED");
        }
        else
        {
            gameInstance.OnNoButtonclick();
            Debug.Log("NO CLICKED");
        }
    }
}