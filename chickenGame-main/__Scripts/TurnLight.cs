using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnLight : MonoBehaviour
{
    void Update()
    {
        transform.position = Vector3.back * 3;
        if (GameManager.CURRENT_PLAYER == null) return;

        transform.position += GameManager.CURRENT_PLAYER.handSlotDef.pos;
    }
}
