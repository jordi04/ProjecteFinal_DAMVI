#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// Helper class to quickly tag rocks in the scene
public class RockTagHelper : MonoBehaviour
{
    [MenuItem("Custom Tools/Tag All Rocks")]
    public static void TagAllRocks()
    {
        // Find all GameObjects with "rock" in their name
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int count = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("golemrock") || obj.name.ToLower().Contains("golemprojectile"))
            {
                // Add GolemProjectile component if it doesn't have one
                if (obj.GetComponent<GolemProjectile>() == null)
                {
                    obj.AddComponent<GolemProjectile>();

                    // If it doesn't have a rigidbody, add one
                    if (obj.GetComponent<Rigidbody>() == null)
                    {
                        Rigidbody rb = obj.AddComponent<Rigidbody>();
                        rb.mass = 10f; // Heavier than default
                    }

                    // If it doesn't have a collider, add one
                    if (obj.GetComponent<Collider>() == null)
                    {
                        obj.AddComponent<SphereCollider>();
                    }
                }

                // Tag the object
                obj.tag = "GolemRock";
                count++;
            }
        }

        Debug.Log($"Tagged {count} rocks with 'GolemRock' tag");
    }
}
#endif