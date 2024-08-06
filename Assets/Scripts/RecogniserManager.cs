using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class RecogniserManager : MonoBehaviour
{
    // Start is called before the first frame update

    void Start()
    {
        QDollar Recog = new QDollar();

        List<List<Vector2>> squareGesture = new List<List<Vector2>>
{
    // First stroke: Draw three sides of the square (starting from top-left, moving clockwise)
    new List<Vector2>
    {
        new Vector2(0, 0),   // Top-left corner
        new Vector2(100, 0), // Top-right corner
        new Vector2(100, 100), // Bottom-right corner
        new Vector2(0, 100)  // Bottom-left corner
    },
    
    // Second stroke: Complete the square by drawing the left side (bottom to top)
          new List<Vector2>
         {
             new Vector2(0, 100), // Start at bottom-left corner
             new Vector2(0, 0)    // End at top-left corner, closing the square
            }
        };


        bool success = Recog.AddGesture("Square", squareGesture);

        if (success)
        {
            print("Square gesture added successfully.");
        }
        else
        {
            print("Failed to add square gesture.");
        }


        List<List<Vector2>> circleGesture = new List<List<Vector2>>
{
    // First stroke: Draw the top half of the circle
    new List<Vector2>
    {
        new Vector2(50, 0),    // Top point
        new Vector2(75, 6.7f),
        new Vector2(93.3f, 25),
        new Vector2(100, 50),  // Right point
        new Vector2(93.3f, 75),
        new Vector2(75, 93.3f),
        new Vector2(50, 100)   // Bottom point
    },
    
    // Second stroke: Complete the circle by drawing the bottom half
    new List<Vector2>
    {
        new Vector2(50, 100),  // Bottom point
        new Vector2(25, 93.3f),
        new Vector2(6.7f, 75),
        new Vector2(0, 50),    // Left point
        new Vector2(6.7f, 25),
        new Vector2(25, 6.7f),
        new Vector2(50, 0)     // Back to top point, closing the circle
    }
};

        // Add the circle gesture to the recognizer
        bool successc = Recog.AddGesture("Circle", circleGesture);

        if (successc)
        {
            print("Circle gesture added successfully.");
        }
        else
        {
            print("Failed to add circle gesture.");
        }


        for (int j  = 0;   j < 12; j++)
        {
            //Recog.PrintStoredGestures();

            List<List<Vector2>> squareGesture2 = new List<List<Vector2>>
{
    // First stroke: Draw three sides of the square (starting from top-left, moving clockwise)
    new List<Vector2>
    {
        new Vector2(0, 0),   // Top-left corner
        new Vector2(100, 0), // Top-right corner
        new Vector2(100, 100), // Bottom-right corner
        new Vector2(0, 100)  // Bottom-left corner
    },
    
    // Second stroke: Complete the square by drawing the left side (bottom to top)
          new List<Vector2>
         {
             new Vector2(0, 100), // Start at bottom-left corner
             new Vector2(0, 0)    // End at top-left corner, closing the square
            }
        };


            for (int i = 0; i < squareGesture2[0].Count; i++)
            {
                squareGesture2[0][i] = new Vector2(squareGesture2[0][i].x + UnityEngine.Random.Range(-5, 50), squareGesture2[0][i].y + UnityEngine.Random.Range(-50, 50));
                //print(squareGesture2[0][i]);
            }

            var test = Recog.Recognize(squareGesture2);
            print($"{test.Name}, with deviation score {test.Score}");
        }
   
    }

    // Update is called once per frame
    void Update()
    {

    }

    //public static 
}
