using Netcode;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float maxSpeed;
    private Vector3 movementDirection;
    private Rigidbody rb;
    //private NetworkTransform nt;
   
    //Forced to be start because of NetTrans
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
       // nt = GetComponent<NetworkTransform>();
        rb.maxLinearVelocity = maxSpeed;
        MyInput.InitializeBase();
        //if (!nt.IsOwner) return;
        MyInput.Init(this);
        MyInput.GameMode();
    }

    private void FixedUpdate()
    {
        //if (!nt.IsOwner) return; // We don't want to move unless we own this object..
        rb.AddForce(movementDirection * speed);
    }

    public void ReceiveMovement(Vector3 vec)
    {
        movementDirection = vec;
    }
}
