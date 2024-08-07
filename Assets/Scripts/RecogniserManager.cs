using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class RecogniserManager : MonoBehaviour
{
    // Start is called before the first frame update

    //    void Start()
    //    {
    //        QDollar Recog = new QDollar();

    //        List<List<Vector2>> squareGesture = new List<List<Vector2>>
    //{
    //    // First stroke: Draw three sides of the square (starting from top-left, moving clockwise)
    //    new List<Vector2>
    //    {
    //        new Vector2(0, 0),   // Top-left corner
    //        new Vector2(100, 0), // Top-right corner
    //        new Vector2(100, 100), // Bottom-right corner
    //        new Vector2(0, 100)  // Bottom-left corner
    //    },

    //    // Second stroke: Complete the square by drawing the left side (bottom to top)
    //          new List<Vector2>
    //         {
    //             new Vector2(0, 100), // Start at bottom-left corner
    //             new Vector2(0, 0)    // End at top-left corner, closing the square
    //            }
    //        };


    //        bool success = Recog.AddGesture("Square", squareGesture);

    //        if (success)
    //        {
    //            print("Square gesture added successfully.");
    //        }
    //        else
    //        {
    //            print("Failed to add square gesture.");
    //        }


    //        List<List<Vector2>> circleGesture = new List<List<Vector2>>
    //{
    //    // First stroke: Draw the top half of the circle
    //    new List<Vector2>
    //    {
    //        new Vector2(50, 0),    // Top point
    //        new Vector2(75, 6.7f),
    //        new Vector2(93.3f, 25),
    //        new Vector2(100, 50),  // Right point
    //        new Vector2(93.3f, 75),
    //        new Vector2(75, 93.3f),
    //        new Vector2(50, 100)   // Bottom point
    //    },

    //    // Second stroke: Complete the circle by drawing the bottom half
    //    new List<Vector2>
    //    {
    //        new Vector2(50, 100),  // Bottom point
    //        new Vector2(25, 93.3f),
    //        new Vector2(6.7f, 75),
    //        new Vector2(0, 50),    // Left point
    //        new Vector2(6.7f, 25),
    //        new Vector2(25, 6.7f),
    //        new Vector2(50, 0)     // Back to top point, closing the circle
    //    }
    //};

    //        // Add the circle gesture to the recognizer
    //        bool successc = Recog.AddGesture("Circle", circleGesture);

    //        if (successc)
    //        {
    //            print("Circle gesture added successfully.");
    //        }
    //        else
    //        {
    //            print("Failed to add circle gesture.");
    //        }


    //        for (int j  = 0;   j < 12; j++)
    //        {
    //            //Recog.PrintStoredGestures();

    //            List<List<Vector2>> squareGesture2 = new List<List<Vector2>>
    //{
    //    // First stroke: Draw three sides of the square (starting from top-left, moving clockwise)
    //    new List<Vector2>
    //    {
    //        new Vector2(0, 0),   // Top-left corner
    //        new Vector2(100, 0), // Top-right corner
    //        new Vector2(100, 100), // Bottom-right corner
    //        new Vector2(0, 100)  // Bottom-left corner
    //    },

    //    // Second stroke: Complete the square by drawing the left side (bottom to top)
    //          new List<Vector2>
    //         {
    //             new Vector2(0, 100), // Start at bottom-left corner
    //             new Vector2(0, 0)    // End at top-left corner, closing the square
    //            }
    //        };


    //            for (int i = 0; i < squareGesture2[0].Count; i++)
    //            {
    //                squareGesture2[0][i] = new Vector2(squareGesture2[0][i].x + UnityEngine.Random.Range(-5, 50), squareGesture2[0][i].y + UnityEngine.Random.Range(-50, 50));
    //                //print(squareGesture2[0][i]);
    //            }

    //            var test = Recog.Recognize(squareGesture2);
    //            print($"{test.Name}, with deviation score {test.Score}");
    //        }

    //}

    public bool drawing;
    public List<Vector3> Points;
    public Transform Cursor;
    public InputActionReference InputAction;
    public List<List<Vector2>> Gesture;
    public string GestureName;
    public List<string> Gestures;
    private QDollar Recogniser;
    int PINDEX = 0;
    public void Awake()
    {
        Gesture = new List<List<Vector2>>();   
        
        Recogniser = new QDollar();

        foreach (var name in Gestures) { 
            var G = Newtonsoft.Json.JsonConvert.DeserializeObject<(string, List<List<Vector2>>)>(File.ReadAllText(Path.Combine(Application.dataPath, "Gestures", name + ".json")));
            Recogniser.AddGesture(G.Item1, G.Item2);
            print($"Added {G.Item1}");
        }
        
        var Gest = Newtonsoft.Json.JsonConvert.DeserializeObject<(string, List<List<Vector2>>)>(File.ReadAllText(Path.Combine(Application.dataPath, "Gestures", "Tree.json")));
        
        foreach (var stroke in Gest.Item2)
        {

            LineRenderer line = new GameObject("Shape", new[] { typeof(LineRenderer) }).GetComponent<LineRenderer>();

            var PTS = Vec2ToVec3(stroke.ToArray());
            line.positionCount = stroke.Count;
            line.SetPositions(PTS.ToArray());
            line.endColor = line.startColor = Color.white;
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;

        }


    }

    private void Update()
    {
        if (InputAction.action.WasPressedThisFrame() || Input.GetKeyDown(KeyCode.P))
        {

            if (drawing) {
               LineRenderer line =  new GameObject("Shape", new[] { typeof(LineRenderer) }).GetComponent<LineRenderer>();
               
                line.positionCount = Points.Count;
                line.SetPositions(Points.ToArray());
                line.endColor = line.startColor = Color.white;
                line.startWidth = 0.05f;
                line.endWidth = 0.05f;
               




                var newP = Vector3Projection(Points);
                var obj = new GameObject("Shape2", new[] { typeof(LineRenderer) });
                LineRenderer line2 = obj.GetComponent<LineRenderer>();

                line2.positionCount = newP.Length;
                line2.SetPositions(Vec2ToVec3(newP));
                line2.endColor = line2.startColor = Color.white;
                line2.startWidth = line2.endWidth = 0.1f;

                Gesture.Add(newP.ToList());

            }
            else
            {
                Points  = new List<Vector3>();
                Points.Add(Cursor.position);
                PINDEX = 0;
            }
            drawing = !drawing;
        }

        if (Input.GetKey(KeyCode.W))
        {
            Cursor.Translate(new Vector3(0, 0.05f, 0));
        }
        if (Input.GetKey(KeyCode.S))
        {
            Cursor.Translate(new Vector3(0, -0.05f, 0));
        }
        if (Input.GetKey(KeyCode.A))
        {
            Cursor.Translate(new Vector3(-0.05f, 0, 0));
        }
        if (Input.GetKey(KeyCode.D))
        {
            Cursor.Translate(new Vector3(0.05f, 0, 0));
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            Cursor.Translate(new Vector3(0, 0, 0.05f));
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            Cursor.Translate(new Vector3(0, 0, -0.05f));
        }
        //Cursor.Translate(0, 0, UnityEngine.Random.Range(-0.5f, 0.7f));

        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    Directory.CreateDirectory(Path.Combine(Application.dataPath, "Gestures"));

        //    var path = Path.Combine(Application.dataPath, "Gestures", GestureName + ".json");
        //    File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject((GestureName, Gesture)));
        //}

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var R = Recogniser.Recognize(Gesture);

            print(R.Name);
        }

        if (drawing)
        {

            if (Vector3.Distance(Points.Last(), Cursor.position) > 0.02f) {
                print($"Added Point {PINDEX}");
                PINDEX++;
                Points.Add(Cursor.position);
            }
           

            //Points.Add(new Vector3(UnityEngine.Random.Range(-10,10), UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(-10, 10)));

            // Points.Add(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            //print(Points.Last());
            //print(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            //Cursor.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

         }
    //public static List<Vector2> Convert3DTo2D(List<Vector3> points3D)
    //{
    //    // Convert to array for easier manipulation
    //    Vector3[] pointsArray = points3D.ToArray();

    //    // Center the points
    //    Vector3 centroid = GetCentroid(pointsArray);
    //    for (int i = 0; i < pointsArray.Length; i++)
    //    {
    //        pointsArray[i] -= centroid;
    //    }

    //    // Perform PCA
    //    Matrix4x4 covarianceMatrix = GetCovarianceMatrix(pointsArray);
    //    Vector3 principalAxis = GetPrincipalAxis(covarianceMatrix);

    //    // Project points onto the plane perpendicular to the principal axis
    //    Vector3 xAxis = Vector3.Cross(principalAxis, Vector3.up).normalized;
    //    Vector3 yAxis = Vector3.Cross(principalAxis, xAxis).normalized;

    //    List<Vector2> points2D = new List<Vector2>();
    //    foreach (Vector3 point in pointsArray)
    //    {
    //        float x = Vector3.Dot(point, xAxis);
    //        float y = Vector3.Dot(point, yAxis);
    //        points2D.Add(new Vector2(x, y));
    //    }

    //    // Normalize
    //    Vector2 centroid2D = GetCentroid2D(points2D);
    //    float scale = points2D.Max(p => Mathf.Max(Mathf.Abs(p.x - centroid2D.x), Mathf.Abs(p.y - centroid2D.y)));

    //    for (int i = 0; i < points2D.Count; i++)
    //    {
    //        points2D[i] = (points2D[i] - centroid2D) / scale;
    //    }

    //    // Rotate to align with x-axis
    //    float angle = Mathf.Atan2(points2D[0].y, points2D[0].x);
    //    for (int i = 0; i < points2D.Count; i++)
    //    {
    //        points2D[i] = RotatePoint(points2D[i], -angle);
    //    }

    //    // Ensure consistent direction
    //    if (points2D.Count(p => p.x > 0) < points2D.Count / 2)
    //    {
    //        for (int i = 0; i < points2D.Count; i++)
    //        {
    //            points2D[i] = new Vector2(-points2D[i].x, points2D[i].y);
    //        }
    //    }

    //    return points2D;
    //}

    //private static Vector3 GetCentroid(Vector3[] points)
    //{
    //    Vector3 sum = Vector3.zero;
    //    foreach (Vector3 point in points)
    //    {
    //        sum += point;
    //    }
    //    return sum / points.Length;
    //}

    //private static Vector2 GetCentroid2D(List<Vector2> points)
    //{
    //    Vector2 sum = Vector2.zero;
    //    foreach (Vector2 point in points)
    //    {
    //        sum += point;
    //    }
    //    return sum / points.Count;
    //}

    //private static Matrix4x4 GetCovarianceMatrix(Vector3[] points)
    //{
    //    Matrix4x4 covarianceMatrix = Matrix4x4.zero;
    //    int n = points.Length;

    //    for (int i = 0; i < n; i++)
    //    {
    //        covarianceMatrix[0, 0] += points[i].x * points[i].x;
    //        covarianceMatrix[0, 1] += points[i].x * points[i].y;
    //        covarianceMatrix[0, 2] += points[i].x * points[i].z;
    //        covarianceMatrix[1, 1] += points[i].y * points[i].y;
    //        covarianceMatrix[1, 2] += points[i].y * points[i].z;
    //        covarianceMatrix[2, 2] += points[i].z * points[i].z;
    //    }

    //    covarianceMatrix[1, 0] = covarianceMatrix[0, 1];
    //    covarianceMatrix[2, 0] = covarianceMatrix[0, 2];
    //    covarianceMatrix[2, 1] = covarianceMatrix[1, 2];

    //    for (int i = 0; i < 3; i++)
    //    {
    //        for (int j = 0; j < 3; j++)
    //        {
    //            covarianceMatrix[i, j] /= n;
    //        }
    //    }

    //    return covarianceMatrix;
    //}

    //private static Vector3 GetPrincipalAxis(Matrix4x4 covarianceMatrix)
    //{
    //    // This is a simplification. For more accurate results, you should use a proper eigenvalue decomposition.
    //    // Unity doesn't provide this out of the box, so you might need to implement it or use a third-party math library.
    //    Vector3 eigenVector = new Vector3(
    //        covarianceMatrix[0, 0],
    //        covarianceMatrix[1, 0],
    //        covarianceMatrix[2, 0]
    //    ).normalized;

    //    return eigenVector;
    //}

    //private static Vector2 RotatePoint(Vector2 point, float angle)
    //{
    //    float cos = Mathf.Cos(angle);
    //    float sin = Mathf.Sin(angle);
    //    return new Vector2(
    //        point.x * cos - point.y * sin,
    //        point.x * sin + point.y * cos
    //    );
    //}


    public static Vector3[] Vec2ToVec3(Vector2[] Points)
    {

        Vector3[] Ps = new Vector3[Points.Length];

        for (int i = 0; i < Points.Length; i++)
        {
            Ps[i] = new Vector3(Points[i].x, Points[i].y, 0);
        }

        return Ps;
    }
    public static Vector2[] Vector3Projection(List<Vector3> pointCloud)
    {
        if (pointCloud == null || pointCloud.Count == 0)
        {
            return new Vector2[0];
        }

        // Calculate the centroid of the point cloud
        Vector3 centroid = pointCloud.Aggregate(Vector3.zero, (acc, p) => acc + p) / pointCloud.Count;

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector2[] projectedPoints = new Vector2[pointCloud.Count];

        // Create a projection plane passing through the centroid and perpendicular to the camera's forward direction
        Plane projectionPlane = new Plane(cameraForward, centroid);

        for (int i = 0; i < pointCloud.Count; i++)
        {
            Vector3 pointToProject = pointCloud[i];

            // Project the point onto the plane
            Ray ray = new Ray(Camera.main.transform.position, pointToProject - Camera.main.transform.position);
            float enter;
            projectionPlane.Raycast(ray, out enter);
            Vector3 projectedPoint = ray.GetPoint(enter);

            // Convert the projected point to the camera's local space
            Vector3 localProjectedPoint = Camera.main.transform.InverseTransformPoint(projectedPoint);

            // Store only the X and Y components as Vector2
            projectedPoints[i] = new Vector2(localProjectedPoint.x, localProjectedPoint.y);
        }

        return projectedPoints;
    }
}
