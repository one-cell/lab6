using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TurnPhase
{
    idle,
    pre,
    waiting,
    post,
    gameOver
}

public class GameManager : MonoBehaviour
{
    static public GameManager S;
    private GMLayout layout;
    private Transform layoutAnchor;
    public static Player CURRENT_PLAYER;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public Vector3 layoutCenter = Vector3.zero;
    public float handFanDegrees = 20f;
    public int numStartingCards = 5;
    public float drawTimeStagger = 0.1f;

    [Header("Set Dynamically")]
    public Deck deck;
    public List<CardManager> drawPile;
    public List<CardManager> discardPile;
    public List<Player> players;
    public CardManager targetcard;
    public TurnPhase phase = TurnPhase.idle;
    public static readonly int players_ = 2;

    void Awake()
    {
        S = this;
    }

    void Start()
    {
        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text);
        Deck.Shuffle(ref deck.cards);

        layout = GetComponent<GMLayout>();
        layout.ReadLayout(layoutXML.text);
        drawPile = UpgradeCardsList(deck.cards);

        LayoutGame();
    }
    public void ArrangeDrawPile()
    {
        CardManager tCM;
        for (int i = 0; i < drawPile.Count; i++)
        {
            tCM = drawPile[i];
            tCM.transform.SetParent(layoutAnchor);
            tCM.transform.localScale = new Vector3(1.2f, 1.2f, 0f);
            tCM.transform.localPosition = layout.drawPile.pos;
            tCM.faceUp = false;
            tCM.SetSortingLayerName(layout.drawPile.layerName);
            tCM.SetSortOrder(-i * players_);
            tCM.state = CMState.drawpile;
        }
    }
    private void LayoutGame()
    {
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }
        ArrangeDrawPile();
        Player pl;
        players = new List<Player>();
        foreach (SlotDef tSD in layout.slotDefs)
        {
            pl = new Player();
            pl.handSlotDef = tSD;
            players.Add(pl);
            pl.playerNum = tSD.player;
        }
        players[0].type = PlayerType.human;

        CardManager tCM;
        for (int i = 0; i < players_; i++)
        {
            for (int j = 0; j < numStartingCards; j++)
            {
                tCM = Draw();

                tCM.timeStart = Time.time + drawTimeStagger * (i * players_ + j);
                players[(j + 1) % players_].AddCard(tCM);
            }
        }

        Invoke("DrawFirstTarget", drawTimeStagger * (numStartingCards * players_ + players_));
    }
    public void DrawFirstTarget()
    {
        CardManager tCM = MoveToTarget(Draw());
        tCM.reportFinishTo = this.gameObject;
    }

    public void CMCallback(CardManager cm)
    {
        Utils.tr("GameManager:CMCallback()", cm.name);
        StartGame();
    }
    public void StartGame()
    {
        PassTurn(1);
    }

    public void PassTurn(int num = -1)
    {
        if (num == -1)
        {
            int ndx = players.IndexOf(CURRENT_PLAYER);
            num = (ndx + 1) % players_;
        }
        int lastPlayerNum = -1;
        if (CURRENT_PLAYER != null)
        {
            lastPlayerNum = CURRENT_PLAYER.playerNum;
            if (CheckGameOver())
                return;
        }
        CURRENT_PLAYER = players[num];
        phase = TurnPhase.pre;
        GameObject.Find("TurnLight").GetComponent<Light>().range = 12;
        CURRENT_PLAYER.TakeTurn();
        Utils.tr("GameManager:PassTurn()", "Old: " + lastPlayerNum, "New: " + CURRENT_PLAYER.playerNum);
    }

    private bool CheckGameOver()
    {
        if (drawPile.Count == 0)
        {
            List<Card> cards = new List<Card>();
            foreach (CardManager cm in discardPile)
                cards.Add(cm);
            discardPile.Clear();
            Deck.Shuffle(ref cards);
            drawPile = UpgradeCardsList(cards);
            ArrangeDrawPile();
        }
        if (CURRENT_PLAYER.hand.Count == 0)
        {
            phase = TurnPhase.gameOver;
            Invoke("RestartGame", 1);
            return (true);
        }
        return false;
    }

    public void RestartGame()
    {
        CURRENT_PLAYER = null;
        SceneManager.LoadScene("__Bartok_Scene_0");
    }

    public bool ValidPlay(CardManager cm)
    {
        if (cm.suit != targetcard.suit)
            return (true);

        return (false);
    }

    public CardManager MoveToTarget(CardManager tCM)
    {
        tCM.timeStart = 0;
        tCM.MoveTo(layout.discardPile.pos + Vector3.back);
        tCM.state = CMState.toTarget;
        tCM.faceUp = true;
        tCM.SetSortingLayerName("10");
        tCM.eventualSortLayer = layout.target.layerName;
        if (targetcard != null)
            MoveToDiscard(targetcard);
        targetcard = tCM;
        return (tCM);
    }
    public CardManager MoveToDiscard(CardManager tCM)
    {
        tCM.state = CMState.discard;
        discardPile.Add(tCM);
        tCM.SetSortingLayerName(layout.discardPile.layerName);
        tCM.SetSortOrder(discardPile.Count * players_);
        tCM.transform.localPosition = layout.discardPile.pos + Vector3.back / 2;
        return (tCM);
    }
    public CardManager Draw()
    {
        CardManager cd = drawPile[0];
        if (drawPile.Count == 0)
        {
            int ndx;
            while (discardPile.Count > 0)
            {
                ndx = Random.Range(0, discardPile.Count);
                drawPile.Add(discardPile[ndx]);
                discardPile.RemoveAt(ndx);
            }
            ArrangeDrawPile();
            float t = Time.time;
            foreach (CardManager tCM in drawPile)
            {
                tCM.transform.localPosition = layout.discardPile.pos;
                tCM.callbackPlayer = null;
                tCM.MoveTo(layout.drawPile.pos);
                tCM.timeStart = t;
                t += 0.02f;
                tCM.state = CMState.toDrawpile;
                tCM.eventualSortLayer = "0";
            }
        }
        drawPile.RemoveAt(0);
        return (cd);
    }

    List<CardManager> UpgradeCardsList(List<Card> lCD)
    {
        List<CardManager> lCM = new List<CardManager>();
        foreach (Card tCD in lCD)
            lCM.Add(tCD as CardManager);
        return lCM;
    }
    public void CardClicked(CardManager tCM)
    {
        if (CURRENT_PLAYER.type == PlayerType.ai)
            return;
        if (phase == TurnPhase.waiting)
            return;
        if (!tCM.faceUp)
            return;
        if (ValidPlay(tCM))
        {
            CURRENT_PLAYER.RemoveCard(tCM);
            MoveToTarget(tCM);
            List<CardManager> removeList = new List<CardManager>();
            foreach (var card in CURRENT_PLAYER.hand)
            {
                if (card.rank == targetcard.rank)
                {
                    MoveToDiscard(card);
                    removeList.Add(card);
                }
            }
            tCM.callbackPlayer = CURRENT_PLAYER;
            phase = TurnPhase.waiting;
            if (removeList.Count != 0)
            {
                foreach (var item in removeList)
                    CURRENT_PLAYER.RemoveCard(item);
            }
            Utils.tr("GameManager:CardClicked()", "Play", tCM.name, targetcard.name + " is target");
        }
        else
            Utils.tr("GameManager:CardClicked()", "Attempted to Play", tCM.name, targetcard.name + " is target");
    }
}