using UnityEngine;

public class PlayerTracker : MonoBehaviour
{
    [Header("Script GridGenerator")]
    [SerializeField] private GridGenerator gridGenerator;

    [Header("XROrigin")]
    [SerializeField] private Transform xrOrigin;

    [Header("Radio de acción")]
    [SerializeField] private int radius = 2;

    //Inicializamos con un valor imposible
    private Vector2Int previousCell = new Vector2Int(-1, -1);

    private void Update()
    {
        //Obtenemos la casilla en la que está el jugador. 
        Vector2Int currentCell = gridGenerator.WorldToGrid(xrOrigin.position);

        //Si el jugador sigue en la misma casilla no se actualizan los colliders.
        if (currentCell == previousCell)
            return;
        
        //Desactiva los colliders activados en la posición anterior del jugador 
        UpdateColliders(previousCell, false);

        //Activa los colliders de la nueva posición
        UpdateColliders(currentCell, true);

        previousCell = currentCell;
    }


    /// <summary>
    /// Activa o desactiva los colliders de las casillas que estén alrededor de ese radio
    /// </summary>
    private void UpdateColliders(Vector2Int center, bool state)
    {
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int z = center.y - radius; z <= center.y + radius; z++)
            {
                //Si no está en el grid esas posiciones seguimos, sobre todo importante para los cubos
                //de los márgenes.
                if (!gridGenerator.IsInsideGrid(x, z))
                    continue;

                //Activamos o desactivamos el box collider según el estado pasado.
                BoxCollider box = gridGenerator.Grid[x, z].GetComponent<BoxCollider>();
                box.enabled = state;
            }
        }
    }
}