using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Data
{
    public float Time;
    public float controlVelocity;
    public float ffVelocity;
    public float controlInput;
    public float controlTheta;
    public float ffTheta;
    public float currentPitch;

    public Data(){}

    public Data(float time, float controlvelocity, float ffvelocity, float controlinput, float controltheta, float fftheta, float currentpitch){
        Time = time;
        controlVelocity = controlvelocity;
        ffVelocity = ffvelocity;
        controlInput = controlinput;
        controlTheta = controltheta;
        ffTheta = fftheta;
        currentPitch = currentpitch;

    }
}
