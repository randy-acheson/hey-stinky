﻿using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using System;

public class PlayerController : MonoBehaviour, CreatureBase
{
    public GameObject playerPrefabRemote;
    public AudioSource audioData;

    public float speed = 8f;
    public float gravity = -9.81f;
    public float bounce = 0.04f;
    public float mouseSensitivity = 400f;
    public float flashlightIntensity = 3.5f;

    private bool isDead;
    
    private Transform body;
    private Transform head;
    private Transform hand;
    private CharacterController controller;
    private Light flashlight;
    private float velY = 0f;
    private float rotX = 0;
    private float movement = 0f;
    private GameObject crystal;

    private DateTime next_update = DateTime.Now;

    private Text uiText;
    private bool isClicking = false;

    public String player_hash;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        head = transform.GetChild(0);
        body = transform.GetChild(1);
        hand = head.GetChild(0);
        flashlight = hand.GetChild(0).GetComponent<Light>();
        player_hash = generatePlayerHash();
        uiText = GetComponentInChildren<Text>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        RaycastHit hit;
        string newUIText = "";
        // Does the ray intersect any objects excluding the player layer
        if (!isDead && Physics.Raycast(head.transform.position, head.transform.forward, out hit, Mathf.Infinity))
        {
            var obj = hit.collider.gameObject.GetComponent<InteractiveObject>();
            if (obj != null)
            {
                //Debug.Log("hit "+hit.transform.name);
                if (isClicking)
                {
                    obj.OnPlayerInteract(gameObject, 0);
                }
                else
                {
                    newUIText = obj.getHoverMessage();
                }
            }
        }
        else
        {
            //Debug.Log("Did not Hit");
        }
        isClicking = false;
        if (uiText != null)
        {
            uiText.text = newUIText;
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            UnityEditor.EditorApplication.isPlaying = false; 
        }

        if (Input.GetMouseButtonDown(0))
        {
            isClicking = true;
        }

        if (Input.GetKey(KeyCode.LeftControl)){
        speed = 8;
        }else{
            speed = 4;
        }

        if(Input.GetKey(KeyCode.LeftShift)){
            speed = 8;
        }else{
            speed = 4;
        }

        float posY = -0.02f;

        if((controller.collisionFlags & CollisionFlags.Below) != 0){
            if(Input.GetKeyDown(KeyCode.Space)){
                velY = 4;
                posY = velY * Time.deltaTime;
                //isGrounded = false;
            }else{
                velY = 0;
            }
        }else if ((controller.collisionFlags & CollisionFlags.Above) != 0){
            velY = 0;
        }else{
            velY += gravity * Time.deltaTime;
            posY = velY * Time.deltaTime;
        }
        
        //Debug.Log("grounded: " + controller.isGrounded + ", vel: " + velY);

        float posX = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float posZ = Input.GetAxis("Vertical") * speed * Time.deltaTime;

        if(controller.isGrounded && (posX != 0 || posZ != 0))
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

        float vertBob = Mathf.Abs(Mathf.Sin(movement));
        float horiBob = Mathf.Sin(movement);

        body.localPosition = new Vector3(horiBob*bounce*0.5f, 0.9f+vertBob*bounce, 0f);
        //body.transform.localRotation = Quaternion.Euler(0f, 0f, horiBob*-0.0244f);
        head.localPosition = new Vector3(horiBob*bounce*0.5f, 1.68f+vertBob*bounce, 0f);

        controller.Move(transform.right*posX + transform.forward*posZ + transform.up*posY);

        /////////////////////////////////

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        //Debug.Log("MouseX: " + Input.GetAxis("Mouse X") + ", sens: " + mouseSensitivity + ", time: " + Time.deltaTime + ", total: " + mouseX);
        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, -85f, 85f);

        head.localRotation = Quaternion.Euler(rotX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        /////////////////////////////////
    
        hand.localRotation = Quaternion.Euler(
            90f + 1.5f*(Mathf.PerlinNoise(0, 1f*Time.time)-0.5f), 
            1.5f*(Mathf.PerlinNoise(1f*Time.time, 0)-0.5f), 
            0f);

        ////////////////////////////////

        if(Input.GetKeyDown(KeyCode.E)){
            flashlight.enabled = !flashlight.enabled;
            hand.gameObject.SetActive(!hand.gameObject.activeSelf);
            if (!isDead)
            {
                Dictionary<string, string> tcpFlashlightCommand = new Dictionary<string, string>();
                tcpFlashlightCommand["function"] = "toggleFlashlight";
                tcpFlashlightCommand["playerHash"] = player_hash;
                tcpFlashlightCommand["isLightOn"] = hand.gameObject.activeSelf.ToString();
                AsyncTCPClient.Send(ClientConnection.dictmuncher(tcpFlashlightCommand));
            }
        }

        float noise = Mathf.PerlinNoise(0, 10f*Time.time);

        flashlight.intensity = Mathf.Min(0.5f*flashlightIntensity + (noise*4f*flashlightIntensity), flashlightIntensity);
    
        /////////////////////
    }

    private void OnMouseDown()
    {
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (!isDead && Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity))
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
        if (!isDead)
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
                other.gameObject.GetComponent<ReceptacleScript>().AddCrystal(crystal);
                crystal = null;
            }
            else if (other.gameObject.CompareTag("Goal"))
            {
                Debug.Log("You win!");
                audioData.Play(0);
            }
            else if (other.gameObject.CompareTag("CharacterSelect"))
            {
                GameObject.Find("CharacterSelectors")
                    .GetComponent<CharacterSelectionController>()
                    .SelectCharacter(other.gameObject, player_hash);
            }
        }
    }

    public GameObject getGameObject() {
        return gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("CharacterSelect"))
        {
            GameObject.Find("CharacterSelectors")
                .GetComponent<CharacterSelectionController>()
                .DeselectCharacter(other.gameObject, player_hash);
        }
    }

    public Dictionary<String, String> getPositionDict() {
        Vector3 player_xyz_pos = transform.position;
        Vector3 player_xyz_rot = transform.eulerAngles;
        float head_x_rot = head.eulerAngles.x;

        Dictionary<String, String> dict = new Dictionary<String, String> {
            {"player_hash", player_hash},
            {"body_posX", player_xyz_pos.x.ToString()},
            {"body_posY", player_xyz_pos.y.ToString()},
            {"body_posZ", player_xyz_pos.z.ToString()},
            {"head_rotX", head_x_rot.ToString()},
            {"body_rotY", player_xyz_rot.y.ToString()},
            {"body_rotZ", player_xyz_rot.z.ToString()},
            {"prefab_name", "playerPrefab"},
        };

        return dict;
    }

    public String get_player_hash() {
        return player_hash;
    }

    public void hideInCloset(GameObject closet) {
        transform.position = closet.transform.position;
    }

    public void Die()
    {
        Debug.Log("You died");
        if (uiText != null)
        {
            uiText.text = "You died";
        }
        isDead = true;
        body.GetComponent<MeshRenderer>().enabled = false; 
    }
}
