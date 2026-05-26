using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{

    public void PrintBoard(Tile[] board)
    {
         for (int row = Constants.TilesPerRow-1; row >= 0 ; row--)
                Debug.Log(board[row * Constants.TilesPerRow].value+ "\t" + board[row * Constants.TilesPerRow + 1].value + "\t" + board[row * Constants.TilesPerRow + 2].value + "\t" + board[row * Constants.TilesPerRow + 3].value
                    + "\t" + board[row * Constants.TilesPerRow + 4].value + "\t" + board[row * Constants.TilesPerRow + 5].value + "\t" + board[row * Constants.TilesPerRow + 6].value + "\t" + board[row * Constants.TilesPerRow + 7].value);
    }

    public Tile[] CreateCopy(Tile[] board)
    {
        Tile[] copy = new Tile[board.Length];

        for (int i = 0; i < board.Length; i++)
            //copy[i] = board[i];
            {
                copy[i] = new Tile();
                copy[i].value = board[i].value;
            }

        return copy;        
    }

    /*
     * Entrada: Dado un tablero y un turno (negras o blancas)
     * Salida: Número de piezas que hay en el tablero de ese color  
     */
    public int CountPieces(Tile[] board, int turn)
    {
        int cont = 0;

        for (int tile = 0; tile < Constants.NumTiles; tile++)
        {
            if (board[tile].value == turn)
                cont++;
        }

        return cont;
    }


    bool Exists(List<int> selectableTiles, int val)
    {
        foreach(int i in selectableTiles)
        {
            if (i == val)
                return true;            
        }
        return false;
    }

    bool CheckNext(Tile[] board, List<int> selectableTiles, int val, int turn)
    {
        //Si hay casilla y está vacía, puedo ir
        if (board[val].value == Constants.Empty)
        {
            //Si no la he marcado ya como seleccionable, la marco
            if (!Exists(selectableTiles,val))            
                selectableTiles.Add(val);

            return true;
        }

        //Si hay casilla pero hay una ficha del mismo color, no puedo ir
        if (board[val].value == turn)
            return true;

        return false;
    }


    /*
     * Entrada: Dado un tablero, una posición de ficha y un turno (negras o blancas)
     * Salida: Listado de casillas donde te puedes mover  
     */
    public List<int> FindSelectableTiles(Tile[] board, int turn)
    {
        List<int> selectableTiles = new List<int>();

        for (int tile = 0; tile < Constants.NumTiles; tile++)
        {
            if (board[tile].value == turn)
            {
  
                //DERECHA: Si no estoy en una esquina derecha y si hay una ficha contraria a mi derecha. La comprovació 2 és simplemente perquè no pete la 3
                if (((tile + 1) % 8 != 0) && ((tile + 1) <= 63) && board[tile + 1].value == -turn)
                {

                    int n = tile + 1;
                    bool stop = false;
                    while (!stop)
                    {
                        //Miro a la derecha de mi vecino. Si mi vecino está en una esquina derecha, paro
                        if ((n + 1) % 8 == 0)
                            stop = true;

                        else
                            stop = CheckNext(board, selectableTiles, n + 1, turn);

                        n = n + 1;
                    }
                }

                //IZQUIERDA: Si no estoy en una esquina izquierda y si hay una ficha contraria a mi izquierda. La comprovació 2 és simplemente perquè no pete la 3
                if ((tile % 8 != 0) && ((tile - 1) >= 0) && board[tile - 1].value == -turn)
                {
                    int n = tile - 1;
                    bool stop = false;
                    while (!stop)
                    {
                        //Miro a la izquierda de mi vecino. Si mi vecino está en una esquina izquierda, paro
                        if (n % 8 == 0)
                            stop = true;

                        else
                            stop = CheckNext(board, selectableTiles, n - 1, turn);

                        n = n - 1;
                    }
                }

                //ARRIBA: Si no estoy en la última fila y si hay una ficha contraria arriba de la mia. La comprovació 2 és simplemente perquè no pete la 3
                if ((tile < 56) && ((tile + 8) <= 63) && board[tile + 8].value == -turn)
                {
                    int n = tile + 8;
                    bool stop = false;
                    while (!stop)
                    {
                        //Miro arriba de mi vecino. Si mi vecino está en la última fila, paro
                        if (n >= 56)
                            stop = true;

                        else
                            stop = CheckNext(board, selectableTiles, n + 8, turn);

                        n = n + 8;
                    }
                }

                //ABAJO: Si no estoy en la primera fila y si hay una ficha contraria abajo de la mia. La comprovació 2 és simplemente perquè no pete la 3
                if ((tile > 7) && ((tile - 8) >= 0) && board[tile - 8].value == -turn)
                {
                    int n = tile - 8;
                    bool stop = false;
                    while (!stop)
                    {
                        //Miro abajo de mi vecino. Si mi vecino está en la primera fila, paro
                        if (n <= 7)
                            stop = true;

                        else
                            stop = CheckNext(board, selectableTiles, n - 8, turn);

                        n = n - 8;
                    }
                }

                //DIAGONAL DERECHA-ABAJO: Si no estoy en la esquina derecha ni en la primera fila y si hay una ficha contraria derecha-abajo mia. La comprovació 3 és perquè no pete la 4
                if (((tile + 1) % 8 != 0) && (tile > 7) && ((tile - 8 + 1) >= 0) && board[tile - 8 + 1].value == -turn)
                {

                    int n = tile - 8 + 1;//este és el veí
                    bool stop = false;
                    while (!stop)
                    {
                        //Si mi vecino está a la derecha o en la primera fila, paro
                        if (((n + 1) % 8 == 0) || (n <= 7))
                            stop = true;

                        else
                            stop = CheckNext(board, selectableTiles, n - 8 + 1, turn);

                        n = n - 8 + 1;
                    }
                }


                //DIAGONAL DERECHA-ARRIBA: Si no estoy en la esquina derecha ni en la última fila y si hay una ficha contraria derecha-arriba mia. La comprovació 3 és perquè no pete la 4
                if (((tile + 1) % 8 != 0) && (tile < 56) && ((tile + 8 + 1) <= 63) && board[tile + 8 + 1].value == -turn)
                {

                    int n = tile + 8 + 1;//este és el veí
                    bool stop = false;
                    while (!stop)
                    {
                        //Si me paso de rango, paro
                        //Si mi vecino está a la derecha o en la última fila, paro
                        if (((n + 1) % 8 == 0) || (n >= 56))
                            stop = true;

                        else
                            stop = CheckNext(board, selectableTiles, n + 8 + 1, turn);

                        n = n + 8 + 1;
                    }
                }


                //DIAGONAL IZQUIERDA-ABAJO: Si no estoy en la esquina izquierda ni en la primera fila y si hay una ficha contraria izquierda-abajo mia. La comprovació 3 és perquè no pete la 4
                if ((tile % 8 != 0) && (tile > 7) && ((tile - 8 - 1) >= 0) && board[tile - 8 - 1].value == -turn)
                {
                    int n = tile - 8 - 1;//este és el veí
                    bool stop = false;
                    while (!stop)
                    {
                        //Si mi vecino está a la izquierda o en la primera fila, paro
                        if ((n % 8 == 0) || (n <= 7))
                            stop = true;

                        else
                            stop = CheckNext(board, selectableTiles, n - 8 - 1, turn);

                        n = n - 8 - 1;
                    }
                }


                //DIAGONAL IZQUIERDA-ARRIBA: Si no estoy en la esquina izquierda ni en la última fila y si hay una ficha contraria izquierda-abajo mia. La comprovació 3 és perquè no pete la 4
                if ((tile % 8 != 0) && (tile < 56) && ((tile + 8 - 1) <= 63) && board[tile + 8 - 1].value == -turn)
                {
                    int n = tile + 8 - 1;//este és el veí
                    bool stop = false;
                    while (!stop)
                    {
                        //Si mi vecino está a la izquierda o en la última fila, paro
                        if ((n % 8 == 0) || (n >= 56))
                            stop = true;

                        else
                            stop = CheckNext(board, selectableTiles, n + 8 - 1, turn);

                        n = n + 8 - 1;
                    }
                }
                             
            }


        }

        return selectableTiles;
    }

    bool CheckNextCandidates(Tile[] board, List<int> candidates, List<int> swappable, int val, int turn)
    {
        //Si es del color propio, pinto los candidatos y paro
        if (board[val].value == turn)
        {
            foreach (int c in candidates)
            {
                //board[c].value = turn;
                swappable.Add(c);
            }

            return true;
        }

        //Si está vacía, paro
        if (board[val].value == Constants.Empty)
            return true;

        //Si llega hasta aquí, es que es del color oponente, añado y continuo
        candidates.Add(val);
        return false;

    }


    /* 
     * Entrada: Dado un tablero, una posición de ficha y un turno (negras o blancas)
     * Salida: Listado de posiciones del oponente que cambiarían de color          
     */
    public List<int> FindSwappablePieces(Tile[] board, int tile, int turn)
    {
        List<int> candidates = new List<int>();
        List<int> swappable = new List<int>();

        //DERECHA: Si hay una casilla y si hay una ficha contraria en esa casilla. La comprovació 2 és perquè no pete la 3
        if (((tile + 1) % 8 != 0) && ((tile + 1) <= 63) && board[tile + 1].value == -turn)
        {
            candidates.Clear();
            candidates.Add(tile + 1);
            int n = tile + 1;
            bool stop = false;
            while (!stop)
            {
                //Si ya no hay casillas a la derecha de mi vecino, paro
                if ((n + 1) % 8 == 0)
                    stop = true;

                else                
                    stop = CheckNextCandidates(board, candidates, swappable, n + 1, turn);
                
                n = n + 1;

            }

        }

        //IZQUIERDA: Si hay una casilla y si hay una ficha contraria en esa casilla. La comprovació 2 és perquè no pete la 3       
        if ((tile % 8 != 0) && ((tile - 1) >= 0) && board[tile - 1].value == -turn)
        {
            candidates.Clear();
            candidates.Add(tile - 1);
            int n = tile - 1;
            bool stop = false;
            while (!stop)
            {
                //Si ya no hay casillas a la izquierda de mi vecino, paro
                if (n % 8 == 0)
                    stop = true;

                else
                    stop = CheckNextCandidates(board, candidates, swappable, n - 1, turn);

                n = n - 1;

            }
        }

        //ARRIBA: Si hay una casilla y si hay una ficha contraria en esa casilla. La comprovació 2 és perquè no pete la 3 però en este cas, no faria falta
        if ((tile < 56) && ((tile + 8) <= 63) && board[tile + 8].value == -turn)
        {
            candidates.Clear();
            candidates.Add(tile + 8);
            int n = tile + 8;
            bool stop = false;
            while (!stop)
            {
                //Si ya no hay casillas arriba de mi vecino, paro
                if (n >= 56)//>= 56)
                    stop = true;

                else
                    stop = CheckNextCandidates(board, candidates, swappable, n + 8, turn);

                n = n + 8;

            }
        }

        //ABAJO: Si hay una casilla y si hay una ficha contraria en esa casilla. La comprovació 2 és perquè no pete la 3 però en este cas, no faria falta
        if ((tile > 7) && ((tile - 8) >= 0) && board[tile - 8].value == -turn)
        {
            candidates.Clear();
            candidates.Add(tile - 8);
            int n = tile - 8;
            bool stop = false;
            while (!stop)
            {
                //Si ya no hay casillas abajo de mi vecino, paro
                if (n <= 7)
                    stop = true;

                else
                    stop = CheckNextCandidates(board, candidates, swappable, n - 8, turn);

                n = n - 8;

            }
        }

        //DIAGONAL DERECHA-ABAJO: Si no estoy ni en la esquina derecha ni en la primera fila y si mi vecino derecha-abajo tiene una ficha del oponente. La comprovació 3 és perquè no pete la 4
        if (((tile + 1) % 8 != 0) && (tile > 7) && ((tile - 8 + 1) >= 0) && board[tile - 8 + 1].value == -turn)
        {
            candidates.Clear();
            candidates.Add(tile - 8 + 1);
            int n = tile - 8 + 1;
            bool stop = false;
            while (!stop)
            {
                //Si mi vecino está en la esquina derecha o en la primera fila, paro
                if (((n + 1) % 8 == 0) || (n <= 7))
                    stop = true;

                else
                    stop = CheckNextCandidates(board, candidates, swappable, n - 8 + 1, turn);

                n = n - 8 + 1;

            }
        }


        //DIAGONAL DERECHA-ARRIBA: Si no estoy ni en la esquina derecha ni en la última fila y si mi vecino derecha-arriba tiene una ficha del oponente. La comprovació 3 és perquè no pete la 4
        if (((tile + 1) % 8 != 0) && (tile < 56) && ((tile + 8 + 1) <= 63) && board[tile + 8 + 1].value == -turn)
        {
            candidates.Clear();
            candidates.Add(tile + 8 + 1);
            int n = tile + 8 + 1;
            bool stop = false;
            while (!stop)
            {
                //Si mi vecino está en la esquina derecha o en la última fila, paro
                if (((n + 1) % 8 == 0) || (n >= 56))
                    stop = true;

                else
                    stop = CheckNextCandidates(board, candidates, swappable, n + 8 + 1, turn);

                n = n + 8 + 1;

            }
        }


        //DIAGONAL IZQUIERDA-ABAJO: Si no estoy ni en la esquina izquierda ni en la primera fila y si mi vecino izquierda-abajo tiene una ficha del oponente. La comprovació 3 és perquè no pete la 4
        if ((tile % 8 != 0) && (tile > 7) && ((tile - 8 - 1) >= 0) && board[tile - 8 - 1].value == -turn)
        {
            candidates.Clear();
            candidates.Add(tile - 8 - 1);
            int n = tile - 8 - 1;
            bool stop = false;
            while (!stop)
            {
                //Si mi vecino está en la esquina izquierda o en la primera fila, paro
                if ((n % 8 == 0) || (n <= 7))
                    stop = true;

                else
                    stop = CheckNextCandidates(board, candidates, swappable, n - 8 - 1, turn);

                n = n - 8 - 1;

            }
        }

        //DIAGONAL IZQUIERDA-ARRIBA: Si no estoy ni en la esquina izquierda ni en la última fila y si mi vecino izquierda-arriba tiene una ficha del oponente. La comprovació 3 és perquè no pete la 4
        if ((tile % 8 != 0) && (tile < 56) && ((tile + 8 - 1) <= 63) && board[tile + 8 - 1].value == -turn)
        {
            candidates.Clear();
            candidates.Add(tile + 8 - 1);
            int n = tile + 8 - 1;
            bool stop = false;
            while (!stop)
            {
                //Si mi vecino está en la esquina izquierda o en la última fila, paro
                if ((n % 8 == 0) || (n >= 56))
                    stop = true;

                else
                    stop = CheckNextCandidates(board, candidates, swappable, n + 8 - 1, turn);

                n = n + 8 - 1;

            }
        }

        return swappable;

    }

    /* 
     * Entrada: Dado un tablero, una posición de ficha y un turno (negras o blancas)
     * Funcionalidad: Modifica el tablero con el estado que queda después de hacer el movimiento          
     */
    public void Move(Tile[] board, int clickedTile, int turn)
    {
        board[clickedTile].value = turn;

        List<int> swappableList = FindSwappablePieces(board, clickedTile, turn);
        foreach (int s in swappableList)
        {
            board[s].value = turn;
        }
        
    }

}
