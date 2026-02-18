using UnityEngine;

public class EnemyBaseClass : MonoBehaviour
{
    protected LevelZoneManager levelZone;

    public void Initialize(LevelZoneManager zone)
    {
        levelZone = zone;
    }

    protected void NotifyDeath()
    {
        if (levelZone != null)
        {
            levelZone.RegisterEnemyDeath();
        }
    }
}
