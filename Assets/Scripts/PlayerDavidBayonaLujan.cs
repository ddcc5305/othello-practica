using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ==========================================
// 1. ESTRUCTURA DEL NODO (ÁRBOL DE BÚSQUEDA)
// ==========================================
public class NodeDavidBayonaLujan
{
    public Tile[] board = new Tile[Constants.NumTiles];
    public NodeDavidBayonaLujan parent;
    public List<NodeDavidBayonaLujan> childList = new List<NodeDavidBayonaLujan>();
    public int type;
    public double utility;
    public double alfa;
    public double beta;
    public int move;

    public NodeDavidBayonaLujan(Tile[] tiles)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            this.board[i] = new Tile();
            this.board[i].value = tiles[i].value;
        }
    }    
}

// ==========================================
// 2. CLASE JUGADOR Y CONFIGURACIÓN
// ==========================================
public class PlayerDavidBayonaLujan : MonoBehaviour
{
    public int turn;    
    private BoardManager boardManager;

    [Header("Configuración de la IA")]
    [Range(2, 4)]
    public int maxDepth = 4;

    [Range(1, 4)]
    public int heuristic = 3;

    private static readonly int[] positionWeights = {
        100, -20,  10,   5,   5,  10, -20, 100,
        -20, -40,  -5,  -5,  -5,  -5, -40, -20,
         10,  -5,   5,   1,   1,   5,  -5,  10,
          5,  -5,   1,   0,   0,   1,  -5,   5,
          5,  -5,   1,   0,   0,   1,  -5,   5,
         10,  -5,   5,   1,   1,   5,  -5,  10,
        -20, -40,  -5,  -5,  -5,  -5, -40, -20,
        100, -20,  10,   5,   5,  10, -20, 100
    };

    private static readonly int[] corners = { 0, 7, 56, 63 };
    private static readonly int[] xSquares = { 9, 14, 49, 54 };
    private static readonly int[][] cSquares = {
        new int[] { 1, 8 },
        new int[] { 6, 15 },
        new int[] { 48, 57 },
        new int[] { 55, 62 }
    };

    void Start()
    {
        boardManager = GameObject.FindGameObjectWithTag("BoardManager").GetComponent<BoardManager>();
    }
       
    // ==========================================
    // 3. PUNTO DE ENTRADA (SELECCIÓN DE JUGADA)
    // ==========================================
    public int SelectTile(Tile[] board)
    {        
        int originalTurn = turn;

        NodeDavidBayonaLujan root = new NodeDavidBayonaLujan(board);
        root.type = Constants.MAX;
        root.alfa = double.NegativeInfinity;
        root.beta = double.PositiveInfinity;

        GenerateTree(root, 0, turn);
        MinimaxAlphaBeta(root, double.NegativeInfinity, double.PositiveInfinity, turn);

        int bestMove = -1;
        double bestValue = double.NegativeInfinity;
        foreach (NodeDavidBayonaLujan child in root.childList)
        {
            if (child.utility > bestValue)
            {
                bestValue = child.utility;
                bestMove = child.move;
            }
        }

        turn = originalTurn;

        if (bestMove == -1)
        {
            List<int> selectableTiles = boardManager.FindSelectableTiles(board, turn);
            if (selectableTiles.Count > 0)
                bestMove = selectableTiles[0];
        }

        return bestMove;
    }

    // ==========================================
    // 4. GENERACIÓN DEL ÁRBOL RECURSIVO
    // ==========================================
    private void GenerateTree(NodeDavidBayonaLujan node, int depth, int currentTurn)
    {
        if (depth >= maxDepth)
            return;

        List<int> selectableTiles = boardManager.FindSelectableTiles(node.board, currentTurn);

        if (selectableTiles.Count == 0)
        {
            NodeDavidBayonaLujan child = new NodeDavidBayonaLujan(node.board);
            child.parent = node;
            child.type = (node.type == Constants.MAX) ? Constants.MIN : Constants.MAX;
            child.move = -1;
            node.childList.Add(child);

            GenerateTree(child, depth + 1, -currentTurn);
            return;
        }

        foreach (int s in selectableTiles)
        {
            NodeDavidBayonaLujan child = new NodeDavidBayonaLujan(node.board);
            child.parent = node;
            child.type = (node.type == Constants.MAX) ? Constants.MIN : Constants.MAX;
            child.move = s;

            boardManager.Move(child.board, s, currentTurn);
            node.childList.Add(child);

            GenerateTree(child, depth + 1, -currentTurn);
        }
    }

    // ==========================================
    // 5. EVALUACIÓN MINIMAX CON PODA ALFA-BETA
    // ==========================================
    private double MinimaxAlphaBeta(NodeDavidBayonaLujan node, double alpha, double beta, int aiTurn)
    {
        if (node.childList.Count == 0)
        {
            node.utility = EvaluateHeuristic(node.board, aiTurn);
            return node.utility;
        }

        if (node.type == Constants.MAX)
        {
            double maxEval = double.NegativeInfinity;

            foreach (NodeDavidBayonaLujan child in node.childList)
            {
                double eval = MinimaxAlphaBeta(child, alpha, beta, aiTurn);
                if (eval > maxEval)
                    maxEval = eval;
                if (eval > alpha)
                    alpha = eval;
                if (alpha >= beta)
                    break;
            }

            node.utility = maxEval;
            node.alfa = alpha;
            return maxEval;
        }
        else
        {
            double minEval = double.PositiveInfinity;

            foreach (NodeDavidBayonaLujan child in node.childList)
            {
                double eval = MinimaxAlphaBeta(child, alpha, beta, aiTurn);
                if (eval < minEval)
                    minEval = eval;
                if (eval < beta)
                    beta = eval;
                if (alpha >= beta)
                    break;
            }

            node.utility = minEval;
            node.beta = beta;
            return minEval;
        }
    }

    // ==========================================
    // 6. SELECCIÓN Y CÁLCULO DE HEURÍSTICAS
    // ==========================================
    private double EvaluateHeuristic(Tile[] board, int aiTurn)
    {
        switch (heuristic)
        {
            case 1: return H1(board, aiTurn);
            case 2: return H2(board, aiTurn);
            case 3: return H3(board, aiTurn);
            case 4: return H4(board, aiTurn);
            default: return H1(board, aiTurn);
        }
    }

    private double H1(Tile[] board, int aiTurn)
    {
        int myPieces = boardManager.CountPieces(board, aiTurn);
        int oppPieces = boardManager.CountPieces(board, -aiTurn);
        return myPieces - oppPieces;
    }

    private double H2(Tile[] board, int aiTurn)
    {
        int myMobility = boardManager.FindSelectableTiles(board, aiTurn).Count;
        int oppMobility = boardManager.FindSelectableTiles(board, -aiTurn).Count;
        double mobilityScore = 0;
        if (myMobility + oppMobility != 0)
            mobilityScore = 100.0 * (myMobility - oppMobility) / (myMobility + oppMobility);

        double positionalScore = 0;
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (board[i].value == aiTurn)
                positionalScore += positionWeights[i];
            else if (board[i].value == -aiTurn)
                positionalScore -= positionWeights[i];
        }

        return mobilityScore * 2 + positionalScore;
    }

    private double H3(Tile[] board, int aiTurn)
    {
        int myCorners = 0, oppCorners = 0;
        foreach (int c in corners)
        {
            if (board[c].value == aiTurn) myCorners++;
            else if (board[c].value == -aiTurn) oppCorners++;
        }
        double cornerScore = 0;
        if (myCorners + oppCorners != 0)
            cornerScore = 100.0 * (myCorners - oppCorners) / (myCorners + oppCorners);

        double dangerScore = 0;
        for (int i = 0; i < corners.Length; i++)
        {
            if (board[corners[i]].value == Constants.Empty)
            {
                if (board[xSquares[i]].value == aiTurn)
                    dangerScore -= 25;
                else if (board[xSquares[i]].value == -aiTurn)
                    dangerScore += 25;

                foreach (int cs in cSquares[i])
                {
                    if (board[cs].value == aiTurn)
                        dangerScore -= 12.5;
                    else if (board[cs].value == -aiTurn)
                        dangerScore += 12.5;
                }
            }
        }

        double stabilityScore = CountStability(board, aiTurn) - CountStability(board, -aiTurn);

        int myPieces = boardManager.CountPieces(board, aiTurn);
        int oppPieces = boardManager.CountPieces(board, -aiTurn);
        double parityScore = 0;
        if (myPieces + oppPieces != 0)
            parityScore = 100.0 * (myPieces - oppPieces) / (myPieces + oppPieces);

        return cornerScore * 3 + dangerScore + stabilityScore * 2 + parityScore;
    }

    private double H4(Tile[] board, int aiTurn)
    {
        int totalPieces = boardManager.CountPieces(board, aiTurn) + boardManager.CountPieces(board, -aiTurn);

        int myMobility = boardManager.FindSelectableTiles(board, aiTurn).Count;
        int oppMobility = boardManager.FindSelectableTiles(board, -aiTurn).Count;
        double mobilityScore = 0;
        if (myMobility + oppMobility != 0)
            mobilityScore = 100.0 * (myMobility - oppMobility) / (myMobility + oppMobility);

        double positionalScore = 0;
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (board[i].value == aiTurn)
                positionalScore += positionWeights[i];
            else if (board[i].value == -aiTurn)
                positionalScore -= positionWeights[i];
        }

        int myCorners = 0, oppCorners = 0;
        foreach (int c in corners)
        {
            if (board[c].value == aiTurn) myCorners++;
            else if (board[c].value == -aiTurn) oppCorners++;
        }
        double cornerScore = 0;
        if (myCorners + oppCorners != 0)
            cornerScore = 100.0 * (myCorners - oppCorners) / (myCorners + oppCorners);

        int myPieces = boardManager.CountPieces(board, aiTurn);
        int oppPieces = boardManager.CountPieces(board, -aiTurn);
        double pieceScore = myPieces - oppPieces;

        double stabilityScore = CountStability(board, aiTurn) - CountStability(board, -aiTurn);

        if (totalPieces <= 20)
        {
            double logStability = System.Math.Log(1 + System.Math.Abs(stabilityScore)) * System.Math.Sign(stabilityScore);
            return mobilityScore * 5 + positionalScore * 2 + cornerScore * 3 + logStability * 2;
        }
        else if (totalPieces <= 44)
        {
            double sqrtPiece = System.Math.Pow(System.Math.Abs(pieceScore), 0.7) * System.Math.Sign(pieceScore);
            return cornerScore * 5 + positionalScore * 3 + mobilityScore * 2 + stabilityScore * 3 + sqrtPiece;
        }
        else
        {
            double sqrtPos = System.Math.Pow(System.Math.Abs(positionalScore), 0.5) * System.Math.Sign(positionalScore);
            return pieceScore * 5 + stabilityScore * 4 + cornerScore * 3 + sqrtPos;
        }
    }

    // ==========================================
    // 7. FUNCIONES AUXILIARES E INVARIANTES
    // ==========================================
    private int CountStability(Tile[] board, int player)
    {
        bool[] stable = new bool[Constants.NumTiles];

        foreach (int c in corners)
        {
            if (board[c].value == player)
                stable[c] = true;
        }

        if (stable[0])
        {
            for (int col = 1; col < 8; col++)
            {
                if (board[col].value == player) stable[col] = true;
                else break;
            }
            for (int row = 1; row < 8; row++)
            {
                if (board[row * 8].value == player) stable[row * 8] = true;
                else break;
            }
        }

        if (stable[7])
        {
            for (int col = 6; col >= 0; col--)
            {
                if (board[col].value == player) stable[col] = true;
                else break;
            }
            for (int row = 1; row < 8; row++)
            {
                if (board[row * 8 + 7].value == player) stable[row * 8 + 7] = true;
                else break;
            }
        }

        if (stable[56])
        {
            for (int col = 1; col < 8; col++)
            {
                if (board[56 + col].value == player) stable[56 + col] = true;
                else break;
            }
            for (int row = 6; row >= 0; row--)
            {
                if (board[row * 8].value == player) stable[row * 8] = true;
                else break;
            }
        }

        if (stable[63])
        {
            for (int col = 6; col >= 0; col--)
            {
                if (board[56 + col].value == player) stable[56 + col] = true;
                else break;
            }
            for (int row = 6; row >= 0; row--)
            {
                if (board[row * 8 + 7].value == player) stable[row * 8 + 7] = true;
                else break;
            }
        }

        int count = 0;
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (stable[i]) count++;
        }
        return count;
    }
}
