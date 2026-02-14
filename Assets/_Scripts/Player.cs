using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    [Header("Internal Components")]
    [SerializeField] private Animator animator;

    [Header("Player Properties")]
    [SerializeField] private float playerSpeed;

    private Vector2 moveInput;

    private void Update()
    {
        PlayerMovement();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        FlipPlayer();
    }

    private void PlayerMovement()
    {
        Vector2 direction = Vector2.ClampMagnitude(moveInput, 1f);
        transform.position += (Vector3)(direction * playerSpeed * Time.deltaTime);
    }

    private void FlipPlayer()
    {
        if (Mathf.Abs(moveInput.x) > 0.01)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (moveInput.x > 0f ? 1f : -1f);
            transform.localScale = scale;
        }
    }
}
