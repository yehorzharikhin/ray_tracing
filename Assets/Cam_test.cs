using UnityEngine;

[ExecuteInEditMode]
public class Cam_test : MonoBehaviour
{
    public int x = 3, y = 3; // Grid dimensions
    public float r = 1.0f;   // Sphere radius
    public Color c = Color.white; // Sphere color

    private GameObject[,] spheres;

    // Called once before the first execution of Update
    void Start()
    {
        GenerateSpheres();
    }

    // Update is called every frame, even in Edit Mode
    void Update()
    {
        if (spheres == null || spheres.GetLength(0) != x || spheres.GetLength(1) != y)
        {
            ClearSpheres();
            GenerateSpheres();
        }

        PositionSpheres();
    }

    // Generate the spheres
    private void GenerateSpheres()
    {
        spheres = new GameObject[x, y];

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                spheres[i, j] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spheres[i, j].transform.localScale = new Vector3(r, r, r);

                // Set material and color with a proper method
                Renderer renderer = spheres[i, j].GetComponent<Renderer>();

                // Create new material if necessary
                Material newMaterial = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial = newMaterial;
                renderer.sharedMaterial.color = c;  // Set the color to the desired value

                // Make sure the spheres are created as children of this GameObject for organization
                spheres[i, j].transform.parent = this.transform;
            }
        }
    }

    // Position the spheres in a grid in the camera's view
    private void PositionSpheres()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera not found!");
            return;
        }

        float distance = mainCamera.nearClipPlane + r;

        // Calculate viewport corners
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, distance));
        Vector3 length = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, distance)) - bottomLeft;
        Vector3 height = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, distance)) - bottomLeft;

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                // Calculate the position of each sphere
                Vector3 new_position = bottomLeft + height * (1f * i / (x - 1)) + length * (1f * j / (y - 1));
                spheres[i, j].transform.position = new_position;
            }
        }
    }

    // Clear the previously created spheres
    private void ClearSpheres()
    {
        if (spheres != null)
        {
            foreach (var sphere in spheres)
            {
                if (sphere != null)
                {
                    DestroyImmediate(sphere);
                }
            }
        }
    }

    // This method is called when the application quits
    void OnApplicationQuit()
    {
        // Clean up by destroying the spheres when the application is stopped
        ClearSpheres();
    }
}
