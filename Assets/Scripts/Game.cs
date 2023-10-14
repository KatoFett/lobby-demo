using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    public GameObject Prefab;
    public Camera Camera;
    public int PlayerID;

    private HeroKnight Character;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var worldPos = Camera.ScreenToWorldPoint(Input.mousePosition);
            var fixedPos = new Vector3(worldPos.x, worldPos.y);
            if (Character == null)
            {
                Character = Instantiate(Prefab, fixedPos, Quaternion.identity).GetComponent<HeroKnight>();
            }
            else
            {
                var command = new Command(PlayerID, new int[] { Character.ID }, CommandType.Move, fixedPos);
                Character.SendCommand(command);
            }
        }
    }
}
