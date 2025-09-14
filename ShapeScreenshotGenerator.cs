using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class ShapeScreenshotGenerator : MonoBehaviour
{
    public Camera screenshotCamera;
    public int imageWidth = 256;
    public int imageHeight = 256;
    public int samplesPerShape = 20;
    public float positionOffset = 0.3f;
    
    [Header("Shape Settings")]
    public float minIrregularity = 0.05f;
    public float maxIrregularity = 0.07f;
    public int circleSegments = 16;
    public float lineThickness = 0.08f;
    
    [Header("Size Variation")]
    public float minSize = 0.6f;
    public float maxSize = 1.2f;

    private string savePath;

    void Start()
    {
        savePath = Path.Combine(Application.dataPath, "GeneratedShapes");
        Directory.CreateDirectory(savePath);
        
        // na exei aspro background - den bgainei polu kala moiazei gkri (isos exei alpha channel??)
        screenshotCamera.backgroundColor = Color.white;
        screenshotCamera.clearFlags = CameraClearFlags.SolidColor;
        screenshotCamera.orthographic = true;
        screenshotCamera.orthographicSize = 1.5f;
        
        StartCoroutine(CaptureAllShapes());
    }

    IEnumerator CaptureAllShapes()
    {
        yield return CaptureShape("circle");
        yield return CaptureShape("triangle");
        yield return CaptureShape("square");

        Debug.Log("All screenshots captured!");
    }

    IEnumerator CaptureShape(string shapeType)
    {
        for (int i = 0; i < samplesPerShape; i++)
        {
            GameObject shapeObj = new GameObject(shapeType + "_" + i);
            LineRenderer lr = shapeObj.AddComponent<LineRenderer>();

            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.widthMultiplier = lineThickness;
            lr.startColor = Color.black;
            lr.endColor = Color.black;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.numCapVertices = 8;
            lr.numCornerVertices = 8;

            Vector3 randomPos = new Vector3(
                Random.Range(-positionOffset, positionOffset),
                Random.Range(-positionOffset, positionOffset),
                0
            );

            // tuxaia 8esh sthn eikona
            shapeObj.transform.position = randomPos;
            
            // tuxaia 8esh gia circles kai triangles - squares den prepei na exoun rotation (gia auto to dataset)
            float randomRotation = shapeType == "square" ? 0f : Random.Range(0f, 360f);
            shapeObj.transform.rotation = Quaternion.Euler(0, 0, randomRotation);

            // tuxaio mege8os kai ateleies
            float irregularity = Random.Range(minIrregularity, maxIrregularity);
            float size = Random.Range(minSize, maxSize);

            switch (shapeType)
            {
                case "circle": DrawHandDrawnCircle(lr, irregularity, size); break;
                case "triangle": DrawHandDrawnPolygon(lr, 3, irregularity, size); break;
                case "square": DrawHandDrawnSquare(lr, irregularity, size); break;
            }

            yield return new WaitForEndOfFrame();

            string filename = Path.Combine(savePath, $"{shapeType}_{i}.png");
            TakeHighQualityScreenshot(filename);

            Destroy(shapeObj);
            yield return null;
        }
    }

    void TakeHighQualityScreenshot(string filename)
    {
        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        rt.filterMode = FilterMode.Trilinear;

        screenshotCamera.targetTexture = rt;
        RenderTexture.active = rt;
        
        // clear opengl func me to default white background ths
        GL.Clear(true, true, Color.white);
        screenshotCamera.Render();

        Texture2D screenShot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false, true);
        screenShot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        screenShot.Apply();

        // clean
        screenshotCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // save .png
        byte[] bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(filename, bytes);
        Destroy(screenShot);
    }

    void DrawHandDrawnCircle(LineRenderer lr, float irregularity, float size)
    {
        List<Vector3> points = new List<Vector3>();
        int irregularityPoints = Random.Range(3, 6);

        // random irregularity points tou kuklou (ta shmeia pou den 8a einai teleios)
        float[] irregularityAngles = new float[irregularityPoints];
        float[] irregularityStrengths = new float[irregularityPoints];
        
        for (int i = 0; i < irregularityPoints; i++)
        {
            irregularityAngles[i] = Random.Range(0f, 360f);
            irregularityStrengths[i] = Random.Range(irregularity * 0.8f, irregularity * 1.2f);
        }

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = 360f * i / circleSegments;
            float radius = size;

            //smooth ta irregular shmeia
            for (int j = 0; j < irregularityPoints; j++)
            {
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle, irregularityAngles[j]));
                if (angleDiff < 60f)
                {
                    // Smooth falloff sta irregular kommatia tou kuklou
                    float influence = Mathf.Pow(Mathf.Cos(angleDiff * Mathf.Deg2Rad * 1.5f), 2f);
                    radius += Mathf.Sin(angle * Mathf.Deg2Rad * 2f) * irregularityStrengths[j] * influence * size;
                }
            }

            // mikro random variation
            radius += Random.Range(-irregularity * 0.1f, irregularity * 0.1f) * size;

            Vector3 point = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                0
            );

            points.Add(point);
        }

        // kai meta pali smooth
        points = SmoothPoints(points, 0.3f);
        
        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
    }

    List<Vector3> SmoothPoints(List<Vector3> points, float smoothness)
    {
        if (points.Count < 3) return points;

        List<Vector3> smoothed = new List<Vector3>();
        smoothed.Add(points[0]);

        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 previous = points[i - 1];
            Vector3 current = points[i];
            Vector3 next = points[i + 1];

            Vector3 smoothedPoint = (previous * 0.25f + current * 0.5f + next * 0.25f);
            smoothed.Add(smoothedPoint);
        }

        smoothed.Add(points[points.Count - 1]);
        return smoothed;
    }

    void DrawHandDrawnPolygon(LineRenderer lr, int sides, float irregularity, float size)
    {
        List<Vector3> points = new List<Vector3>();
        float startAngle = Random.Range(0f, 360f);

        Vector3[] corners = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = startAngle + 360f * i / sides;
            float radius = size + Random.Range(-irregularity * 0.2f, irregularity * 0.2f) * size;
            
            corners[i] = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                0
            );
        }

        for (int i = 0; i < sides; i++)
        {
            Vector3 currentCorner = corners[i];
            Vector3 nextCorner = corners[(i + 1) % sides];

            points.Add(currentCorner);

            // intermediate points me smooth curves
            int intermediatePoints = 10;
            for (int j = 1; j < intermediatePoints; j++)
            {
                float t = j / (float)intermediatePoints;
                Vector3 point = Vector3.Lerp(currentCorner, nextCorner, t);

                // curve pou fainetai grammeno sto xeri
                float curveStrength = Mathf.Sin(t * Mathf.PI) * irregularity * 0.3f * size;
                Vector3 perpendicular = Vector3.Cross(nextCorner - currentCorner, Vector3.forward).normalized;
                point += perpendicular * curveStrength;

                // pros8etoume sto telos random variation
                point += (Vector3)Random.insideUnitCircle * irregularity * 0.1f * size;

                points.Add(point);
            }
        }

        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
    }

    void DrawHandDrawnSquare(LineRenderer lr, float irregularity, float size)
    {
        List<Vector3> points = new List<Vector3>();
        
        float width = size * Random.Range(0.9f, 1.1f);
        float height = size * Random.Range(0.9f, 1.1f);
        
        // 8ese gonies me orizontia kato grammh
        Vector3 bottomLeft = new Vector3(-width, -height, 0);
        Vector3 bottomRight = new Vector3(width, -height, 0);
        Vector3 topRight = new Vector3(width, height, 0);
        Vector3 topLeft = new Vector3(-width, height, 0);

        // irregularity stis gonies
        bottomLeft += (Vector3)Random.insideUnitCircle * irregularity * 0.2f * size;
        bottomRight += (Vector3)Random.insideUnitCircle * irregularity * 0.2f * size;
        topRight += (Vector3)Random.insideUnitCircle * irregularity * 0.2f * size;
        topLeft += (Vector3)Random.insideUnitCircle * irregularity * 0.2f * size;

        Vector3[] corners = new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };

        // gonies pou fainontai grammenes sto xeri (ateleies)
        for (int i = 0; i < 4; i++)
        {
            Vector3 currentCorner = corners[i];
            Vector3 nextCorner = corners[(i + 1) % 4];

            points.Add(currentCorner);

            // intermideate points me smooth curves
            int intermediatePoints = 10;
            for (int j = 1; j < intermediatePoints; j++)
            {
                float t = j / (float)intermediatePoints;
                Vector3 point = Vector3.Lerp(currentCorner, nextCorner, t);

                // curve pou fainetai grammeno sto xeri
                float curveStrength = Mathf.Sin(t * Mathf.PI) * irregularity * 0.3f * size;
                Vector3 perpendicular = Vector3.Cross(nextCorner - currentCorner, Vector3.forward).normalized;
                point += perpendicular * curveStrength;

                // mikro random variation
                point += (Vector3)Random.insideUnitCircle * irregularity * 0.1f * size;

                points.Add(point);
            }
        }

        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
    }
}