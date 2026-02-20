using UnityEngine;

public class ArmMovement : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float angleRange = 20f;   // degrees up/down
    [SerializeField] private float speed = 2f;         // cycles per second-ish

    private float startZ;

    private void Awake()
    {
        startZ = transform.localEulerAngles.z;
    }

    private void Update()
    {
        float t = Mathf.Sin(Time.time * speed);
        float z = startZ + (t * angleRange);

        transform.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    /*    [SerializeField] private float yRange = 0.10f; // max distance DOWN from the top
        [SerializeField] private float speed = 2f;     // how fast

        private Vector3 topLocalPos;

        private void Awake()
        {
            topLocalPos = transform.localPosition; // arm position in editor = TOP
        }

        private void Update()
        {
            // 0..1 loop (never negative). Starts at 0, increases -> moves DOWN first.
            float t01 = (1f - Mathf.Cos(Time.time * speed)) * 0.5f;

            float downOffset = t01 * yRange;

            transform.localPosition = new Vector3(
                topLocalPos.x,
                topLocalPos.y - downOffset,
                topLocalPos.z
            );
        }*/
}
