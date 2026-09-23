using UnityEngine;

public class LevelScript : MonoBehaviour
{
    private EndlessRunnerEngine eng;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        eng = GetComponent<EndlessRunnerEngine>(); 
    }

    void Start()
    {
        Block b=eng.CurrentBlock;
        Debug.Log("Current block: " + b.Id);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
