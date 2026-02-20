using UnityEngine;

public class ArmMovement : MonoBehaviour
{
    [SerializeField] private float yRange = 0.10f; // max distance DOWN from the top
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
    }
}
