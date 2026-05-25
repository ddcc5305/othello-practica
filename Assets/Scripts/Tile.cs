using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile 
{
    /*
     * Si queremos ir a una casilla [i][j] equivale a board[i * 8 + j]
     */
    public int numTile;
    public int fila, columna;
    public int value = Constants.Empty;
}
