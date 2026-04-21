using Sirenix.OdinInspector;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonControllerRecover : MonoBehaviour
{
    [FoldoutGroup ("References")]
    public InputSystem_Actions inputs;
    [FoldoutGroup("References")]
    private CharacterController controller;
    [FoldoutGroup("References")]
    public CinemachineCamera characterCamera;
    [FoldoutGroup("References")]
    public Animator animator;


    [FoldoutGroup("Controller")]
    public float moveSpeed = 5f;
    [FoldoutGroup("Controller")]
    public float rotationSpeed = 200f;
    [FoldoutGroup("Controller")]
    public float verticalVelocity = 0;
    [FoldoutGroup("Controller")]
    public float jumpForce = 10;


    public float pushForce = 4;
    [FoldoutGroup("Controller/Dash")]
    private bool IsDashing;
    [FoldoutGroup("Controller/Dash")]
    public float dashForce;
    [FoldoutGroup("Controller/Dash")]
    public float dashDuration = 0.2f;
    [FoldoutGroup("Controller/Dash")]
    private float dashTimer;

    [SerializeField] private Vector2 moveInput;
    Vector3 normalDebug;
    Vector3 impactPoint;
    Vector3 crossResult;

    [FoldoutGroup("Controller/WalkRun")]
    public float rayLenght;
    [FoldoutGroup("Controller/WalkRun")]
    public bool enableWalkRun;

    

    private void Awake()
    {
        inputs = new();
        controller = GetComponent<CharacterController>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void OnEnable()
    {
        inputs.Enable();

        inputs.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputs.Player.Move.canceled += ctx => moveInput = Vector2.zero;


        inputs.Player.Jump.performed += OnJump;

        inputs.Player.Sprint.performed += OnDash;



    }
    void Start()
    {

    }
    void Update()
    {
        EnableWalRum();
        OnMove();
        //OnSimpleMove();
        
    }

    public void OnMove()
    {
        Vector3 cameraForwardDir = characterCamera.transform.forward;
        cameraForwardDir.y = 0;
        cameraForwardDir.Normalize();


        if (moveInput != Vector2.zero)
        {
            Quaternion targetQuaternion = Quaternion.LookRotation(cameraForwardDir);
            //transform.rotation = targetQuaternion;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetQuaternion,
                rotationSpeed * Time.deltaTime);


        }
        Vector3 moveDir;
        if (!enableWalkRun)
        {
            moveDir = (cameraForwardDir * moveInput.y + transform.right * moveInput.x) * moveSpeed;
        }
        else
        {
            moveDir = (crossResult * moveInput.y) * moveSpeed;

        }

        //Vector3 moveDir = (cameraForwardDir * moveInput.y + transform.right * moveInput.x) * moveSpeed;
        float magnitud = Mathf.Abs(controller.velocity.magnitude);
       
        animator.SetFloat("Speed", magnitud);


        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;


        moveDir.y = verticalVelocity;

        animator.SetBool("Grounded", controller.isGrounded);


        if (IsDashing)
        {
            //->convertir el dash a un barrido por el piso! dash con gravedad integrada omaegoto!
            moveDir = transform.forward * dashForce * (dashTimer / dashDuration);

            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0)
                IsDashing = false;
            
        }
        controller.Move(moveDir * Time.deltaTime);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (!controller.isGrounded) return;

        animator.SetTrigger("Jump");

        verticalVelocity = jumpForce;
    }
    public void OnSimpleMove()
    {
        transform.Rotate(Vector3.up * moveInput.x * rotationSpeed * Time.deltaTime);
        Vector3 moveDir = transform.forward * moveSpeed * moveInput.y;
        controller.SimpleMove(moveDir);
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Vector3 pushDir = (hit.transform.position - transform.position).normalized;

        if (hit.rigidbody != null && hit.rigidbody.linearVelocity == Vector3.zero)
        {
            print(hit.gameObject.name);
            hit.rigidbody.AddForce(pushDir * pushForce, ForceMode.Impulse);
        }
    }
    private void OnDash(InputAction.CallbackContext context)
    {      
        IsDashing = true;
        dashTimer = dashDuration;
        
         StartCoroutine(nameof(DashCooldown));
             
    }
    
    public void EnableWalRum()
    {
        Physics.Raycast(transform.position,transform.right , out  RaycastHit hitRight , rayLenght);

        Physics.Raycast(transform.position, -transform.right, out RaycastHit hitLeft, rayLenght);
        
        if (hitRight.collider !=null && hitRight.collider.gameObject.tag =="Wall")
        {
            enableWalkRun = true;
            Debug.Log("Aleluya R");
            normalDebug = hitRight.normal;
            impactPoint = hitRight.point;
            crossResult = Vector3.Cross(normalDebug,transform.up);

            if(Vector3.Dot(crossResult,transform.forward) < 0)
            {
                crossResult *= -1;
            }
        }
        else
        {
            enableWalkRun=false;
        }

        if(hitLeft.collider != null && hitLeft.collider.gameObject.tag == "Wall")
        {
            Debug.Log("Aleluya L");
            
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.purple;
        Gizmos.DrawRay(transform.position, transform.right * rayLenght);

        Gizmos.color = Color.navyBlue;
        Gizmos.DrawRay(transform.position , -transform.right* rayLenght);

        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(impactPoint, normalDebug * rayLenght);
        Gizmos.DrawSphere(impactPoint, 0.1f);

        Gizmos.color = Color.orange;
        Gizmos.DrawRay(impactPoint, crossResult * rayLenght);
    }

    public float cooldownTimer;
    public float cooldownTime =2;

    public IEnumerator DashCooldown()
    {
            yield return new WaitUntil(() => IsDashing = true);
            yield return new WaitForSeconds(4f);
            yield return new WaitUntil(() => IsDashing = false);                  
            Debug.Log("Cooldown");
        /*while (cooldownTimer >= cooldownTime)
        {
            
            
        }*/
        yield break;
    }

}
