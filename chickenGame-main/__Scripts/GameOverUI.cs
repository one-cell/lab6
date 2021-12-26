using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    private Text txt;

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
        if (GameManager.CURRENT_PLAYER == null)
            return;
        if (GameManager.CURRENT_PLAYER.type == PlayerType.human)
            txt.text = "You won!";
        else
            txt.text = "Game Over";
    }
}
