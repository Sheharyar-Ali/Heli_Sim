using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEditor.Media;
using UnityEngine;

public class Heli : MonoBehaviour
{

    public float FoV;


    public float baseFoV = 140f;
    public float vertFov = 100f;
    public float currentFoV;
    // float horVerRatio = 1.839026278f;

    public TextAsset forcingFuncFile;
    public TextAsset trainingFile;

    private float[] forcingFunc;
    private float[] trainingFunc1;
    private float[] trainingFunc2;
    private float dtPython = 0.1f;
    private float T_m = 120.0f;
    private float T_total;
    private KeyCode pitchDown = KeyCode.UpArrow;
    private KeyCode pitchUp = KeyCode.DownArrow;
    public float pushValue;
    public float pitchSpeed = 1f;
    private float smoothTime = 0.1f;
    private float maxPitch = 15f;
    private float maxVal = 1;
    private float angleWanted;
    private float currentPitch;
    private float newPitch;
    private float currentAccel;
    private Vector3 controlVelocity;
    private Vector3 ffVelocity;
    private float Mu = 0.0468f;
    private float Mq = -1.8954f;
    private float M_theta1s = 26.4f;
    private float g = 9.80665f;
    private float X_u = -0.02f;
    private float X_theta1s = -9.280f;

    private List<Data> exportData;
    bool recording = false;
    public bool kill = false;

    private float beginTIme = 0f;
    public Quaternion initialRotation;
    public Vector3 spawnLocation;
    private string indicator;
    public string id;


    public float ConvertToHorFoV(float fov_wanted, Camera cam)
    {
        float ratio = (fov_wanted / 2) / 57.29578f;
        var half_theta = Mathf.Atan(Mathf.Tan(ratio * Mathf.Deg2Rad) / cam.aspect);
        return 2 * half_theta * Mathf.Rad2Deg;
    }

    public void ChangeFoV(float fov)
    {
        var distance = GetComponent<Camera>().nearClipPlane + 0.5f;


        // double length = (Math.Tan(Mathf.Deg2Rad * baseFoV / 2) - Math.Tan(Mathf.Deg2Rad * fov / 2)) * distance;
        Transform blockerRight = transform.Find("BlockerRight");
        double length = blockerRight.transform.localScale.x;
        Debug.Log("Length" + length + " " + distance);
        double pos_x = (Math.Tan(Mathf.Deg2Rad * fov / 2) * distance) + (length / 2);
        Debug.Log("pos_x" + pos_x);

        Vector3 currentSize = blockerRight.transform.localScale;
        Vector3 currentPos = blockerRight.transform.localPosition;
        blockerRight.transform.localScale = new Vector3((float)length, currentSize.y + 4, currentSize.z);
        blockerRight.transform.localPosition = transform.rotation * new Vector3((float)pos_x, currentPos.y, distance);

        Transform blockerLeft = transform.Find("BlockerLeft");
        blockerLeft.transform.localScale = blockerRight.transform.localScale;
        blockerLeft.transform.localPosition = new Vector3((float)-pos_x, currentPos.y, distance);



        if (!blockerRight.gameObject.activeSelf)
        {
            blockerRight.gameObject.SetActive(true);
            blockerLeft.gameObject.SetActive(true);
        }
        currentFoV = fov;


    }

    private void GetData()
    {

        string[] data = forcingFuncFile.text.Split(new string[] { ",", "\n" }, StringSplitOptions.None);
        int tableSize = data.Length / 2 - 1;
        forcingFunc = new float[tableSize];
        for (int i = 0; i < tableSize; i++)
        {
            var value = float.Parse(data[2 * (i + 1) + 1], CultureInfo.InvariantCulture);
            forcingFunc[i] = value;

        }

    }
    private void GetTrainingData()
    {

        string[] data = trainingFile.text.Split(new string[] { ",", "\n" }, StringSplitOptions.None);
        int tableSize = data.Length / 3 - 1;
        trainingFunc1 = new float[tableSize];
        trainingFunc2 = new float[tableSize];
        for (int i = 0; i < tableSize; i++)
        {
            var value = float.Parse(data[3 * (i + 1) + 1], CultureInfo.InvariantCulture);
            trainingFunc1[i] = value;
            var value2 = float.Parse(data[3 * (i + 1) + 2]) * Mathf.Rad2Deg;
            trainingFunc2[i] = value2;

        }

    }
    private float GetPitch()
    {
        if (transform.rotation.eulerAngles.x > 180f)
        {
            Debug.Log($"Pitch: {360 - transform.rotation.eulerAngles.x} Rad {(360 - transform.rotation.eulerAngles.x) * Mathf.Deg2Rad}");
            return (360 - transform.rotation.eulerAngles.x) * Mathf.Deg2Rad;
        }
        else
        {
            Debug.Log($"Pitch: {-transform.rotation.eulerAngles.x} Rad {(-transform.rotation.eulerAngles.x) * Mathf.Deg2Rad}");
            return (-transform.rotation.eulerAngles.x) * Mathf.Deg2Rad;
        }

    }

    IEnumerator ChangeVelocity()
    {
        float elapsedTime = 0f;
        int index = 0;
        while (elapsedTime < T_total)
        {
            float t = elapsedTime % dtPython / dtPython;
            float currentVelocity = Mathf.Lerp(forcingFunc[index], forcingFunc[(index + 1) % forcingFunc.Length], t);

            ffVelocity = new Vector3(0.0f, 0.0f, currentVelocity);
            yield return new WaitForSeconds(dtPython);

            elapsedTime += dtPython;

            index = (index + 1) % forcingFunc.Length;
        }
        recording = false;
        SaveToFile();
        kill = true;
        Start();
    }
        IEnumerator Training()
    {
        float elapsedTime = 0f;
        int index = 0;
        while (elapsedTime < T_total)
        {
            float t = elapsedTime % dtPython / dtPython;
            float currentVelocity = Mathf.Lerp(trainingFunc1[index], trainingFunc1[(index + 1) % trainingFunc1.Length], t);

            ffVelocity = new Vector3(0.0f, 0.0f, currentVelocity);
            yield return new WaitForSeconds(dtPython);

            elapsedTime += dtPython;

            index = (index + 1) % trainingFunc1.Length;
        }
        recording = false;
        SaveToFile();
        kill = true;
        Start();
    }

    private float u_dot(float u, float theta)
    {
        float theta_1s = theta * (-Mq / M_theta1s);
        float u_dot = X_u * u - g * theta + X_theta1s * theta_1s;
        return u_dot;
    }

    private void AddData(float time, float controlvelocity, float ffvelocity, float controlinput)
    {
        exportData.Add(new Data(time: time, controlvelocity: controlvelocity, ffvelocity: ffvelocity, controlinput: controlinput));

    }

    private string ToCsv()
    {
        var sb = new StringBuilder("Time,CV,FF,Input");

        foreach (var entry in exportData)
        {
            sb.Append('\n').Append(entry.Time.ToString(CultureInfo.InvariantCulture)).Append(',').
            Append(entry.controlVelocity.ToString(CultureInfo.InvariantCulture)).Append(',').
            Append(entry.ffVelocity.ToString(CultureInfo.InvariantCulture)).Append(',').
            Append(entry.controlInput.ToString(CultureInfo.InvariantCulture))
            ;
        }
        return sb.ToString();
    }
    public void SaveToFile()
    {
        // Use the CSV generation from before
        var content = ToCsv();


        var filePath = "Assets/Scripts/export_";


        using (var writer = new StreamWriter(filePath + id+ "_"+ indicator + "_" + currentFoV.ToString() + ".csv", false))
        {
            writer.Write(content);
        }

        // Or just
        //File.WriteAllText(content);

        Debug.Log($"CSV file written to \"{filePath + id+ "_"+ indicator + "_" + currentFoV.ToString() + ".csv"}\"");


    }
    // Start is called before the first frame update
    void Start()
    {
        spawnLocation = new(0, 5, -25);
        transform.position = spawnLocation;
        transform.rotation = initialRotation;
        controlVelocity= new  Vector3(0.0f,0.0f,0.0f);
        ffVelocity =  new  Vector3(0.0f,0.0f,0.0f);
        GetData();
        GetTrainingData();
        T_total = T_m + 30;
        currentFoV = baseFoV;
        currentPitch = transform.rotation.x;
        pushValue = Input.GetAxis("Vertical");
        float totalDataPoints = T_total / Time.deltaTime;
        exportData = new List<Data>((int)totalDataPoints);
        ChangeFoV(140);
        
        // exportData = new List<Data>((int) 10000);

    }

    // Update is called once per frame
    void Update()
    {

        pushValue = Input.GetAxis("Vertical");
        var camera = GetComponent<Camera>();
        if (!kill)
        {
            angleWanted = pushValue * maxPitch / maxVal;
            var thetaDot = angleWanted * M_theta1s;
            var finalAngle = thetaDot * Time.deltaTime;
            transform.localEulerAngles = new Vector3(finalAngle, transform.localEulerAngles.y, transform.localEulerAngles.z);
            //transform.Rotate(Vector3.right, smoothedPitchAngle);
            newPitch = GetPitch();
            currentPitch = newPitch;
            Debug.Log($"value {pushValue} angle wanted {angleWanted} final angle {finalAngle} dt {Time.deltaTime}");

        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ChangeFoV(20);

        }
        else if (Input.GetKeyDown(KeyCode.X))
        {

            ChangeFoV(30);
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeFoV(60);
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            ChangeFoV(120);
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            ChangeFoV(140);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            recording = true;
            beginTIme = Time.time;
            indicator = "actual";
            kill = false;
            StartCoroutine(ChangeVelocity());
            //StartCoroutine(ChangePitch());
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            recording = true;
            beginTIme = Time.time;
            indicator = "training";
            kill = false;
            StartCoroutine(Training());
            //StartCoroutine(ChangePitch());
        }
        if (Input.GetKey(pitchDown))
        {
            transform.Rotate(Vector3.right, pitchSpeed * Time.deltaTime);
            newPitch = GetPitch();
            currentPitch = newPitch;

        }
        else if (Input.GetKey(pitchUp))
        {
            transform.Rotate(-Vector3.right, pitchSpeed * Time.deltaTime);
            newPitch = GetPitch();
            currentPitch = newPitch;
        }
        currentAccel = u_dot(u: controlVelocity.z, theta: currentPitch);
        //Debug.Log($"pitch {currentPitch * Mathf.Rad2Deg} u  {GetComponent<Rigidbody>().velocity.z} accel {currentAccel} dt {Time.deltaTime} velocity {controlVelocity.z}");
        controlVelocity += new Vector3(0.0f, 0.0f, currentAccel * Time.deltaTime);
        if (!kill)
        {
            GetComponent<Rigidbody>().velocity = controlVelocity + ffVelocity;
            
        }


        if (recording)
        {
            //Debug.Log($"Time: {Time.time - beginTIme} CV {controlVelocity.z} FF{ffVelocity.z} PV {angleWanted}");
            AddData(Time.time - beginTIme, controlVelocity.z, ffVelocity.z, angleWanted);
        }


    }
    public void OnDestroy()
    {
    }
}
