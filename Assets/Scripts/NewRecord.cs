using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewRecord : MonoBehaviour
{
    //[SerializeField]
    //private GameObject NewRecordScreen;

    private LTDescr restartAnimation;
    private void OnEnable() 
    {
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(480, 700);

        rectTransform.LeanMoveY(100, 1f).setEaseOutElastic().delay = 1.25f;

        if (restartAnimation is null)
        {
            restartAnimation = GetComponentInChildren<TMPro.TextMeshProUGUI>().gameObject
                .LeanScale(new Vector3(1.2f, 1.2f), 1f);
                //.setLoopPingPong();
        }
        restartAnimation.resume();
    }
}
