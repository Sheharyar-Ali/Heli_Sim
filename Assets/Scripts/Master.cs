using System.Collections;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Master : MonoBehaviour
{
    [SerializeField] Ground groundPrefab;
    [SerializeField] Heli heliPrefab;
    [SerializeField] Blocker blockerPrefab;

    public Vector3 groundSpawnLocation = new(0,0,0);
    public Vector3 heliSpawnLocation = new(0,20,-25);

    public float FoV = 140f;
    public int Change;
    public bool button = false;

    private Color[] colourList = {Color.black,Color.blue,Color.green,Color.yellow};




    private Ground ground;
    private Heli heli;
    private Blocker blockerRight;
    private void SpawnGround(){

        ground = Instantiate(groundPrefab,groundSpawnLocation,groundPrefab.transform.rotation);
        //ground.GetComponent<Renderer>().material.color = colourList[Random.Range(0,colourList.Length)];
    }
    private void SpawnHeli(){
        heli = Instantiate(heliPrefab,heliSpawnLocation,heliPrefab.transform.rotation);
        heli.FoV = FoV;
        string[] names = {"BlockerRight","BlockerLeft","BlockerFarRight","BlockerFarLeft"};
        foreach (var name in names){
        Transform partToHide = heli.transform.Find(name);
        partToHide.gameObject.SetActive(false);
        }
        
        

    }
    private void SpawnBlocker(){

        double length = (Math.Tan(Mathf.Deg2Rad* heli.baseFoV/2) - Math.Tan(Mathf.Deg2Rad* Change/2)) * heli.GetComponent<Camera>().nearClipPlane;  
        Debug.Log("Length" + length + " "+ heli.GetComponent<Camera>().nearClipPlane);
        double pos_x = (Math.Tan(Mathf.Deg2Rad* Change/2) * heli.GetComponent<Camera>().nearClipPlane) + (length/2);
        Debug.Log("pos_x" + pos_x);
        if(blockerRight == null){
            blockerRight = Instantiate(blockerPrefab,blockerPrefab.transform.position,blockerPrefab.transform.rotation);
        }
    
        Vector3 currentSize = blockerRight.transform.localScale ;
        blockerRight.transform.localScale = new Vector3((float)length,currentSize.y,currentSize.z);
        blockerRight.transform.localPosition = new Vector3((float)pos_x,heli.transform.position.y,heli.transform.position.z + heli.GetComponent<Camera>().nearClipPlane + 0.5f);
    }
    private void ChangeFoV(){
        heli.ChangeFoV(Change);
        //SpawnBlocker();
        button = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        SpawnGround();
        SpawnHeli();
        
    }
    void OnDestroy(){

        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {
        if (button){
            ChangeFoV();
        }
        if (blockerRight !=null){
            blockerRight.transform.rotation = heli.transform.rotation;
        }
        if(heli.kill){
            heli.GetComponent<Rigidbody>().velocity = new  Vector3(0.0f,0.0f,0.0f);
            
        }


    }
}
