using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects de la escena
    public GameObject boardGameObject;
    public GameObject player1;
    public GameObject player2;
    public GameObject piecePrefab;
    public GameObject lastMovementPrefab;

    //UI
    public Text finalMessage;
    public Text blackText;
    public Text whiteText;
    public Button playAgainButton;

    //Materials
    public Material blackMaterial;
    public Material whiteMaterial;
    public Material tileMaterial;
    public Material tileSelectableMaterial;

    //Otras variables
    GameObject[] tileGameObjects = new GameObject[Constants.NumTiles];
    GameObject[] pieces = new GameObject[Constants.NumTiles];
    Tile[] board = new Tile[Constants.NumTiles];
    private List<int> selectableTiles = new List<int>();
    private BoardManager boardManager;
    GameObject lastMovementMark;
    private int turn;
    private bool passBlack = false, passWhite = false;
                    
    void Start()
    {       
        InitTiles();

        boardManager = GameObject.FindGameObjectWithTag("BoardManager").GetComponent<BoardManager>();

        turn = Constants.Start;
        PlaceInitialPiece(3, 3, turn);
        PlaceInitialPiece(4, 4, turn);
        PlaceInitialPiece(3, 4, -turn);
        PlaceInitialPiece(4, 3, -turn);

        DrawSelectableTiles();

        Vector3 pos = new Vector3(-1.5f, -0.138f, -1.5f);
        lastMovementMark = Instantiate(lastMovementPrefab, pos, Quaternion.identity);

        if (Constants.Player1.CompareTo("AI") == 0 && Constants.Player2.CompareTo("AI") == 0)
        {
            StartCoroutine(CountDown(3));
        }

    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = boardGameObject.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tileGameObjects[fil * Constants.TilesPerRow + col] = tilechild;
                board[fil * Constants.TilesPerRow + col] = tileGameObjects[fil * Constants.TilesPerRow + col].GetComponent<TileBehaviour>().tileInfo;
            }
        }

    }

    void PlaceInitialPiece(int row, int col, int color)
    {

        float coordrow = row - 3.5f;
        float coordcol = col - 3.5f;
        int clickedtile = row * 8 + col;

        Vector3 pos = new Vector3(coordcol, 0.238f, coordrow);

        pieces[clickedtile] = Instantiate(piecePrefab, pos, Quaternion.identity);
        if (color == 1)
            pieces[clickedtile].GetComponent<Renderer>().material = blackMaterial;
        else
            pieces[clickedtile].GetComponent<Renderer>().material = whiteMaterial;

        tileGameObjects[clickedtile].GetComponent<TileBehaviour>().tileInfo.value = color;

        blackText.text = ""+ boardManager.CountPieces(board, Constants.Black);
        whiteText.text = "" + boardManager.CountPieces(board, Constants.White);

    }          

    private void DrawSelectableTiles()
    {
        // Reseteamos color
        foreach (int t in selectableTiles)
        {
            tileGameObjects[t].GetComponent<Renderer>().material = tileMaterial;
        }

        // Limpiamos la lista
        selectableTiles.Clear();

        // La rellenamos con las nuevas casillas seleccionables
        selectableTiles = boardManager.FindSelectableTiles(board, turn);
        // Marcamos los seleccionables
        foreach (int t in selectableTiles)
        {
            tileGameObjects[t].GetComponent<Renderer>().material = tileSelectableMaterial;
        }

    }
    
    private bool IsClickable(int t)
    {
        foreach(int s in selectableTiles)
        {
            if (t == s)
                return true;
        }
        return false;
    }
        
    public void ClickOnTile(int clickedtile)
    {                
        if (IsClickable(clickedtile))
        {
            int row = clickedtile / 8;
            int col = clickedtile % 8;
            float coordrow = row - 3.5f;
            float coordcol = col - 3.5f;

            Vector3 pos = new Vector3(coordcol, 0.238f, coordrow);
            pieces[clickedtile] = Instantiate(piecePrefab, pos, Quaternion.identity);

            if (turn == 1)
                pieces[clickedtile].GetComponent<Renderer>().material = blackMaterial;
            else
                pieces[clickedtile].GetComponent<Renderer>().material = whiteMaterial;

            lastMovementMark.transform.position = pos + Vector3.up*0.1f;
            board[clickedtile].value = turn;
            if (turn == Constants.Black)
            {
                passBlack = false;
            }
            else
            {
                passWhite = false;
            }

            //Cambio las fichas con movimiento
            List<int> swappablePieces = boardManager.FindSwappablePieces(board, clickedtile, turn);

            foreach (int s in swappablePieces)
            {
                if (turn == Constants.Black)
                    pieces[s].GetComponent<Renderer>().material = blackMaterial;
                else
                    pieces[s].GetComponent<Renderer>().material = whiteMaterial;

                board[s].value = turn;
            }

            int blackPieces = boardManager.CountPieces(board, Constants.Black);
            int whitePieces = boardManager.CountPieces(board, Constants.White);
            blackText.text = "" + blackPieces;
            whiteText.text = "" + whitePieces;

            if (((blackPieces + whitePieces) == Constants.NumTiles) || blackPieces == 0 || whitePieces == 0)
            {
                finalMessage.text = "End Game!!";
                DrawSelectableTiles();
                int res=GetWinner(blackPieces, whitePieces);
                EndGame(res);
            }

            else
            {
                turn = -turn;                

                DrawSelectableTiles();                

                if (selectableTiles.Count == 0)
                {
                    if (turn == Constants.Black)
                    {
                        passBlack = true;
                        if (passBlack && passWhite)
                        {
                            finalMessage.text = "End Game!!";
                        }
                        else
                        {
                            finalMessage.text = "Pass!!";
                            StartCoroutine("Wait2");
                        }
                    }
                    else
                    {
                        passWhite = true;
                        if (passBlack && passWhite)
                        {
                            finalMessage.text = "End Game!!";
                        }
                        else
                        {
                            finalMessage.text = "Pass!!";
                            StartCoroutine("Wait2");
                        }
                    }

                }

                else
                {
                    NextTurn();
                }
            }                        

        }                      

    }

    public void NextTurn()
    {

        if (turn == 1)
        {
            if (Constants.Player1.Equals("AI"))
            {
                int tile=player1.GetComponent<Player>().SelectTile(board);
                ClickOnTile(tile);
            }
        }

        else
        {
            if (Constants.Player2.Equals("AI"))
            {                               
                int tile = player2.GetComponent<Player>().SelectTile(board);
                ClickOnTile(tile);
            }
             
        }


    }

    int GetWinner(int blackPieces, int whitePieces)
    {
        if (blackPieces > whitePieces)
            return Constants.Black;
        if (whitePieces > blackPieces)
            return Constants.White;

        return Constants.Empty;
    }

    public void EndGame(int turn)
    {
        if (turn == Constants.Black)
            finalMessage.text = "Black wins!";
        if (turn == Constants.White)
            finalMessage.text = "White wins!";
        if (turn == Constants.Empty)
            finalMessage.text = "Draw!";

        playAgainButton.interactable = true;
    }

    public void PlayAgain()
    {

        GameObject[] pieces = GameObject.FindGameObjectsWithTag("Piece");
        foreach (GameObject g in pieces)
            GameObject.Destroy(g);

        for (int row = 0; row < Constants.TilesPerRow; row++)
            for(int col=0; col < Constants.TilesPerRow; col++)
                board[row*8+col].value = 0;

        playAgainButton.interactable = false;
        finalMessage.text = "";

        turn = Constants.Start;
        PlaceInitialPiece(3, 3, turn);
        PlaceInitialPiece(4, 4, turn);
        PlaceInitialPiece(3, 4, -turn);
        PlaceInitialPiece(4, 3, -turn);

        DrawSelectableTiles();

        Vector3 pos = new Vector3(-1.5f, -0.138f, -1.5f);
        lastMovementMark.transform.position = pos;

        if (Constants.Player1.CompareTo("AI") == 0 && Constants.Player2.CompareTo("AI") == 0)
            StartCoroutine(CountDown(3));

    }
        

    IEnumerator CountDown(int val)
    {

        yield return new WaitForSeconds(1);
        finalMessage.text = "" + val;
        if (val >= 0)
        {
            val--;
            StartCoroutine(CountDown(val));
        }
        else
        {
            finalMessage.text = "";
            NextTurn();
        }
                
    }

    IEnumerator Wait2()
    {
        yield return new WaitForSeconds(Constants.PassTime);
        finalMessage.text = "";
        turn = -turn;

        DrawSelectableTiles();
        
        if (selectableTiles.Count == 0)
        {
            if (turn == Constants.Black)
            {
                passBlack = true;
                if (passBlack && passWhite)
                {
                    finalMessage.text = "End Game!!";
                }
                else
                {
                    finalMessage.text = "Pass!!";
                    StartCoroutine("Wait2");
                }
            }
            else
            {
                passWhite = true;
                if (passBlack && passWhite)
                {
                    finalMessage.text = "End Game!!";
                }
                else
                {
                    finalMessage.text = "Pass!!";
                    StartCoroutine("Wait2");
                }
            }
            

        }

        else
        {
            NextTurn();
        }

    }

       
}
