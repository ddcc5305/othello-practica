using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
    public GameObject controller;
    public Tile tileInfo = new Tile();
              
    private void Awake()
    {     
        Vector3 pos = transform.position;
        tileInfo.fila = (int)(pos.z + 3.5);
        tileInfo.columna = (int)(pos.x + 3.5);
        this.tileInfo.numTile = tileInfo.fila * Constants.TilesPerRow + tileInfo.columna;
    }

    //Hacemos clic en casilla
    private void OnMouseDown()
    {
        if(Constants.Player1.CompareTo("AI")!=0 || Constants.Player2.CompareTo("AI")!=0)
            controller.GetComponent<Controller>().ClickOnTile(tileInfo.numTile);
    }      
 
}
