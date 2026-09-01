using System.Collections.Generic;
using UnityEngine;

public class HousePlacementManager : MonoBehaviour
{
    [Header("Casas a colocar")]
    public List<HouseOnGeodesic> houses = new List<HouseOnGeodesic>();

    [Header("Distancia mínima entre casas")]
    public float minimumDistance = 2.0f;


    private void Start()
    {
        PlaceAllHouses();
    }


    public void PlaceAllHouses()
    {
        List<Vector3> placedPositions = new List<Vector3>();

        foreach (HouseOnGeodesic house in houses)
        {
            if (house == null)
                continue;


            // ==================================================
            // 1. Calcular dónde debería estar la casa
            // ==================================================

            if (house.geodesic == null)
            {
                Debug.LogWarning(
                    "HousePlacementManager: Una casa no tiene geodésica asignada."
                );

                house.gameObject.SetActive(false);
                continue;
            }


            Vector2 roadPoint =
                house.geodesic.GetPointAt(house.positionOnRoad);


            Vector2 tangent =
                house.geodesic.GetTangentAt(house.positionOnRoad);

            tangent.Normalize();


            // ==================================================
            // 2. Normal a la carretera
            // ==================================================

            Vector2 normal =
                new Vector2(
                    -tangent.y,
                    tangent.x
                ).normalized;


            if (!house.leftSide)
            {
                normal = -normal;
            }


            // ==================================================
            // 3. Posición de la casa
            // ==================================================

            Vector2 housePoint =
                roadPoint + normal * house.offset;


            Vector3 proposedPosition =
                new Vector3(
                    housePoint.x * house.diskScale,
                    house.height,
                    housePoint.y * house.diskScale
                );


            // ==================================================
            // 4. Comprobar distancia con las casas colocadas
            // ==================================================

            bool tooClose = false;


            foreach (Vector3 placedPosition in placedPositions)
            {
                float distance =
                    Vector3.Distance(
                        proposedPosition,
                        placedPosition
                    );


                if (distance < minimumDistance)
                {
                    tooClose = true;
                    break;
                }
            }


            // ==================================================
            // 5. Decidir si colocar la casa
            // ==================================================

            if (tooClose)
            {
                house.gameObject.SetActive(false);

                Debug.Log(
                    "HousePlacementManager: Casa descartada por estar demasiado cerca de otra."
                );
            }
            else
            {
                house.gameObject.SetActive(true);

                house.PlaceHouse();

                placedPositions.Add(proposedPosition);
            }
        }
    }
}