using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QDollar
{
    private const int NumPointClouds = 16;
    private const int NumPoints = 32;
    private static readonly float2 Origin = float2.zero;
    private const int MaxIntCoord = 1024;
    private const int LUTSize = 128;
    private const float LUTScaleFactor = MaxIntCoord / (float)LUTSize;

    private NativeArray<PointCloud> pointClouds;

    [System.Serializable]
    public struct Point
    {
        public float2 position;
        public int ID;
        public int2 intPosition;

        public Point(float x, float y, int id)
        {
            position = new float2(x, y);
            ID = id;
            intPosition = int2.zero;
        }
    }

    public struct PointCloud
    {
        public FixedString64Bytes Name;
        public NativeArray<Point> Points;
        public NativeArray<int> LUT;
        public bool IsInitialized;

        public void Initialize(string name, NativeArray<Point> points)
        {
            Name = new FixedString64Bytes(name);
            Points = new NativeArray<Point>(NumPoints, Allocator.Persistent);
            LUT = new NativeArray<int>(LUTSize * LUTSize, Allocator.Persistent);

            ResampleJob resampleJob = new ResampleJob
            {
                InputPoints = points,
                OutputPoints = Points,
                TargetCount = NumPoints
            };
            resampleJob.Run();

            ScaleJob scaleJob = new ScaleJob { Points = Points };
            scaleJob.Run();

            TranslateJob translateJob = new TranslateJob { Points = Points, Target = Origin };
            translateJob.Run();

            MakeIntCoordsJob intCoordsJob = new MakeIntCoordsJob { Points = Points };
            intCoordsJob.Run();

            ComputeLUTJob lutJob = new ComputeLUTJob
            {
                Points = Points,
                LUT = LUT,
                LUTSize = LUTSize,
                LUTScaleFactor = LUTScaleFactor
            };
            lutJob.Run();

            IsInitialized = true;
        }

        public void Dispose()
        {
            if (Points.IsCreated) Points.Dispose();
            if (LUT.IsCreated) LUT.Dispose();
            IsInitialized = false;
        }
    }

    public struct Result
    {
        public FixedString64Bytes Name;
        public float Score;
        public float Time;

        public Result(string name, float score, float time)
        {
            Name = new FixedString64Bytes(name);
            Score = score;
            Time = time;
        }
    }

    public QDollar()
    {
        pointClouds = new NativeArray<PointCloud>(NumPointClouds, Allocator.Persistent);
        //InitializePredefinedGestures();
    }

    ~QDollar()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (pointClouds.IsCreated)
        {
            for (int i = 0; i < pointClouds.Length; i++)
            {
                if (pointClouds[i].IsInitialized)
                {
                    pointClouds[i].Dispose();
                }
            }
            pointClouds.Dispose();
        }
    }

    //private void InitializePredefinedGestures()
    //{
    //    // Example for initializing a predefined gesture
    //    NativeArray<Point> tPoints = new NativeArray<Point>(4, Allocator.TempJob);
    //    tPoints[0] = new Point(30, 7, 1);
    //    tPoints[1] = new Point(103, 7, 1);
    //    tPoints[2] = new Point(66, 7, 2);
    //    tPoints[3] = new Point(66, 87, 2);

    //    PointCloud tCloud = new PointCloud();
    //    tCloud.Initialize("T", tPoints);
    //    pointClouds[0] = tCloud;

    //    tPoints.Dispose();

    //    // Add more predefined gestures here...
    //}

    public bool AddGesture(string name, List<List<Vector2>> strokes)
    {
        int totalPoints = strokes.Sum(stroke => stroke.Count);
        NativeArray<Point> points = new NativeArray<Point>(totalPoints, Allocator.TempJob);

        int pointIndex = 0;
        for (int strokeId = 0; strokeId < strokes.Count; strokeId++)
        {
            foreach (Vector2 point in strokes[strokeId])
            {
                points[pointIndex] = new Point(point.x, point.y, strokeId + 1); // StrokeID starts from 1
                pointIndex++;
            }
        }

        PointCloud cloud = new PointCloud();
        cloud.Initialize(name, points);

        // Find an empty slot in pointClouds array
        for (int i = 0; i < pointClouds.Length; i++)
        {
            if (!pointClouds[i].IsInitialized)
            {
                pointClouds[i] = cloud;
                points.Dispose();
                return true;
            }
        }

        // If we reach here, the pointClouds array is full
        cloud.Dispose();
        points.Dispose();
        return false;
    }

    public void PrintStoredGestures()
    {
        for (int i = 0; i < pointClouds.Length; i++)
        {
            if (pointClouds[i].IsInitialized)
            {
                Debug.Log($"Gesture Name: {pointClouds[i].Name.ToString()}");
                Debug.Log("Points:");
                for (int j = 0; j < pointClouds[i].Points.Length; j++)
                {
                    var point = pointClouds[i].Points[j];
                    Debug.Log($"  Point {j + 1}: Position = {point.position}, ID = {point.ID}, IntPosition = {point.intPosition}");
                }
                Debug.Log("LUT:");
                for (int j = 0; j < pointClouds[i].LUT.Length; j++)
                {
                    if (pointClouds[i].LUT[j] != -1)
                    {
                        Debug.Log($"  LUT[{j}]: {pointClouds[i].LUT[j]}");
                    }
                }
            }
        }
    }

    //public Result Recognize(List<List<Vector2>> strokes)
    //{
    //    int totalPoints = strokes.Sum(stroke => stroke.Count);
    //    NativeArray<Point> candidatePoints = new NativeArray<Point>(totalPoints, Allocator.TempJob);

    //    int pointIndex = 0;
    //    for (int strokeId = 0; strokeId < strokes.Count; strokeId++)
    //    {
    //        foreach (Vector2 point in strokes[strokeId])
    //        {
    //            candidatePoints[pointIndex] = new Point(point.x, point.y, strokeId + 1); // ID starts from 1
    //            pointIndex++;
    //        }
    //    }

    //    Result result = Recognize(candidatePoints);
    //    candidatePoints.Dispose();
    //    return result;
    //}

    //public Result Recognize(NativeArray<Point> points)
    //{
    //    float startTime = Time.time;

    //    NativeArray<float> distances = new NativeArray<float>(pointClouds.Length, Allocator.TempJob);

    //    // Preprocess candidate points
    //    NativeArray<Point> processedPoints = new NativeArray<Point>(NumPoints, Allocator.TempJob);
    //    PreprocessPointsJob preprocessJob = new PreprocessPointsJob
    //    {
    //        InputPoints = points,
    //        OutputPoints = processedPoints,
    //        TargetCount = NumPoints
    //    };
    //    preprocessJob.Run();

    //    // Match against all point clouds
    //    CloudMatchJob matchJob = new CloudMatchJob
    //    {
    //        CandidatePoints = processedPoints,
    //        PointClouds = pointClouds,
    //        Distances = distances,
    //        LUTSize = LUTSize,
    //        LUTScaleFactor = LUTScaleFactor
    //    };
    //    matchJob.Run();

    //    float minDistance = float.MaxValue;
    //    int bestMatch = -1;
    //    for (int i = 0; i < distances.Length; i++)
    //    {
    //        if (distances[i] < minDistance && pointClouds[i].IsInitialized)
    //        {
    //            minDistance = distances[i];
    //            bestMatch = i;
    //        }
    //    }

    //    float endTime = Time.time;
    //    distances.Dispose();
    //    processedPoints.Dispose();

    //    if (bestMatch == -1)
    //        return new Result("No match.", 0, endTime - startTime);
    //    else
    //        return new Result(pointClouds[bestMatch].Name.ToString(), minDistance > 1 ? 1 / minDistance : 1, endTime - startTime);
    //}

    //public Result Recognize(List<List<Vector2>> strokes)
    //{
    //    float startTime = Time.realtimeSinceStartup;

    //    int totalPoints = strokes.Sum(stroke => stroke.Count);
    //    NativeArray<Point> candidatePoints = new NativeArray<Point>(totalPoints, Allocator.TempJob);

    //    int pointIndex = 0;
    //    for (int strokeId = 0; strokeId < strokes.Count; strokeId++)
    //    {
    //        foreach (Vector2 point in strokes[strokeId])
    //        {
    //            candidatePoints[pointIndex] = new Point(point.x, point.y, strokeId + 1); // ID starts from 1
    //            pointIndex++;
    //        }
    //    }

    //    NativeArray<Point> processedPoints = new NativeArray<Point>(NumPoints, Allocator.TempJob);
    //    NativeArray<float> distances = new NativeArray<float>(pointClouds.Length, Allocator.TempJob);

    //    // Preprocess candidate points
    //    PreprocessPointsJob preprocessJob = new PreprocessPointsJob
    //    {
    //        InputPoints = candidatePoints,
    //        OutputPoints = processedPoints,
    //        TargetCount = NumPoints
    //    };
    //    preprocessJob.Run();

    //    // Match against all point clouds
    //    for (int i = 0; i < pointClouds.Length; i++)
    //    {
    //        if (pointClouds[i].IsInitialized)
    //        {
    //            distances[i] = CloudDistance(processedPoints, pointClouds[i].Points, pointClouds[i].LUT, LUTSize, LUTScaleFactor);
    //        }
    //        else
    //        {
    //            distances[i] = float.MaxValue;
    //        }
    //    }

    //    float minDistance = float.MaxValue;
    //    int bestMatch = -1;
    //    for (int i = 0; i < distances.Length; i++)
    //    {
    //        if (distances[i] < minDistance && pointClouds[i].IsInitialized)
    //        {
    //            minDistance = distances[i];
    //            bestMatch = i;
    //        }
    //    }

    //    float endTime = Time.realtimeSinceStartup;
    //    candidatePoints.Dispose();
    //    processedPoints.Dispose();
    //    distances.Dispose();

    //    if (bestMatch == -1)
    //        return new Result("No match.", 0, endTime - startTime);
    //    else
    //        return new Result(pointClouds[bestMatch].Name.ToString(), minDistance > 1 ? 1 / minDistance : 1, endTime - startTime);
    //}

    public Result Recognize(List<List<Vector2>> strokes)
    {
        float startTime = Time.realtimeSinceStartup;

        int totalPoints = strokes.Sum(stroke => stroke.Count);
        NativeArray<Point> candidatePoints = new NativeArray<Point>(totalPoints, Allocator.TempJob);

        int pointIndex = 0;
        for (int strokeId = 0; strokeId < strokes.Count; strokeId++)
        {
            foreach (Vector2 point in strokes[strokeId])
            {
                candidatePoints[pointIndex] = new Point(point.x, point.y, strokeId + 1); // ID starts from 1
                pointIndex++;
            }
        }

        NativeArray<Point> processedPoints = new NativeArray<Point>(NumPoints, Allocator.TempJob);
        NativeArray<float> distances = new NativeArray<float>(pointClouds.Length, Allocator.TempJob);

        // Preprocess candidate points
        PreprocessCandidatePoints(candidatePoints, processedPoints);

        // Match against all point clouds
        for (int i = 0; i < pointClouds.Length; i++)
        {
            if (pointClouds[i].IsInitialized)
            {
                distances[i] = CloudDistance(processedPoints, pointClouds[i].Points, pointClouds[i].LUT, LUTSize, LUTScaleFactor);
            }
            else
            {
                distances[i] = float.MaxValue;
            }
        }

        float minDistance = float.MaxValue;
        int bestMatch = -1;
        for (int i = 0; i < distances.Length; i++)
        {
            if (distances[i] < minDistance && pointClouds[i].IsInitialized)
            {
                minDistance = distances[i];
                bestMatch = i;
            }
        }

        float endTime = Time.realtimeSinceStartup;
        candidatePoints.Dispose();
        processedPoints.Dispose();
        distances.Dispose();

        if (bestMatch == -1)
            return new Result("No match.", 0, endTime - startTime);
        else
            return new Result(pointClouds[bestMatch].Name.ToString(), minDistance, endTime - startTime);
            //return new Result(pointClouds[bestMatch].Name.ToString(), minDistance > 1 ? 1 / minDistance : 1, endTime - startTime);
    }


    private float CloudDistance(NativeArray<Point> pts1, NativeArray<Point> pts2, NativeArray<int> lut, int lutSize, float lutScaleFactor)
    {
        int n = pts1.Length;
        float sum = 0;

        for (int i = 0; i < n; i++)
        {
            int x = (int)(pts1[i].position.x * lutSize);
            int y = (int)(pts1[i].position.y * lutSize);
            int index = math.clamp(y * lutSize + x, 0, lutSize * lutSize - 1);
            int match = lut[index];

            float dist = math.distance(pts1[i].position, pts2[match].position);
            sum += dist;
        }

        return sum / n;
    }

    // Jobs for gesture processing

    [BurstCompile]
    private struct PreprocessPointsJob : IJob
    {
        [ReadOnly] public NativeArray<Point> InputPoints;
        public NativeArray<Point> OutputPoints;
        public int TargetCount;

        public void Execute()
        {
            ResampleJob resampleJob = new ResampleJob
            {
                InputPoints = InputPoints,
                OutputPoints = OutputPoints,
                TargetCount = TargetCount
            };
            resampleJob.Run();

            ScaleJob scaleJob = new ScaleJob { Points = OutputPoints };
            scaleJob.Run();

            TranslateJob translateJob = new TranslateJob { Points = OutputPoints, Target = Origin };
            translateJob.Run();

            MakeIntCoordsJob intCoordsJob = new MakeIntCoordsJob { Points = OutputPoints };
            intCoordsJob.Run();
        }
    }

    [BurstCompile]
    private struct ResampleJob : IJob
    {
        [ReadOnly] public NativeArray<Point> InputPoints;
        public NativeArray<Point> OutputPoints;
        public int TargetCount;

        public void Execute()
        {
            float intervalLength = PathLength(InputPoints) / (TargetCount - 1);
            float distanceSoFar = 0;

            OutputPoints[0] = InputPoints[0];
            int outputIndex = 1;

            for (int i = 1; i < InputPoints.Length && outputIndex < TargetCount; i++)
            {
                float distance = math.distance(InputPoints[i].position, InputPoints[i - 1].position);
                if (InputPoints[i].ID == InputPoints[i - 1].ID)
                {
                    if (distanceSoFar + distance >= intervalLength)
                    {
                        float overshoot = intervalLength - distanceSoFar;
                        float2 newPoint = math.lerp(InputPoints[i - 1].position, InputPoints[i].position, overshoot / distance);
                        OutputPoints[outputIndex] = new Point(newPoint.x, newPoint.y, InputPoints[i].ID);
                        outputIndex++;
                        i--; // Stay on the same point for the next iteration
                        distanceSoFar = 0;
                    }
                    else
                    {
                        distanceSoFar += distance;
                    }
                }
            }

            // Fill any remaining points with the last point
            while (outputIndex < TargetCount)
            {
                OutputPoints[outputIndex] = InputPoints[InputPoints.Length - 1];
                outputIndex++;
            }
        }

        private float PathLength(NativeArray<Point> points)
        {
            float length = 0;
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].ID == points[i - 1].ID)
                    length += math.distance(points[i].position, points[i - 1].position);
            }
            return length;
        }
    }

    [BurstCompile]
    private struct ScaleJob : IJob
    {
        public NativeArray<Point> Points;

        public void Execute()
        {
            float2 min = new float2(float.MaxValue);
            float2 max = new float2(float.MinValue);

            for (int i = 0; i < Points.Length; i++)
            {
                min = math.min(min, Points[i].position);
                max = math.max(max, Points[i].position);
            }

            float size = math.max(max.x - min.x, max.y - min.y);
            if (size > 0)
            {
                for (int i = 0; i < Points.Length; i++)
                {
                    Points[i] = new Point(
                        (Points[i].position.x - min.x) / size,
                        (Points[i].position.y - min.y) / size,
                        Points[i].ID
                    );
                }
            }
        }
    }

    [BurstCompile]
    private struct TranslateJob : IJob
    {
        public NativeArray<Point> Points;
        public float2 Target;

        public void Execute()
        {
            float2 centroid = float2.zero;
            for (int i = 0; i < Points.Length; i++)
            {
                centroid += Points[i].position;
            }
            centroid /= Points.Length;

            float2 translation = Target - centroid;
            for (int i = 0; i < Points.Length; i++)
            {
                Points[i] = new Point(
                    Points[i].position.x + translation.x,
                    Points[i].position.y + translation.y,
                    Points[i].ID
                );
            }
        }
    }

    [BurstCompile]
    private struct MakeIntCoordsJob : IJob
    {
        public NativeArray<Point> Points;

        public void Execute()
        {
            for (int i = 0; i < Points.Length; i++)
            {
                Point p = Points[i];
                p.intPosition = new int2(
                    (int)math.round(p.position.x * (MaxIntCoord - 1)),
                    (int)math.round(p.position.y * (MaxIntCoord - 1))
                );
                Points[i] = p;
            }
        }
    }

    [BurstCompile]
    private struct ComputeLUTJob : IJob
    {
        [ReadOnly] public NativeArray<Point> Points;
        [WriteOnly] public NativeArray<int> LUT;
        public int LUTSize;
        public float LUTScaleFactor;

        public void Execute()
        {
            for (int index = 0; index < LUT.Length; index++)
            {
                int x = index / LUTSize;
                int y = index % LUTSize;

                float2 lutPoint = new float2(x, y) * LUTScaleFactor;

                float minDist = float.MaxValue;
                int bestIndex = -1;

                for (int i = 0; i < Points.Length; i++)
                {
                    float dist = math.distancesq(Points[i].intPosition, lutPoint);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        bestIndex = i;
                    }
                }

                LUT[index] = bestIndex;
            }
        }
    }

    private void PreprocessCandidatePoints(NativeArray<Point> inputPoints, NativeArray<Point> outputPoints)
    {
        // Resample Points
        ResampleJob resampleJob = new ResampleJob
        {
            InputPoints = inputPoints,
            OutputPoints = outputPoints,
            TargetCount = NumPoints
        };
        JobHandle resampleHandle = resampleJob.Schedule();
        resampleHandle.Complete();

        // Scale Points
        ScaleJob scaleJob = new ScaleJob { Points = outputPoints };
        JobHandle scaleHandle = scaleJob.Schedule(resampleHandle);
        scaleHandle.Complete();

        // Translate Points
        TranslateJob translateJob = new TranslateJob { Points = outputPoints, Target = Origin };
        JobHandle translateHandle = translateJob.Schedule(scaleHandle);
        translateHandle.Complete();

        // Make Integer Coordinates
        MakeIntCoordsJob intCoordsJob = new MakeIntCoordsJob { Points = outputPoints };
        JobHandle intCoordsHandle = intCoordsJob.Schedule(translateHandle);
        intCoordsHandle.Complete();
    }


    //[BurstCompile]
    //private struct CloudMatchJob : IJob
    //{
    //    [ReadOnly] public NativeArray<Point> CandidatePoints;
    //    [ReadOnly] public NativeArray<PointCloud> PointClouds;
    //    [WriteOnly] public NativeArray<float> Distances;
    //    public int LUTSize;
    //    public float LUTScaleFactor;

    //    public void Execute()
    //    {
    //        for (int i = 0; i < PointClouds.Length; i++)
    //        {
    //            if (PointClouds[i].IsInitialized)
    //            {
    //                Distances[i] = CloudDistance(CandidatePoints, PointClouds[i].Points, PointClouds[i].LUT);
    //            }
    //            else
    //            {
    //                Distances[i] = float.MaxValue;
    //            }
    //        }
    //    }

    //    private float CloudDistance(NativeArray<Point> pts1, NativeArray<Point> pts2, NativeArray<int> lut)
    //    {
    //        int n = pts1.Length;
    //        float sum = 0;

    //        for (int i = 0; i < n; i++)
    //        {
    //            int x = (int)(pts1[i].position.x * LUTSize);
    //            int y = (int)(pts1[i].position.y * LUTSize);
    //            int index = math.clamp(y * LUTSize + x, 0, LUTSize * LUTSize - 1);
    //            int match = lut[index];
    //            float dist = math.distance(pts1[i].position, pts2[match].position);
    //            sum += dist;
    //        }

    //        return sum / n;
    //    }
    //}
    //// Helper class to replace Time.realtimeSinceStartup
    //public static class Time
    //{
    //    public static float realtimeSinceStartup => (float)System.DateTime.Now.TimeOfDay.TotalSeconds;
    //}
    //// Helper struct to replace Vector2
    //public struct Vector2
    //{
    //    public float x;
    //    public float y;
    //    public Vector2(float x, float y)
    //    {
    //        this.x = x;
    //        this.y = y;
    //    }
    //}
}