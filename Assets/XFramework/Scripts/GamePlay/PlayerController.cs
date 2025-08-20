using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    [Header("视角限制")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private CharacterController controller;
    private Camera cam;
    private float pitch = 0f; // 竖直角度

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();

        // 隐藏并锁定鼠标光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleLook();
        HandleMove();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 水平旋转
        transform.Rotate(Vector3.up * mouseX);

        // 垂直旋转
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMove()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        Vector3 move = (transform.right * h + transform.forward * v).normalized;
        controller.SimpleMove(move * moveSpeed);
    }
}
