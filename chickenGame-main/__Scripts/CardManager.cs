using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CMState
{
    toDrawpile,
    drawpile,
    toHand,
    hand,
    toTarget,
    target,
    discard,
    to,
    idle
}
public class CardManager : Card
{
    public static float MOVE_DURATION = 0.5f;
    public static string MOVE_EASING = Easing.InOut;
    public static float CARD_HEIGHT = 3.5f;
    public static float CARD_WIDTH = 2f;

    [Header("Set Dynamically: CardManager")]
    public CMState state = CMState.drawpile;
    public List<Vector3> bezierPts;
    public List<Quaternion> bezierRots;
    public float timeStart, timeDuration;
    public int eventualSortOrder;
    public string eventualSortLayer;

    public GameObject reportFinishTo = null;
    [System.NonSerialized]
    public Player callbackPlayer = null;

    public void MoveTo(Vector3 ePos, Quaternion eRot)
    {
        bezierPts = new List<Vector3>();
        bezierPts.Add(transform.localPosition);
        bezierPts.Add(ePos);

        bezierRots = new List<Quaternion>();
        bezierRots.Add(transform.rotation);
        bezierRots.Add(eRot);

        if (timeStart == 0)
            timeStart = Time.time;
        timeDuration = MOVE_DURATION;
        state = CMState.to;
    }
    public void MoveTo(Vector3 ePos)
    {
        MoveTo(ePos, Quaternion.identity);
    }
    private void Update()
    {
        switch (state)
        {
            case CMState.toHand:
            case CMState.toTarget:
            case CMState.toDrawpile:
            case CMState.to:
                float u = (Time.time - timeStart) / timeDuration;
                float uC = Easing.Ease(u, MOVE_EASING);
                if (u < 0)
                {
                    transform.localPosition = bezierPts[0];
                    transform.rotation = bezierRots[0];
                    return;
                }
                else if (u >= 1)
                {
                    uC = 1;
                    if (state == CMState.toHand)
                        state = CMState.hand;
                    if (state == CMState.toTarget)
                        state = CMState.target;
                    if (state == CMState.toDrawpile)
                        state = CMState.drawpile;
                    if (state == CMState.to)
                        state = CMState.idle;
                    transform.localPosition = bezierPts[bezierPts.Count - 1];
                    transform.rotation = bezierRots[bezierPts.Count - 1];
                    timeStart = 0;

                    if (reportFinishTo != null)
                    {
                        reportFinishTo.SendMessage("CMCallback", this);
                        reportFinishTo = null;
                    }
                    else if (callbackPlayer != null)
                    {
                        callbackPlayer.CMCallback(this);
                        callbackPlayer = null;
                    }
                }
                else
                {
                    Vector3 pos = Utils.Bezier(uC, bezierPts);
                    transform.localPosition = pos;
                    Quaternion rotQ = Utils.Bezier(uC, bezierRots);
                    transform.rotation = rotQ;
                    if (u > 0.5f)
                    {
                        SpriteRenderer sRend = spriteRenderers[0];
                        if (sRend.sortingOrder != eventualSortOrder)
                            SetSortOrder(eventualSortOrder);
                        if (sRend.sortingLayerName != eventualSortLayer)
                            SetSortingLayerName(eventualSortLayer);
                    }
                }
                break;
        }
    }

    public override void OnMouseUpAsButton()
    {
        GameManager.S.CardClicked(this);
        base.OnMouseUpAsButton();
    }
}
