using System.Collections;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

public class Master : MonoBehaviour
{
    [SerializeField] Ground groundPrefab;
    [SerializeField] Heli heliPrefab;
    [SerializeField] Blocker blockerPrefab;
    [SerializeField] GameObject marker;

    public Vector3 groundSpawnLocation = new(0,0,0);
    public Vector3 heliSpawnLocation;
    public Quaternion initialRotation;
    private float[] fovs = {20f,30f,60f,120f,140f};


    public float FoV = 140f;
    public int Change;
    public bool button = false;

    private Color[] colourList = {Color.black,Color.blue,Color.green,Color.yellow,Color.red};




    private Ground ground;
    private Heli heli;
    private Blocker blockerRight;
    private void SpawnGround(){
        if(ground == null){
            ground = Instantiate(groundPrefab,groundSpawnLocation,groundPrefab.transform.rotation);
        }
        
        //ground.GetComponent<Renderer>().material.color = colourList[Random.Range(0,colourList.Length)];
    }
    private void SpawnHeli(){
        if(heli==null){
            heli = Instantiate(heliPrefab,heliSpawnLocation,heliPrefab.transform.rotation);
        }
        
        heli.FoV = FoV;
        initialRotation = heli.transform.rotation;
        heli.initialRotation = initialRotation;
        string[] names = {"BlockerRight","BlockerLeft"};
        foreach (var name in names){
        Transform partToHide = heli.transform.Find(name);
        partToHide.gameObject.SetActive(false);
        }
        heli.spawnLocation = heliSpawnLocation;
        
        

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
    private void FoVCheck(Heli heli){
        var heliPos = heli.transform.position;
        for (int i = 0; i<fovs.Length;i++){
            var fov = fovs[i] - 2;
            var z = 10f;
            var x = z* Mathf.Tan(fov/2 * Mathf.Deg2Rad);
            Vector3 tempPos = heli.transform.position + heli.transform.rotation * new Vector3(x, 0, z);
            Debug.Log($"fov {fov} x val {tempPos.x} {heliPos.z}");
            var tempObject = Instantiate(marker,tempPos,heli.transform.rotation);
            tempObject.GetComponent<Renderer>().material.color = colourList[i];

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        heliSpawnLocation = new Vector3(0,10,-25);
        heli = FindAnyObjectByType<Heli>();
        SpawnHeli();
        ground = FindAnyObjectByType<Ground>();
        SpawnGround();
        FoVCheck(heli);
        
        
        
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
        if(heli.kill){
            heli.GetComponent<Rigidbody>().velocity = new  Vector3(0.0f,0.0f,0.0f);
            
        }


    }
}
