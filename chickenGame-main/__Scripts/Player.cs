using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum PlayerType
{
    human,
    ai,
    onHuman
}

public class Player
{
    public PlayerType type = PlayerType.ai;
    public int playerNum;
    public SlotDef handSlotDef;
    public List<CardManager> hand;


    public CardManager AddCard(CardManager eCM)
    {
        if (hand == null)
            hand = new List<CardManager>();
        hand.Add(eCM);
        if (type == PlayerType.human || type == PlayerType.onHuman)
        {
            CardManager[] cards = hand.ToArray();
            cards = cards.OrderBy(cd => cd.rank).ToArray();
            hand = new List<CardManager>(cards);
        }
        eCM.SetSortingLayerName("10");
        eCM.eventualSortLayer = handSlotDef.layerName;
        FanHand();
        return (eCM);
    }

    private void FanHand()
    {
        float startRot = handSlotDef.rot;
        if (hand.Count > 1)
            startRot += GameManager.S.handFanDegrees * (hand.Count - 1) / 2;
        Vector3 pos;
        float rot;
        Quaternion rotQ;
        for (int i = 0; i < hand.Count; i++)
        {
            rot = startRot - GameManager.S.handFanDegrees * i;
            rotQ = Quaternion.Euler(0, 0, rot);
            pos = Vector3.up * CardManager.CARD_HEIGHT / 2f;
            pos = rotQ * pos;
            pos += handSlotDef.pos;
            pos.z = -0.5f * i;

            if (GameManager.S.phase != TurnPhase.idle)
                hand[i].timeStart = 0;

            hand[i].MoveTo(pos, rotQ);
            hand[i].state = CMState.toHand;

            hand[i].faceUp = type == PlayerType.human;
            hand[i].eventualSortOrder = i * 4;
        }
    }

    public CardManager RemoveCard(CardManager cm)
    {
        if (hand == null || !hand.Contains(cm))
            return null;
        hand.Remove(cm);
        FanHand();
        return (cm);
    }

    public void TakeTurn()
    {
        //If AI
        CardManager cm;
        cm = AddCard(GameManager.S.Draw());
        List<CardManager> validCards = new List<CardManager>();
        foreach (CardManager tCM in hand)
        {
            if (GameManager.S.ValidPlay(tCM))
                validCards.Add(tCM);
        }
        if (validCards.Count == 0)
        {
            foreach (var card in GameManager.S.discardPile)
                this.AddCard(card);
            GameManager.S.discardPile.Clear();
            cm.callbackPlayer = this;
            return;
        }
        if (type == PlayerType.human)
            return;
        Utils.tr("Player.TakeTurn");
        GameManager.S.phase = TurnPhase.waiting;
        cm = AiMiniMax(validCards);
        RemoveCard(cm);
        GameManager.S.MoveToTarget(cm);
        List<CardManager> removeList = new List<CardManager>();
        foreach (var card in hand)
        {
            if (card.rank == cm.rank)
            {
                card.faceUp = true;
                GameManager.S.MoveToDiscard(card);
                removeList.Add(card);
            }
        }
        if (removeList.Count != 0)
        {
            foreach (var item in removeList)
                RemoveCard(item);
        }
        cm.callbackPlayer = this;
    }
    CardManager AiMiniMax(List<CardManager> cards)
    {
        int i = 4;
        int j = 0;
        CardManager cm = null;
        if (cards.Count >= 4)
        {
            foreach (var card in cards)
            {
                int g = Count(card.rank);
                if (j <= g)
                {
                    j = g;
                    cm = card;
                }
            }
        }
        if (cards.Count < 4)
        {
            foreach (var card in cards)
            {
                int g = Count(card.rank);
                if (i >= g )
                {
                    i = g;
                    cm = card;
                }
            }
        }
        return cm;
    }
    int Count(int rank)
    {
        int i = 0;
        foreach (var card in hand)
            if (card.rank == rank)
                i++;
        return i;
    }
    public void CMCallback(CardManager tCM)
    {
        Utils.tr("Player.CMCallback()", tCM.name, "Player " + playerNum);
        GameManager.S.PassTurn();
    }
}
