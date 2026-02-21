using UnityEngine;

public class DragonSprite : MonoBehaviour
{
    public void Init()
    {
        GetComponentInParent<EnemyDragon>().Init();
    }
}