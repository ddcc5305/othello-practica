using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Tile[] board = new Tile[Constants.NumTiles];
    public Node parent;
    public List<Node> childList = new List<Node>();
    public int type;//Constants.MIN o Constants.MAX
    public double utility;
    public double alfa;
    public double beta;
    public int move; // Casilla donde se ha movido para llegar a este nodo (-1 si es pase)

    public Node(Tile[] tiles)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            this.board[i] = new Tile();
            this.board[i].value = tiles[i].value;
        }

    }    

}

public class Player : MonoBehaviour
{
    public int turn;    
    private BoardManager boardManager;

    // ============================================================
    // CONFIGURACIÓN - Cambiar estos valores desde el Inspector de Unity
    // ============================================================
    [Header("Configuración de la IA")]
    [Range(2, 4)]
    [Tooltip("Profundidad del árbol (entre 2 y 4)")]
    public int maxDepth = 4;

    [Range(1, 4)]
    [Tooltip("Heurística a usar: 1=H1, 2=H2, 3=H3, 4=H4")]
    public int heuristic = 3;


    // Tabla de pesos posicionales para H2 y H4
    // Las esquinas valen mucho, las casillas adyacentes a esquinas penalizan
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

    // Posiciones de las 4 esquinas del tablero
    private static readonly int[] corners = { 0, 7, 56, 63 };

    // X-squares: casillas diagonales a cada esquina (peligrosas si la esquina está vacía)
    private static readonly int[] xSquares = { 9, 14, 49, 54 };

    // C-squares: casillas adyacentes a cada esquina en los bordes
    private static readonly int[][] cSquares = {
        new int[] { 1, 8 },     // Adyacentes a esquina 0
        new int[] { 6, 15 },    // Adyacentes a esquina 7
        new int[] { 48, 57 },   // Adyacentes a esquina 56
        new int[] { 55, 62 }    // Adyacentes a esquina 63
    };

    void Start()
    {
        boardManager = GameObject.FindGameObjectWithTag("BoardManager").GetComponent<BoardManager>();
    }
       
    /*
     * Entrada: Dado un tablero
     * Salida: Posición donde mueve  
     */
    public int SelectTile(Tile[] board)
    {        
        // Guardamos el turn original para restaurarlo antes del return
        int originalTurn = turn;

        // Generamos el nodo raíz del árbol (MAX = la IA quiere maximizar)
        Node root = new Node(board);
        root.type = Constants.MAX;
        root.alfa = double.NegativeInfinity;
        root.beta = double.PositiveInfinity;

        // Generamos el árbol MINIMAX recursivamente hasta MAX_DEPTH
        GenerateTree(root, 0, turn);

        // Evaluamos el árbol con Minimax + poda alfa-beta
        MinimaxAlphaBeta(root, double.NegativeInfinity, double.PositiveInfinity, turn);

        // Buscamos el hijo con la mejor utilidad (MAX elige el mayor)
        int bestMove = -1;
        double bestValue = double.NegativeInfinity;
        foreach (Node child in root.childList)
        {
            if (child.utility > bestValue)
            {
                bestValue = child.utility;
                bestMove = child.move;
            }
        }

        // Restauramos turn a su valor original
        turn = originalTurn;

        // Si no se encontró movimiento válido (no debería pasar), usar el primero disponible
        if (bestMove == -1)
        {
            List<int> selectableTiles = boardManager.FindSelectableTiles(board, turn);
            if (selectableTiles.Count > 0)
                bestMove = selectableTiles[0];
        }

        return bestMove;
    }

    // ============================================================
    // GENERACIÓN DEL ÁRBOL MINIMAX
    // ============================================================

    /*
     * Genera el árbol MINIMAX recursivamente
     * node: nodo actual
     * depth: profundidad actual (0 = raíz)
     * currentTurn: turno del jugador que mueve en este nivel
     */
    private void GenerateTree(Node node, int depth, int currentTurn)
    {
        // Si hemos alcanzado la profundidad máxima, es un nodo terminal
        if (depth >= maxDepth)
            return;

        // Calculamos los movimientos posibles para el jugador actual
        List<int> selectableTiles = boardManager.FindSelectableTiles(node.board, currentTurn);

        if (selectableTiles.Count == 0)
        {
            // El jugador actual no puede mover → pasa el turno
            // Generamos un nodo hijo con el mismo tablero (Nota 1 del enunciado)
            Node child = new Node(node.board);
            child.parent = node;
            child.type = (node.type == Constants.MAX) ? Constants.MIN : Constants.MAX;
            child.move = -1; // Indica que es un pase
            node.childList.Add(child);

            // Continuamos generando con el turno del otro jugador
            GenerateTree(child, depth + 1, -currentTurn);
            return;
        }

        // Para cada movimiento posible, generamos un nodo hijo
        foreach (int s in selectableTiles)
        {
            // Creamos un nuevo nodo hijo con copia del tablero padre
            Node child = new Node(node.board);
            child.parent = node;
            child.type = (node.type == Constants.MAX) ? Constants.MIN : Constants.MAX;
            child.move = s; // Guardamos qué movimiento lleva a este nodo

            // Aplicamos el movimiento sobre el tablero del hijo
            boardManager.Move(child.board, s, currentTurn);

            // Lo añadimos a la lista de hijos del padre
            node.childList.Add(child);

            // Generamos recursivamente el siguiente nivel
            GenerateTree(child, depth + 1, -currentTurn);
        }
    }

    // ============================================================
    // MINIMAX CON PODA ALFA-BETA
    // ============================================================

    /*
     * Evalúa el árbol con Minimax + poda alfa-beta
     * node: nodo actual a evaluar
     * alpha: mejor valor encontrado para MAX en el camino
     * beta: mejor valor encontrado para MIN en el camino
     * aiTurn: color de la IA (para evaluar la heurística desde su perspectiva)
     * Retorna: la utilidad del nodo
     */
    private double MinimaxAlphaBeta(Node node, double alpha, double beta, int aiTurn)
    {
        // Si es nodo terminal (sin hijos = hoja del árbol), calculamos su utilidad
        if (node.childList.Count == 0)
        {
            node.utility = EvaluateHeuristic(node.board, aiTurn);
            return node.utility;
        }

        if (node.type == Constants.MAX)
        {
            // Nodo MAX: la IA quiere maximizar
            double maxEval = double.NegativeInfinity;

            foreach (Node child in node.childList)
            {
                double eval = MinimaxAlphaBeta(child, alpha, beta, aiTurn);

                if (eval > maxEval)
                    maxEval = eval;

                // Actualizar alfa (mejor opción para MAX)
                if (eval > alpha)
                    alpha = eval;

                // Poda beta: si alfa >= beta, no merece la pena seguir explorando
                if (alpha >= beta)
                    break;
            }

            node.utility = maxEval;
            node.alfa = alpha;
            return maxEval;
        }
        else
        {
            // Nodo MIN: el oponente quiere minimizar
            double minEval = double.PositiveInfinity;

            foreach (Node child in node.childList)
            {
                double eval = MinimaxAlphaBeta(child, alpha, beta, aiTurn);

                if (eval < minEval)
                    minEval = eval;

                // Actualizar beta (mejor opción para MIN)
                if (eval < beta)
                    beta = eval;

                // Poda alfa: si alfa >= beta, no merece la pena seguir explorando
                if (alpha >= beta)
                    break;
            }

            node.utility = minEval;
            node.beta = beta;
            return minEval;
        }
    }

    // ============================================================
    // SELECCIÓN DE HEURÍSTICA
    // ============================================================

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

    // ============================================================
    // H1: HEURÍSTICA BÁSICA - Diferencia de fichas
    // ============================================================
    // Criterio simple: maximizar la diferencia de fichas propias vs oponente
    private double H1(Tile[] board, int aiTurn)
    {
        int myPieces = boardManager.CountPieces(board, aiTurn);
        int oppPieces = boardManager.CountPieces(board, -aiTurn);
        return myPieces - oppPieces;
    }

    // ============================================================
    // H2: HEURÍSTICA ESTRATÉGICA - Movilidad + Valor posicional
    // ============================================================
    // Combina dos criterios estratégicos:
    // - Movilidad: tener más movimientos disponibles que el oponente
    // - Valor posicional: ocupar casillas estratégicas (esquinas, bordes)
    private double H2(Tile[] board, int aiTurn)
    {
        // --- Movilidad normalizada ---
        int myMobility = boardManager.FindSelectableTiles(board, aiTurn).Count;
        int oppMobility = boardManager.FindSelectableTiles(board, -aiTurn).Count;
        double mobilityScore = 0;
        if (myMobility + oppMobility != 0)
            mobilityScore = 100.0 * (myMobility - oppMobility) / (myMobility + oppMobility);

        // --- Valor posicional ---
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

    // ============================================================
    // H3: HEURÍSTICA AVANZADA - Estabilidad + Esquinas + Paridad normalizada
    // ============================================================
    // NO es combinación lineal de H1 y H2.
    // Criterios propios:
    // - Control de esquinas (normalizadas)
    // - Penalización de X-squares y C-squares cuando la esquina adyacente está vacía
    // - Estabilidad: fichas que no pueden ser volteadas
    // - Paridad normalizada (distinta a la diferencia simple de H1)
    private double H3(Tile[] board, int aiTurn)
    {
        // 1. Control de esquinas (normalizado)
        int myCorners = 0, oppCorners = 0;
        foreach (int c in corners)
        {
            if (board[c].value == aiTurn) myCorners++;
            else if (board[c].value == -aiTurn) oppCorners++;
        }
        double cornerScore = 0;
        if (myCorners + oppCorners != 0)
            cornerScore = 100.0 * (myCorners - oppCorners) / (myCorners + oppCorners);

        // 2. Penalización de X-squares y C-squares
        // Solo penalizamos cuando la esquina adyacente está VACÍA (riesgo de regalarla)
        double dangerScore = 0;
        for (int i = 0; i < corners.Length; i++)
        {
            if (board[corners[i]].value == Constants.Empty)
            {
                // X-square: casilla diagonal a la esquina
                if (board[xSquares[i]].value == aiTurn)
                    dangerScore -= 25;
                else if (board[xSquares[i]].value == -aiTurn)
                    dangerScore += 25;

                // C-squares: casillas adyacentes en los bordes
                foreach (int cs in cSquares[i])
                {
                    if (board[cs].value == aiTurn)
                        dangerScore -= 12.5;
                    else if (board[cs].value == -aiTurn)
                        dangerScore += 12.5;
                }
            }
        }

        // 3. Estabilidad (fichas estables que no se pueden voltear)
        double stabilityScore = CountStability(board, aiTurn) - CountStability(board, -aiTurn);

        // 4. Paridad de fichas normalizada (no es la diferencia simple de H1)
        int myPieces = boardManager.CountPieces(board, aiTurn);
        int oppPieces = boardManager.CountPieces(board, -aiTurn);
        double parityScore = 0;
        if (myPieces + oppPieces != 0)
            parityScore = 100.0 * (myPieces - oppPieces) / (myPieces + oppPieces);

        return cornerScore * 3 + dangerScore + stabilityScore * 2 + parityScore;
    }

    // ============================================================
    // H4: HEURÍSTICA ADAPTATIVA POR FASE DE JUEGO (Opcional)
    // ============================================================
    // Adapta los pesos de evaluación según la fase de la partida:
    // - Inicio (≤20 fichas): prioriza movilidad y posición
    // - Medio (21-44 fichas): prioriza control posicional y esquinas
    // - Final (≥45 fichas): prioriza diferencia de fichas y estabilidad
    // Usa funciones NO LINEALES (log, pow) para que no sea una simple combinación lineal
    private double H4(Tile[] board, int aiTurn)
    {
        int totalPieces = boardManager.CountPieces(board, aiTurn) + boardManager.CountPieces(board, -aiTurn);

        // --- Movilidad normalizada ---
        int myMobility = boardManager.FindSelectableTiles(board, aiTurn).Count;
        int oppMobility = boardManager.FindSelectableTiles(board, -aiTurn).Count;
        double mobilityScore = 0;
        if (myMobility + oppMobility != 0)
            mobilityScore = 100.0 * (myMobility - oppMobility) / (myMobility + oppMobility);

        // --- Valor posicional ---
        double positionalScore = 0;
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (board[i].value == aiTurn)
                positionalScore += positionWeights[i];
            else if (board[i].value == -aiTurn)
                positionalScore -= positionWeights[i];
        }

        // --- Esquinas ---
        int myCorners = 0, oppCorners = 0;
        foreach (int c in corners)
        {
            if (board[c].value == aiTurn) myCorners++;
            else if (board[c].value == -aiTurn) oppCorners++;
        }
        double cornerScore = 0;
        if (myCorners + oppCorners != 0)
            cornerScore = 100.0 * (myCorners - oppCorners) / (myCorners + oppCorners);

        // --- Diferencia de fichas ---
        int myPieces = boardManager.CountPieces(board, aiTurn);
        int oppPieces = boardManager.CountPieces(board, -aiTurn);
        double pieceScore = myPieces - oppPieces;

        // --- Estabilidad ---
        double stabilityScore = CountStability(board, aiTurn) - CountStability(board, -aiTurn);

        // ===== FASES DEL JUEGO CON PESOS ADAPTATIVOS NO LINEALES =====
        if (totalPieces <= 20)
        {
            // FASE INICIAL: priorizar movilidad y posición, las fichas no importan
            double logStability = System.Math.Log(1 + System.Math.Abs(stabilityScore))
                                  * System.Math.Sign(stabilityScore);
            return mobilityScore * 5 + positionalScore * 2 + cornerScore * 3 + logStability * 2;
        }
        else if (totalPieces <= 44)
        {
            // FASE MEDIA: priorizar control posicional y esquinas
            double sqrtPiece = System.Math.Pow(System.Math.Abs(pieceScore), 0.7)
                               * System.Math.Sign(pieceScore);
            return cornerScore * 5 + positionalScore * 3 + mobilityScore * 2
                   + stabilityScore * 3 + sqrtPiece;
        }
        else
        {
            // FASE FINAL (≥45 fichas): priorizar diferencia de fichas y estabilidad
            double sqrtPos = System.Math.Pow(System.Math.Abs(positionalScore), 0.5)
                             * System.Math.Sign(positionalScore);
            return pieceScore * 5 + stabilityScore * 4 + cornerScore * 3 + sqrtPos;
        }
    }

    // ============================================================
    // FUNCIÓN AUXILIAR: Contar fichas estables de un jugador
    // ============================================================
    // Una ficha estable es aquella que no puede ser volteada.
    // Las esquinas son siempre estables.
    // Las fichas en bordes adyacentes a una esquina propia también lo son.
    private int CountStability(Tile[] board, int player)
    {
        bool[] stable = new bool[Constants.NumTiles];

        // Las esquinas capturadas son siempre estables
        foreach (int c in corners)
        {
            if (board[c].value == player)
                stable[c] = true;
        }

        // Esquina 0 (fila 0, columna 0): expandir por borde inferior y borde izquierdo
        if (stable[0])
        {
            // Borde inferior hacia la derecha (fila 0: posiciones 1, 2, 3, ...)
            for (int col = 1; col < 8; col++)
            {
                if (board[col].value == player) stable[col] = true;
                else break;
            }
            // Borde izquierdo hacia arriba (columna 0: posiciones 8, 16, 24, ...)
            for (int row = 1; row < 8; row++)
            {
                if (board[row * 8].value == player) stable[row * 8] = true;
                else break;
            }
        }

        // Esquina 7 (fila 0, columna 7): expandir por borde inferior y borde derecho
        if (stable[7])
        {
            // Borde inferior hacia la izquierda (fila 0: posiciones 6, 5, 4, ...)
            for (int col = 6; col >= 0; col--)
            {
                if (board[col].value == player) stable[col] = true;
                else break;
            }
            // Borde derecho hacia arriba (columna 7: posiciones 15, 23, 31, ...)
            for (int row = 1; row < 8; row++)
            {
                if (board[row * 8 + 7].value == player) stable[row * 8 + 7] = true;
                else break;
            }
        }

        // Esquina 56 (fila 7, columna 0): expandir por borde superior y borde izquierdo
        if (stable[56])
        {
            // Borde superior hacia la derecha (fila 7: posiciones 57, 58, 59, ...)
            for (int col = 1; col < 8; col++)
            {
                if (board[56 + col].value == player) stable[56 + col] = true;
                else break;
            }
            // Borde izquierdo hacia abajo (columna 0: posiciones 48, 40, 32, ...)
            for (int row = 6; row >= 0; row--)
            {
                if (board[row * 8].value == player) stable[row * 8] = true;
                else break;
            }
        }

        // Esquina 63 (fila 7, columna 7): expandir por borde superior y borde derecho
        if (stable[63])
        {
            // Borde superior hacia la izquierda (fila 7: posiciones 62, 61, 60, ...)
            for (int col = 6; col >= 0; col--)
            {
                if (board[56 + col].value == player) stable[56 + col] = true;
                else break;
            }
            // Borde derecho hacia abajo (columna 7: posiciones 55, 47, 39, ...)
            for (int row = 6; row >= 0; row--)
            {
                if (board[row * 8 + 7].value == player) stable[row * 8 + 7] = true;
                else break;
            }
        }

        // Contamos las fichas estables
        int count = 0;
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (stable[i]) count++;
        }
        return count;
    }

}
