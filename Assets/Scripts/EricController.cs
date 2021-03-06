﻿using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using System.Net.Sockets;
using System;
using System.Net;
using System.Threading;
using System.ServiceModel;

public class EricController : MonoBehaviour
{
    public GameObject playerPrefabNoCodeReal;

    public float speed = 8f;
    public float gravity = -9.81f;
    public float bounce = 0.04f;
    public float mouseSensitivity = 400f;
    
    private Transform body;
    private Transform camera;
    private Transform hand;
    private CharacterController controller;
    private Light flashlight;
    private float rotX = 0;
    private float movement = 0f;
    private float horo = 0f;
    private float vert = 0f;
    private GameObject crystal;

    private DateTime next_update = DateTime.Now;

    private Text uiText;
    private bool isClicking = false;

    public string player_hash;

    public object __lockObj = new object();
    public List<String> to_add = new List<String>();

    public Dictionary<String, GameObject> player_holder = new Dictionary<String, GameObject>();
    
    void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        camera = transform.GetChild(0);
        body = transform.GetChild(1);
        hand = camera.GetChild(0);
        flashlight = hand.GetChild(0).GetComponent<Light>();
        player_hash = generatePlayerHash();
        uiText = GetComponentInChildren<Text>();

        Cursor.lockState = CursorLockMode.Locked;

        // client_connection = new ClientConnection(this);
        // client_connection = gameObject.AddComponent<ClientConnection>(this) as ClientConnection;
    }


    //////////////////////////////////////////////////////////////////////////////////////////////


    public GameObject GetRemotePlayer(String username) {
        if (!player_holder.ContainsKey(username)) {
            Debug.Log("instantiating player named: " + username);
            GameObject new_guy = null;
            try {
                new_guy = Instantiate(playerPrefabNoCodeReal, new Vector3(0, 0, 0), Quaternion.identity);
            }
            catch (Exception e) {
                Debug.Log(e);
                Application.Quit();
            }
            Debug.Log("instantiated player: " + username);
            player_holder[username] = new_guy;
            Debug.Log(player_holder[username]);
            return new_guy;
        }
        else {
            // Debug.Log("found player");
            return player_holder[username];
        }
    }

    public void HandlePlayerInteract(String username, String Object, byte action){
        GameObject player = GameObject.Find(username);
        if (player == null){
            Instantiate(playerPrefabNoCodeReal, new Vector3(0, 0, 0), Quaternion.identity);
        }
        GameObject.Find(Object).GetComponent<InteractiveObject>().OnPlayerInteract(player, action);
    }
    //////////////////

    private Tuple<String, String> GetKeyVal(String something) {
        List<String> stuff3 = new List<String>(something.Split(':'));
        String val;
        String key;
        key = stuff3[0].Trim();
        val = stuff3[1].Trim();
        return new Tuple<String, String>(key, val);
    }

    private void process_thing(String msg) {
        try {
            GameObject remotePlayer = null;
            List<String> stuff2 = new List<String>(msg.Split(','));

            Dictionary<String, String> all_dict = new Dictionary<String, String>();
            foreach (var something in stuff2) {
                Tuple<String, String> lmaoo = GetKeyVal(something);
                String key = lmaoo.Item1;
                String val = lmaoo.Item2;

                // if (key == "player_hash") {
                    // remove later
                    // val = val + "o";
                // }
                all_dict[key] = val;
            }

            if (all_dict["player_hash"] == player_hash) {
                return;
            }
            // Debug.Log("getting palyer");
            remotePlayer = GetRemotePlayer(all_dict["player_hash"]);

            if (remotePlayer != null) {
                remotePlayer.transform.position = new Vector3(float.Parse(all_dict["body_posX"]), float.Parse(all_dict["body_posY"]), float.Parse(all_dict["body_posZ"]));
                // else if (key == "body_rotY") {
                // else if (key == "body_rotZ") {
                // else if (key == "head_rotX") {
            }
            else {
                // Debug.Log("couldnt find player");
            }
        }
        catch (Exception e) {
            Debug.Log(e);
            Application.Quit();
        }
    }

    void OnCollisionStay(Collision collisionInfo)
    {
        for (int i=0; i<collisionInfo.contactCount; i++){
            ContactPoint contact = collisionInfo.GetContact(i);
            Debug.DrawRay(contact.point, contact.normal, Color.red);
        }
    }

    private void FixedUpdate()
    {
        float moveX = horo * speed * Time.deltaTime;
        float moveY = 0;
        float moveZ = vert * speed * Time.deltaTime;

        //float posY = velY * Time.deltaTime;
        
        GetComponent<Rigidbody>().MovePosition(transform.position + moveZ*transform.forward + moveX*transform.right + moveY*transform.up);

        Debug.DrawRay(camera.transform.position, camera.transform.forward, Color.white, 5f, false);
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        //if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity))
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, Mathf.Infinity))
        {
            Debug.DrawRay(camera.transform.position, camera.transform.forward, Color.yellow, 5f, false);
            var obj = hit.collider.gameObject.GetComponent<InteractiveObject>();
            if (obj != null)
            {
                //Debug.Log("hit "+hit.transform.name);
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

        horo = Input.GetAxis("Horizontal");
        vert = Input.GetAxis("Vertical");

        /////////////////////////////////

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        //Debug.Log("MouseX: " + Input.GetAxis("Mouse X") + ", sens: " + mouseSensitivity + ", time: " + Time.deltaTime + ", total: " + mouseX);
        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, -85f, 85f);

        camera.localRotation = Quaternion.Euler(rotX, 0f, 0f);
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
        }

        float noise = Mathf.PerlinNoise(0, 10f*Time.time);

        //flashlight.intensity = Mathf.Min(40f + noise*160f, 80f);
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
            other.gameObject.GetComponent<ReceptacleScript>().AddCrystal(crystal);
            crystal = null;
        }
    }

    public string getPositionDict() {
        Vector3 player_xyz_pos = gameObject.transform.position;
        Vector3 player_xyz_rot = gameObject.transform.eulerAngles;
        float head_x_rot = gameObject.transform.GetChild(0).eulerAngles.x;
        return $"player_hash: {player_hash}, body_posX: {player_xyz_pos.x}, body_posY: {player_xyz_pos.y}, " + 
                $"body_posZ: {player_xyz_pos.z}, head_rotX: {head_x_rot}, body_rotY: {player_xyz_rot.y}, body_rotZ: {player_xyz_rot.z}";
    }

    public void hideInCloset(GameObject closet)
    {
        transform.position = closet.transform.position;
    }
}
