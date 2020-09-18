using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PathCreation;

public class RoadGeneratorTester : MonoBehaviour
{
    public PathCreator m_PathCreator;

    // Start is called before the first frame update
    void Start()
    {
        List<Vector3> points = new List<Vector3>() {
            new Vector3(0,0,0),
            new Vector3(10,0,0),
            new Vector3(10,0,10),
            new Vector3(0,0,10),
        };
        BezierPath path = new BezierPath(points, true, PathSpace.xyz);
        m_PathCreator.bezierPath = path;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
