using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public class RecogniserManager : MonoBehaviour
{
    [Header("Drawing State")]
    public bool drawing;
    public List<Vector3> currentStroke;
    private List<GameObject> DrawnLines;
    [Header("References")]
    public Transform cursor;
    public InputActionReference inputAction;
    public GameObject Label;
    public ModelArchive Archive;
    public List<List<Vector3>> currentGesture3D;
    [Header("Gesture Settings & loader")]
    public string newGestureName;
    public List<string> gestureNames;
    private QDollar recogniser;
    //[Header("Buttons")]

    public Vector3 Centroid;
    public String DrawingName;
    

    private void Awake()
    {
        // Initialize lists
        currentGesture3D = new List<List<Vector3>>();
        currentStroke = new List<Vector3>();
        DrawnLines = new List<GameObject>();

        // Initialize the recogniser
        recogniser = new QDollar();
        Label.SetActive(false);

        // Load all gestures
        LoadGestures();

        

        // //Tests
        //
        // // Create a sample List<List<Vector2>>
        // List<List<Vector2>> strokeList = new List<List<Vector2>>();
        // for (int i = 0; i < Random.Range(2, 6); i++)
        // {
        //     List<Vector2> testList = new List<Vector2>();
        //     for (int j = 0; j < 10; j++)
        //     {
        //         testList.Add(new Vector2(Random.Range(0, 300), Random.Range(0, 300)));
        //     }
        //     strokeList.Add(testList);
        // }
        //
        // // Convert List<List<Vector2>> to JSON
        // string json = JsonHelper.ToJson(("Dhruv",strokeList));
        // Debug.Log("Serialized JSON: " + json);
        //
        // // Convert JSON back to List<List<Vector2>>
        // (string, List<List<Vector2>>) deserializedStrokeList = JsonHelper.FromJson(json);
        // Debug.Log("Deserialized List: " + deserializedStrokeList.Item2.Count + " outer lists");
        //
        // foreach (var list in deserializedStrokeList.Item2)
        // {
        //     Debug.Log("Inner List: " + list.Count + " Vector2 elements");
        // }
        //
        // //Tests
    }

    private void LoadGestures()
    {
        foreach (var name in gestureNames)
        {
            string filePath = Path.Combine(Application.dataPath, "Gestures", name + ".json");
            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                var gestureData = JsonHelper.FromJson(jsonContent);
                
                
                recogniser.AddGesture(gestureData.Item1, gestureData.Item2);
                Debug.Log($"Loaded gesture: {gestureData.Item1}");
            }
            else
            {
                Debug.LogWarning($"Gesture file not found: {filePath}");
            }
        }
    }

    private void Update()
    {
        HandleInput();
        MoveCursor();
    }

    private void HandleInput()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // If over a UI element, don't process drawing input
            return;
        }
        
        if (inputAction.action.WasPressedThisFrame() || Input.GetKeyDown(KeyCode.P))
        {
            if (drawing)
            {
                FinishStroke();
                RecognizeGesture();
            }
            else
            {
                StartNewStroke();
                Label.SetActive(false);
            }
            drawing = !drawing;
        }

        if (drawing)
        {
            ContinueStroke();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            RecognizeGesture();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveGesture();
        }
    }

    private void StartNewStroke()
    {
        currentStroke = new List<Vector3> { cursor.position };
    }

    private void ContinueStroke()
    {
        if (Vector3.Distance(currentStroke[currentStroke.Count - 1], cursor.position) > 0.02f)
        {
            currentStroke.Add(cursor.position);
        }
    }

    private void FinishStroke()
    {
        DrawStroke(currentStroke);
        currentGesture3D.Add(new List<Vector3>(currentStroke));
        currentStroke.Clear();
    }

    private void DrawStroke(List<Vector3> stroke)
    {
        if (stroke == null || stroke.Count == 0)
        {
            Debug.LogWarning("Stroke list is null or empty.");
            return;
        }

        GameObject lineObj = new GameObject("Stroke");
        LineRenderer line = lineObj.AddComponent<LineRenderer>();

        // Set line renderer properties
        line.positionCount = stroke.Count;
        line.SetPositions(stroke.ToArray());
        line.startColor = Color.white;
        line.endColor = Color.white;
        line.startWidth = 0.01f;
        line.endWidth = 0.01f;

        // Set line renderer material and other properties
        line.material = new Material(Shader.Find("Unlit/Color")); // Use an unlit shader for simple color
        line.widthMultiplier = 1.0f; // Optional: scale width uniformly

        // Optional: configure alignment and other settings
        line.alignment = LineAlignment.TransformZ; // Aligns line segments based on transform's Z axis
        line.useWorldSpace = false; // Set to true if you want the line to be in world space

        // Optional: add a collider or other components if needed
        // lineObj.AddComponent<Collider>();
        
        DrawnLines.Add(lineObj);
    }


    private void RecognizeGesture()
    {
        var DisplayMSG = "Import: ";
        if (currentGesture3D.Count > 0)
        {
            // Project the entire 3D multi-stroke gesture to 2D
            Vector2[] projectedPoints = Projection(currentGesture3D);

            // Convert the projected points back into a List<List<Vector2>> format
            List<List<Vector2>> gesture2D = new List<List<Vector2>>();
            int currentIndex = 0;
            foreach (var stroke in currentGesture3D)
            {
                List<Vector2> projectedStroke = new List<Vector2>();
                for (int i = 0; i < stroke.Count; i++)
                {
                    projectedStroke.Add(projectedPoints[currentIndex]);
                    currentIndex++;
                }
                gesture2D.Add(projectedStroke);
            }

            // Recognize the projected 2D gesture
            var result = recogniser.Recognize(gesture2D);
            Debug.Log($"Recognized gesture: {result.Name} with score: {result.Score}");
            Label.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TMP_Text>().text = DisplayMSG + result.Name;
            Centroid = CalculateCentroid(currentGesture3D);
            DrawingName = result.Name.ToString();
            Label.transform.position = Centroid;
            Label.transform.LookAt(Camera.main.transform);
            Label.transform.Rotate(0,180,0);
            Label.SetActive(true);
            
            // Clear the current gesture after recognition
            // currentGesture3D.Clear();
        }
        else
        {
            Debug.Log("No gesture to recognize");
        }
    }

    private void MoveCursor()
         {
             float moveSpeed = 0.05f;
             if (Input.GetKey(KeyCode.W)) cursor.Translate(Vector3.up * moveSpeed);
             if (Input.GetKey(KeyCode.S)) cursor.Translate(Vector3.down * moveSpeed);
             if (Input.GetKey(KeyCode.A)) cursor.Translate(Vector3.left * moveSpeed);
             if (Input.GetKey(KeyCode.D)) cursor.Translate(Vector3.right * moveSpeed);
             if (Input.GetKey(KeyCode.UpArrow)) cursor.Translate(Vector3.forward * moveSpeed);
             if (Input.GetKey(KeyCode.DownArrow)) cursor.Translate(Vector3.back * moveSpeed);
         }
    
    public static Vector2[] Projection(List<List<Vector3>> MultipointCloud)
    {
        if (MultipointCloud == null || MultipointCloud.Count == 0)
            return Array.Empty<Vector2>();

        // Get the camera's forward direction and round it to the nearest 15 degrees
        Vector3 cameraForward = RoundToNearest15Degrees(Camera.main.transform.forward);

        // Create a projection plane perpendicular to the camera direction
        Plane projectionPlane = new Plane(-cameraForward, Vector3.zero);

        // Flatten the multipoint cloud into a single list
        List<Vector3> allPoints = MultipointCloud.SelectMany(stroke => stroke).ToList();

        // Project all points onto the plane
        Vector2[] projectedPoints = new Vector2[allPoints.Count];

        for (int i = 0; i < allPoints.Count; i++)
        {
            projectedPoints[i] = ProjectPointOntoPlane(allPoints[i], projectionPlane, cameraForward);
        }

        return projectedPoints;
    }

    private static Vector3 RoundToNearest15Degrees(Vector3 direction)
    {
        float x = Mathf.Round(direction.x / 0.2588f) * 0.2588f; // 0.2588 ≈ sin(15°)
        float y = Mathf.Round(direction.y / 0.2588f) * 0.2588f;
        float z = Mathf.Round(direction.z / 0.2588f) * 0.2588f;
        return new Vector3(x, y, z).normalized;
    }

    private static Vector2 ProjectPointOntoPlane(Vector3 point, Plane plane, Vector3 cameraForward)
    {
        // Project the point onto the plane
        Vector3 projectedPoint = point - plane.GetDistanceToPoint(point) * plane.normal;

        // Create a coordinate system on the plane
        Vector3 right = Vector3.Cross(Vector3.up, cameraForward).normalized;
        Vector3 up = Vector3.Cross(cameraForward, right).normalized;

        // Convert the projected point to 2D coordinates
        float x = Vector3.Dot(projectedPoint, right);
        float y = Vector3.Dot(projectedPoint, up);

        return new Vector2(x, y);
    }
    
    private void SaveGesture()
    {
        if (currentGesture3D.Count > 0 && !string.IsNullOrEmpty(newGestureName))
        {
            // Project the entire 3D multi-stroke gesture to 2D
            Vector2[] projectedPoints = Projection(currentGesture3D);

            // Convert the projected points back into a List<List<Vector2>> format
            List<List<Vector2>> gesture2D = new List<List<Vector2>>();
            int currentIndex = 0;
            foreach (var stroke in currentGesture3D)
            {
                List<Vector2> projectedStroke = new List<Vector2>();
                for (int i = 0; i < stroke.Count; i++)
                {
                    projectedStroke.Add(projectedPoints[currentIndex]);
                    currentIndex++;
                }
                gesture2D.Add(projectedStroke);
            }

            // Create a tuple with the gesture name and the 2D gesture data
            (string, List<List<Vector2>>) gestureData = (newGestureName, gesture2D);

            // Convert the gesture data to JSON
            string jsonData = JsonHelper.ToJson(gestureData);

            // Create the Gestures directory if it doesn't exist
            string gesturesDir = Path.Combine(Application.dataPath, "Gestures");
            if (!Directory.Exists(gesturesDir))
            {
                Directory.CreateDirectory(gesturesDir);
            }

            // Save the JSON data to a file
            string filePath = Path.Combine(gesturesDir, newGestureName + ".json");
            File.WriteAllText(filePath, jsonData);

            // Add the new gesture to the recognizer
            recogniser.AddGesture(newGestureName, gesture2D);

            Debug.Log($"Gesture '{newGestureName}' saved successfully.");

            // Clear the current gesture and reset the name
            currentGesture3D.Clear();
            newGestureName = "";
        }
        else
        {
            Debug.LogWarning("Cannot save gesture: Either no gesture drawn or no name provided.");
        }
    }
    
    public static Vector3[] Vec2ToVec3(Vector2[] Points)
    {

        Vector3[] Ps = new Vector3[Points.Length];

        for (int i = 0; i < Points.Length; i++)
        {
            Ps[i] = new Vector3(Points[i].x, Points[i].y, 0);
        }

        return Ps;
    }
    
    public Vector3 CalculateCentroid(List<List<Vector3>> pointCloud)
    {
        // Validate input
        if (pointCloud == null || pointCloud.Count == 0)
        {
            Debug.LogWarning("Point cloud is null or empty.");
            return Vector3.zero;
        }

        // Initialize variables for summing up the positions
        Vector3 sum = Vector3.zero;
        int totalPoints = 0;

        // Iterate through each list in the point cloud
        foreach (var pointList in pointCloud)
        {
            if (pointList == null || pointList.Count == 0)
            {
                continue; // Skip empty lists
            }

            // Sum up the positions
            foreach (var point in pointList)
            {
                sum += point;
                totalPoints++;
            }
        }

        // Check if there are any points to avoid division by zero
        if (totalPoints == 0)
        {
            Debug.LogWarning("No valid points found in the point cloud.");
            return Vector3.zero;
        }

        // Calculate and return the centroid
        return sum / totalPoints;
    }

    public void DeleteGesture()
    {
        currentGesture3D.Clear();
        currentStroke.Clear();

        foreach (var line in DrawnLines)
        {
            Destroy(line);
        }
        
        DrawnLines.Clear();
        Label.SetActive(false);
    }

    public void ManifestObject()
    {
        print($"Manifesting {DrawingName}");
        Archive.CreateModel(DrawingName,Centroid);
        DeleteGesture();
        Label.SetActive(false);
    }
}