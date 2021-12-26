using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class RoundResultUI : MonoBehaviour
{
    public Text txt;

    private void Awake()
    {
        txt = GetComponent<Text>();
        txt.text = "";
    }
    void Update()
    {
        if (GameManager.S.phase != TurnPhase.gameOver)
        {
            txt.text = "";
            return;
        }
        Player cP = GameManager.CURRENT_PLAYER;
        if (cP == null || cP.type == PlayerType.human)
            txt.text = "";
        else
            txt.text = "Player " + (cP.playerNum) + " won";
    }
}
