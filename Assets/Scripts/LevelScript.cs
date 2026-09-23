using UnityEngine;

public class LevelScript : MonoBehaviour
{
    public GameObject[] leftTurns;
    public GameObject[] rightTurns;
    public GameObject[] Straights;
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

        while (b != null)
        {
            Debug.Log("Processing block: " + b.Id);
            if(b.type == BlockType.TurnLeft)
            {
                GameObject obj = Instantiate(leftTurns[Random.Range(0, leftTurns.Length)]);
                obj.transform.localPosition = new Vector3(0f,0f,0.6f);
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;
                obj.transform.SetParent(b.GameObject.transform, false);
                obj.SetActive(true);
                Debug.Log("Instantiated left turn for block: " + b.Id);
            }
            else if(b.type == BlockType.TurnRight)
            {
                GameObject obj = Instantiate(rightTurns[Random.Range(0, rightTurns.Length)]);
                obj.transform.localPosition = new Vector3(0f,0f,0.6f);
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;
                obj.transform.SetParent(b.GameObject.transform, false);
                obj.SetActive(true);
                Debug.Log("Instantiated right turn for block: " + b.Id);
            }
            else if(b.type == BlockType.Straight)
            {
                GameObject obj = Instantiate(Straights[Random.Range(0, Straights.Length)]);
                obj.transform.localPosition = new Vector3(0f,0f,0.6f);
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;
                obj.transform.SetParent(b.GameObject.transform, false);
                obj.SetActive(true);
                Debug.Log("Instantiated straight for block: " + b.Id);
            }
            b=b.Next;
        }

        //GameObject obj = Instantiate(library[permutations[i]]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
