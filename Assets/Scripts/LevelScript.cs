using System.Diagnostics.Contracts;
using UnityEngine;

public class LevelScript : LevelCallback
{
    public GameObject[] leftTurns;
    public GameObject[] rightTurns;
    public GameObject[] Straights;
    
    void Start()
    {

       
    }

    void CreateLeftTurn(Block b)
    {
        GameObject obj = Instantiate(leftTurns[Random.Range(0, leftTurns.Length)]);
        obj.transform.localPosition = new Vector3(0f,0f,0.6f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.transform.SetParent(b.GameObject.transform, false);
        obj.SetActive(true);
        //Debug.Log("Instantiated left turn for block: " + b.Id);
    }

    void CreateRightTurn(Block b)
    {
        GameObject obj = Instantiate(rightTurns[Random.Range(0, rightTurns.Length)]);
        obj.transform.localPosition = new Vector3(0f,0f,0.6f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.transform.SetParent(b.GameObject.transform, false);
        obj.SetActive(true);
       // Debug.Log("Instantiated right turn for block: " + b.Id);
    }

    void CreateStraight(Block b)
    {
        GameObject obj = Instantiate(Straights[Random.Range(0, Straights.Length)]);
        obj.transform.localPosition = new Vector3(0f,0f,0.6f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.transform.SetParent(b.GameObject.transform, false);
        obj.SetActive(true);
       // Debug.Log("Instantiated straight for block: " + b.Id);
    }

    public override void show(Block b)
    {
        if(b.type == BlockType.TurnLeft)
        {
            CreateLeftTurn(b);
        }
        else if(b.type == BlockType.TurnRight)
        {
            CreateRightTurn(b);
        }
        else if(b.type == BlockType.Straight)
        {
            CreateStraight(b);
        }
    }

    public override void hide(Block b)
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
