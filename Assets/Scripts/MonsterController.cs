﻿using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour
{
    public float speed = 4f;
    public float gravity = -9.81f;
    public float bounce = 0.04f;
    public float mouseSensitivity = 0.1f;

    public GameObject headBone;
    
    private float maxSpeed;

    private Transform body;
    private Transform camera;
    //private Transform hand;
    private CharacterController controller;
    //private Light flashlight;
    private float velY = 0f;
    private float rotX = 0;
    private float movement = 0f;
    private bool isGrounded = true;
    private GameObject crystal;

    private Animator animator;

    private Text uiText;
    private bool isClicking = false;

    private string player_hash;

    private float[] sendPacket = new float[6];
    

    void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        camera = headBone.transform;
        body = transform.Find("crawler_low");
        //hand = camera.GetChild(0);
        player_hash = generatePlayerHash();
        uiText = GetComponentInChildren<Text>();

        Cursor.lockState = CursorLockMode.Locked;

        animator = GetComponent<Animator>();
        maxSpeed = speed;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(hit.normal.y > 0.5){
            isGrounded = true;
        }else if(hit.normal.y < -0.9 && hit.moveDirection.y > 0 && velY > 0){
            velY = 0;
        }
    }

    private void FixedUpdate()
    {
        Debug.DrawRay(camera.transform.position, camera.transform.forward, Color.white, 5f, false);
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        //if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity))
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, Mathf.Infinity))
        {
            Debug.DrawRay(camera.transform.position, camera.transform.forward, Color.yellow, 5f, false);
            //Debug.Log(hit.collider.gameObject.GetComponent<CrystalController>());
            var obj = hit.collider.gameObject.GetComponent<InteractiveObject>();
//            InteractiveObject obj = hit.transform.GetComponent<InteractiveObject>();
            if (obj != null)
            {
                Debug.Log("hit "+hit.transform.name);
                if (isClicking)
                {
                    uiText.text = "";
                    obj.OnPlayerInteract(gameObject, 0);
                }
                else
                {
                    uiText.text = obj.getHoverMessage();
                }
            }
            else if (uiText != null)
            {
                uiText.text = "";
            }
        }
        else
        {
            if (uiText != null)
            {
                uiText.text = "";
            }
            Debug.DrawRay(camera.transform.position, camera.transform.forward, Color.white, 5f, false);
            //Debug.Log("Did not Hit");
        }
        isClicking = false;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            UnityEditor.EditorApplication.isPlaying = false; 
        }

        if(!Physics.CheckSphere(transform.position, 0.1f)){
            isGrounded = false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isClicking = true;
        }

        if (Input.GetKey(KeyCode.LeftControl)){
        speed = maxSpeed;
        }else{
            speed = maxSpeed/2;
        }

        if(Input.GetKey(KeyCode.LeftShift)){
            speed = maxSpeed;
        }else{
            speed = maxSpeed/2;
        }

        if(isGrounded){
            if(Input.GetKeyDown(KeyCode.Space)){
                velY = 4;
                isGrounded = false;
                animator.SetTrigger("jump");
            }
            else{
                velY = 0;
            }
        }else{
            velY += gravity * Time.deltaTime;
        }

        float posX = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float posZ = Input.GetAxis("Vertical") * speed * Time.deltaTime;

        if (Mathf.Abs(controller.velocity.x) > 0 || Mathf.Abs(controller.velocity.z) > 0)
        {
            if (speed > 2.5) {
                animator.SetInteger("movementState", 2);
            }
            else
            {
                animator.SetInteger("movementState", 1);
            }
        }
        else
        {
            animator.SetInteger("movementState", 0);
        }

        if(isGrounded && (posX !=0 || posZ != 0))
        {
            movement = (movement + 1.5f * Mathf.Max(Mathf.Abs(posX), Mathf.Abs(posZ))) % (Mathf.PI*2f);
        }
        else if(movement < Mathf.PI*0.5f)
        {
            movement = Mathf.Max(movement - 5f*Time.deltaTime, 0f);
        }
        else if(movement >= Mathf.PI*0.5f && movement < Mathf.PI)
        {
            movement = Mathf.Min(movement + 5f*Time.deltaTime, Mathf.PI);
        }
        else if(movement >= Mathf.PI && movement < Mathf.PI*1.5f)
        {
            movement = Mathf.Max(movement - 5f*Time.deltaTime, Mathf.PI);
        }
        else if(movement >= Mathf.PI*1.5f)
        {
            movement = Mathf.Min(movement + 5f*Time.deltaTime, Mathf.PI*2f);
        }

        float vertBob = Mathf.Abs(Mathf.Sin(movement + Mathf.PI*0.5f));
        float horiBob = Mathf.Sin(movement);

        float posY = velY * Time.deltaTime;

        body.localPosition = new Vector3(horiBob*bounce*0.5f, 0.9f+vertBob*bounce, 0f);
        //body.transform.localRotation = Quaternion.Euler(0f, 0f, horiBob*-0.0244f);
        //camera.localPosition = new Vector3(horiBob*bounce*0.5f, 1.68f+vertBob*bounce, 0f);

        controller.Move(transform.forward*posX - transform.right*posZ + Vector3.up*posY);

        sendPacket[0] = transform.position.x;
        sendPacket[1] = transform.position.y;
        sendPacket[2] = transform.position.z;

        /////////////////////////////////

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = -1f * Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        //Debug.Log("MouseX: " + Input.GetAxis("Mouse X") + ", sens: " + mouseSensitivity + ", time: " + Time.deltaTime + ", total: " + mouseX);
        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, -85f, 85f);

        camera.localRotation = Quaternion.Euler(rotX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        sendPacket[3] = camera.localRotation.x;
        sendPacket[4] = transform.rotation.y;

        /////////////////////////////////
    


        ////////////////////////////////

        if(Input.GetKeyDown(KeyCode.E)){
            animator.SetTrigger("attack");
        }

        float noise = Mathf.PerlinNoise(0, 10f*Time.time);

    }

    private void OnMouseDown()
    {
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            //Debug.Log("Did Hit");
            if (hit.transform.tag == "Interactive")
            {
                InteractiveObject obj = hit.transform.GetComponent<InteractiveObject>();
                obj.OnPlayerInteract(gameObject, 0);
            }
        }
    }

    string generatePlayerHash() {
        byte[] byte_hash;
        using (HashAlgorithm algorithm = SHA256.Create()) {
            byte_hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(System.DateTime.Now.ToString()+System.Environment.MachineName));
        }
        
        StringBuilder sb = new StringBuilder();
        foreach (byte b in byte_hash) {
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Crystal") && crystal == null)
        {
            if (!other.gameObject.GetComponent<CrystalController>().isDeposited)
            {
                crystal = other.gameObject;
                other.gameObject.GetComponent<CrystalController>()
                    .SetTransformParent(gameObject.transform);
                gameObject.transform.position = new Vector3(10, 10, 10);
            }
        }
        else if (other.gameObject.CompareTag("Receptacle") && crystal != null)
        {
            crystal.GetComponent<CrystalController>()
                .SetTransformParent(other.gameObject.transform);
            crystal.GetComponent<CrystalController>().isDeposited = true;
            crystal = null;
        }
    }

    public string getPositionDict() {
        Vector3 player_xyz_pos = gameObject.transform.position;
        Vector3 player_xyz_rot = gameObject.transform.eulerAngles;
        float head_x_rot = gameObject.transform.GetChild(0).eulerAngles.x;
        return $"{{'body_posX:' '{player_xyz_pos.x}', 'body_posY:' '{player_xyz_pos.y}', 'body_posZ:' '{player_xyz_pos.z}', 'head_rotX:' '{head_x_rot}', 'body_rotY:' '{player_xyz_rot.y}', 'body_rotZ:' '{player_xyz_rot.z}'}}";
    }

    public void hideInCloset(GameObject closet)
    {
        transform.position = closet.transform.position;
    }
}
