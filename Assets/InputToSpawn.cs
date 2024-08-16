using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputToSpawn : MonoBehaviour
{
    public List<GameObject> registeredSpawnables = default; 
    private List<string> registeredSpawnablesNames = default;
    [SerializeField] private string inputSpawn = "";

    private void Start()
    {
        for (int i = 0; i < registeredSpawnables.Count; i++)
        {
            registeredSpawnablesNames[i] = registeredSpawnables[i].name;
            print(registeredSpawnablesNames[i]);
        }
    }

    public void Spawn(string name)
    {
        Instantiate(registeredSpawnables[registeredSpawnablesNames.FindIndex( x => x.Equals(name))]);
    }

    private void Update()
    {
        Debug.Log("wow");
        Spawn(inputSpawn);
    }
}
