using UnityEngine;

public class ConveyorObjectTracker : MonoBehaviour
{
    private ConveyorObjectSpawner spawner;
    private Collider2D col;
    private LayerMask groundLayer;
    private float destroyDelayOffGround = 3f;
    private float offGroundTimer = 0f;

    public void Initialize(ConveyorObjectSpawner parentSpawner, LayerMask ground, int wallLayer)
    {
        spawner = parentSpawner;
        groundLayer = ground;
        col = GetComponent<Collider2D>();

        // Ignore collisions between this object's layer and the wall layer
        Physics2D.IgnoreLayerCollision(gameObject.layer, wallLayer, true);
    }

    private void Update()
    {
        if (col == null) return;

        if (!Physics2D.IsTouchingLayers(col, groundLayer))
        {
            offGroundTimer += Time.deltaTime;

            if (offGroundTimer >= destroyDelayOffGround)
                Destroy(gameObject);
        }
        else
        {
            offGroundTimer = 0f;
        }
    }

    private void OnDestroy()
    {
        if (spawner != null)
            spawner.OnTrackedObjectDestroyed();
    }
}
