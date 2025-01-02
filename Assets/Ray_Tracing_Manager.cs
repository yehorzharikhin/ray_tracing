using UnityEngine;
using System.Collections.Generic;
using System;

//const float INF_float = 1000000000.0f;
//const int INF = 1000000000;

public struct Ray {
    public Vector3 origin;
    public Vector3 dir;
    public Color color;

    public Ray(Vector3 origin, Vector3 dir, Color color)
    {
        this.origin = origin;
        this.dir = dir.normalized;
        this.color = color;
    }
}

public struct Material {
    public Color color;

    public Material(Color color)
    {
        this.color = color;
    }
}

public struct Triangle {
    public Vector3 v0, v1, v2;
    //public Vector3 n1, n2, n3;
    public Material material;

    public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Material material)
    {
        this.v0 = v0;
        this.v1 = v1;
        this.v2 = v2;
        this.material = material;
    }
}

[RequireComponent(typeof(Camera))]
public class Ray_Tracing_Manager : MonoBehaviour
{
    public static int textureWidth;
    public static int textureHeight;

    private Texture2D renderTexture; // Texture to manipulate and display
    private Camera cam;

    // For ray tracing
    static Camera mainCamera;
    static float distance;
    static Vector3 bottomLeft;
    static Vector3 length;
    static Vector3 height;

    public List<Triangle> light_source_triangles;
    public List<Triangle> render_object_triangles;

    public int k = 0, p = 0;
    public const int bounce_limit = 1;
    System.Random rnd;

    public int[,] number_of_hits;

    void Start()
    {
        Initialize_default();
        Initialize_triangles();
        InitializeTexture(Color.black);
    }

    void Initialize_default()
    {
        textureWidth = Screen.width;
        textureHeight = Screen.height;
        mainCamera = Camera.main;
        distance = mainCamera.nearClipPlane;
        bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, distance));
        length = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, distance)) - bottomLeft;
        height = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, distance)) - bottomLeft;

        // Disable default camera rendering
        cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; // Clear with a solid color
        cam.backgroundColor = Color.black; // Black background
        cam.cullingMask = 0; // Prevent the camera from rendering any objects

        // Create a texture for rendering
        renderTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        renderTexture.filterMode = FilterMode.Point;

        light_source_triangles = new List<Triangle>();
        render_object_triangles = new List<Triangle>();

        rnd = new System.Random();
        number_of_hits = new int[textureWidth, textureHeight];
        for (int i = 0; i < textureWidth; i++)
            for (int j = 0; j < textureHeight; j++)
                number_of_hits[i, j] = 0;
    }

    void Initialize_triangles()
    {
        GameObject[] light_sources = GameObject.FindGameObjectsWithTag("Light_source");
        GameObject[] render_objects = GameObject.FindGameObjectsWithTag("Render_object");

        foreach (GameObject obj in light_sources)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // Get the transformation matrix of the object
            Matrix4x4 localToWorld = obj.transform.localToWorldMatrix;

            // Convert all vertices to world space
            Vector3[] worldVertices = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                worldVertices[i] = localToWorld.MultiplyPoint3x4(vertices[i]);

            // Add triangles to the list
            for (int i = 0; i < triangles.Length; i += 3)
            {
                light_source_triangles.Add(new Triangle(
                    worldVertices[triangles[i]],
                    worldVertices[triangles[i + 1]],
                    worldVertices[triangles[i + 2]],
                    new Material(obj.GetComponent<Renderer>().material.color)
                ));
            }
        }

        foreach (GameObject obj in render_objects)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // Get the transformation matrix of the object
            Matrix4x4 localToWorld = obj.transform.localToWorldMatrix;

            // Convert all vertices to world space
            Vector3[] worldVertices = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                worldVertices[i] = localToWorld.MultiplyPoint3x4(vertices[i]);

            // Add triangles to the list
            for (int i = 0; i < triangles.Length; i += 3)
            {
                render_object_triangles.Add(new Triangle(
                    worldVertices[triangles[i]],
                    worldVertices[triangles[i + 1]],
                    worldVertices[triangles[i + 2]],
                    new Material(obj.GetComponent<Renderer>().material.color)
                ));
            }
        }
    }
    
    void InitializeTexture(Color color)
    {
        // Fill the texture with a specified color
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                renderTexture.SetPixel(x, y, color);
            }
        }
        renderTexture.Apply();
    }
    
    void OnGUI()
    {
        // Step 5: Draw the texture to the screen
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
    }

    void Update()
    {
        for (int i = 0; i < 100; i++)
        {
            int x = rnd.Next(textureWidth / 5);
            int y = rnd.Next(textureHeight / 5);
            // Calculate the new position on the image plane
            Vector3 new_position = bottomLeft + height * (((1f * y + 0.5f) / (textureHeight / 5 - 1))) + length * (((1f * x + 0.5f) / (textureWidth / 5 - 1)));

            // Compute the direction vector
            Vector3 dir = (new_position - mainCamera.transform.position).normalized;

            p = bounce_limit;
            k++;
            // Create a custom Ray instance
            Ray ray = new Ray(mainCamera.transform.position, dir, Color.white);
            Color new_color = Ray_trace(ray);
            new_color = (renderTexture.GetPixel(5 * x, 5 * y) / 3.0f * number_of_hits[5 * x, 5 * y] + new_color) / (number_of_hits[5 * x, 5 * y] + 1);
            //renderTexture.SetPixel(x, y, new_color * 2);
            //number_of_hits[x, y]++;
            for (int j = 5 * x; j < 5 * x + 5; j++)
            {
                for (int l = 5 * y; l < 5 * y + 5; l++)
                {
                    renderTexture.SetPixel(j, l, new_color * 3.0f);
                    number_of_hits[j, l]++;
                }
            }
        }
        renderTexture.Apply(); // Apply the changes to the texture
    }

    float Ray_to_triangle_distance_to_hit(Ray ray, Triangle triangle)
    {
        // Vertices of the triangle
        Vector3 v0 = triangle.v0;
        Vector3 v1 = triangle.v1;
        Vector3 v2 = triangle.v2;

        // Edges of the triangle
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;

        // Begin calculating determinant - also used to calculate u parameter
        Vector3 pvec = Vector3.Cross(ray.dir, edge2);
        float det = Vector3.Dot(edge1, pvec);

        // If the determinant is near zero, the ray lies in the plane of the triangle
        if (Mathf.Abs(det) < Mathf.Epsilon)
            return -1.0f;

        float invDet = 1.0f / det;

        // Calculate distance from v0 to ray origin
        Vector3 tvec = ray.origin - v0;

        // Calculate u parameter and test bounds
        float u = Vector3.Dot(tvec, pvec) * invDet;
        if (u < 0.0f || u > 1.0f)
            return -1.0f;

        // Prepare to test v parameter
        Vector3 qvec = Vector3.Cross(tvec, edge1);

        // Calculate v parameter and test bounds
        float v = Vector3.Dot(ray.dir, qvec) * invDet;
        if (v < 0.0f || u + v > 1.0f)
            return -1.0f;

        // Calculate t, ray intersects triangle
        float t = Vector3.Dot(edge2, qvec) * invDet;

        // If t is negative, the triangle is behind the ray
        if (t < 0.0f)
            return -1.0f;

        // Return the distance to the intersection point
        return t;
    }

    Vector3 Find_triangle_normal(Triangle triangle, Vector3 hit_point)
    {
        Vector3 edge1 = triangle.v1 - triangle.v0;
        Vector3 edge2 = triangle.v2 - triangle.v0;

        return Vector3.Cross(edge1, edge2).normalized;
    }

    Vector3 Choose_reflection_dir(Vector3 dir, Vector3 normal)
    {
        for (int i = 0; i < 100; i++)
        {
            float x = (float)rnd.NextDouble() * 2.0f - 1.0f;
            float y = (float)rnd.NextDouble() * 2.0f - 1.0f;
            float z = (float)rnd.NextDouble() * 2.0f - 1.0f;
            Vector3 point = new Vector3(x, y, z);
            float dst_sqrd = Vector3.Dot(point, point);

            if (dst_sqrd > 1)
                continue;

            if (Vector3.Dot(point, normal) < 0.0f)
                point = -point;
            return point.normalized;
        }

        return Vector3.zero;
    }

    Color Ray_trace(Ray ray)
    { 
        bool is_hit = false;
        bool hit_light = false;
        Vector3 hit_point;
        float distance_to_hit = 1000000000.0f;
        Triangle triangle = new Triangle(Vector3.zero, Vector3.zero, Vector3.zero, new Material(Color.black));

        for (int i = 0; i < light_source_triangles.Count; i++)
        {
            float dst = Ray_to_triangle_distance_to_hit(ray, light_source_triangles[i]);
            if (dst == -1.0f || dst > distance_to_hit)
                continue;
            is_hit = true;
            hit_light = true;
            distance_to_hit = dst;
            triangle = light_source_triangles[i];
        }

        for (int i = 0; i < render_object_triangles.Count; i++)
        {
            float dst = Ray_to_triangle_distance_to_hit(ray, render_object_triangles[i]);
            if (dst == -1.0f || dst > distance_to_hit)
                continue;
            is_hit = true;
            hit_light = false;
            distance_to_hit = dst;
            triangle = render_object_triangles[i];
        }

        if (!is_hit)
            return Color.black;
        
        hit_point = ray.origin + ray.dir * distance_to_hit;
        ray.color *= triangle.material.color;

        if (hit_light)
            return ray.color;
        if (p <= 0)
            return Color.black;
        
        p--;
        ray.origin = hit_point;
        Vector3 normal = Find_triangle_normal(triangle, hit_point);
        if (Vector3.Dot(ray.dir, normal) > 0.0f)
            normal = -normal;
        ray.dir = Choose_reflection_dir(ray.dir, normal);
        ray.color *= Vector3.Dot(normal, ray.dir);

        return Ray_trace(ray);
    }
}