using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constants
{
    /* Constantes del juego */
    public const int NumTiles = 64;
    public const int TilesPerRow = 8;
    public const int Black = 1;
    public const int White = -1;
    public const int Empty = 0;
    public const int Start = 1;//No cambiar con player1 humano. Hay un bug
    public const string Player1 = "HUMAN";//AI o HUMAN
    public const string Player2 = "AI";//AI o HUMAN
    public const int PassTime = 2;

    /* Constantes de minimax */
    public const int MIN = -1;
    public const int MAX = 1;        
    
}
