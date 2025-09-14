using UnityEngine;
using System.IO;
using System.Collections;

public class ShapeScreenshotGenerator2_Final : MonoBehaviour
{
    public Camera screenshotCamera;
    public int imageWidth = 224;
    public int imageHeight = 224;

    private string savePath;

    private Vector2 circleScaleRange = new Vector2(0.45f, 0.75f);
    private Vector2 squareScaleRange = new Vector2(0.45f, 0.75f);
    private Vector2 triangleScaleRange = new Vector2(0.45f, 0.75f);

    void Start()
    {
        savePath = Path.Combine(Application.dataPath, "GeneratedShapes2");
        Directory.CreateDirectory(savePath);

        screenshotCamera.backgroundColor = new Color(1f, 1f, 1f, 1f);
        screenshotCamera.clearFlags = CameraClearFlags.SolidColor;
        screenshotCamera.orthographic = true;
        screenshotCamera.orthographicSize = 1.5f;

        StartCoroutine(CaptureAllShapes());
    }

    IEnumerator CaptureAllShapes()
    {
        for (int i = 0; i < 20; i++)
        {
            yield return CaptureCircle("circle_green_" + i, new Color32(0, 128, 0, 255));
            yield return CaptureCircle("circle_pink_" + i, new Color32(255, 192, 203, 255));
        }

        for (int i = 0; i < 40; i++)
        {
            yield return CaptureSquare("square_pink_" + i, new Color32(255, 192, 203, 255));
        }

        for (int i = 0; i < 20; i++)
        {
            yield return CaptureTriangle("triangle_purple_" + i, new Color32(128, 0, 128, 255));
            yield return CaptureTriangle("triangle_red_" + i, new Color32(165, 42, 42, 255));
        }

        Debug.Log("All shapes captured in " + savePath);
    }

    IEnumerator CaptureCircle(string filename, Color32 rgb)
    {
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(circle.GetComponent<Collider>());

        float scale = Random.Range(circleScaleRange.x, circleScaleRange.y);
        circle.transform.localScale = new Vector3(scale * 1.05f, scale * 1.05f, 0.12f);
        circle.transform.position = RandomPositionInside();

        ApplyUnlitColor(circle, rgb);

        yield return new WaitForEndOfFrame();
        SaveScreenshotToFile(filename);
        Destroy(circle);
    }

    IEnumerator CaptureSquare(string filename, Color32 rgb)
    {
        GameObject square = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(square.GetComponent<Collider>());

        float scale = Random.Range(squareScaleRange.x, squareScaleRange.y);
        square.transform.localScale = new Vector3(scale * 1.05f, scale * 1.05f, 0.12f);

        square.transform.position = RandomPositionInside();
        square.transform.rotation = Quaternion.identity;

        ApplyUnlitColor(square, rgb);

        yield return new WaitForEndOfFrame();
        SaveScreenshotToFile(filename);
        Destroy(square);
    }

    IEnumerator CaptureTriangle(string filename, Color32 rgb)
    {
        GameObject triangle = CreateTriangleMeshDoubleSided();
        float scale = Random.Range(triangleScaleRange.x, triangleScaleRange.y);
        triangle.transform.localScale = Vector3.one * scale * 1.05f;

        triangle.transform.position = RandomPositionInBottomRight();

        triangle.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        ApplyUnlitColor(triangle, rgb);

        yield return new WaitForEndOfFrame();
        SaveScreenshotToFile(filename);
        Destroy(triangle);
    }

    Vector3 RandomPositionInside()
    {
        return new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.8f, 0.8f), 0f);
    }

    Vector3 RandomPositionInBottomRight()
    {
        return new Vector3(Random.Range(0.2f, 0.8f), Random.Range(-0.8f, -0.2f), 0f);
    }

    void ApplyUnlitColor(GameObject obj, Color32 rgb)
    {
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = rgb;
        obj.GetComponent<MeshRenderer>().material = mat;
    }

    void SaveScreenshotToFile(string filename)
    {
        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24);
        screenshotCamera.targetTexture = rt;

        Texture2D screenShot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
        screenshotCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        screenshotCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        string filePath = Path.Combine(savePath, filename + ".png");
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Saved " + filePath);
    }

    GameObject CreateTriangleMeshDoubleSided()
    {
        GameObject triangle = new GameObject("Triangle");
        MeshFilter mf = triangle.AddComponent<MeshFilter>();
        MeshRenderer mr = triangle.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0.5f, 0),
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0)
        };

        int[] triangles = new int[]
        {
            0, 1, 2,
            2, 1, 0
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;
        return triangle;
    }
}
