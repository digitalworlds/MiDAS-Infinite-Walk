using System.Diagnostics.Contracts;
using Unity.XR.CoreUtils;
using UnityEngine;

public class LevelScript : LevelCallback
{
    public GameObject[] leftTurns;
    public GameObject[] rightTurns;
    public GameObject[] Straights;
    
    void Start()
    {

       
    }

    void InstantiateObject(GameObject o, Block b)
    {
        GameObject obj = Instantiate(o);
        obj.transform.localPosition = new Vector3(0f,0f,0.6f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.transform.SetParent(b.GameObject.transform, false);
        obj.SetActive(true);
    }

    void CreateLeftTurn(Block b)
    {
        if(!b.isDecorated)
        {
            b.isDecorated = true;
            b.decorationIds[0] = Random.Range(0, leftTurns.Length);
        }
        InstantiateObject(leftTurns[b.decorationIds[0]],b);
    }

    void CreateRightTurn(Block b)
    {
        if(!b.isDecorated)
        {
            b.isDecorated = true;
            b.decorationIds[0] = Random.Range(0, rightTurns.Length);
        }
        InstantiateObject(rightTurns[b.decorationIds[0]],b);
    }

    void CreateStraight(Block b)
    {
        if(!b.isDecorated)
        {
            b.isDecorated = true;
            b.decorationIds[0] = Random.Range(0, Straights.Length);
        }
        InstantiateObject(Straights[b.decorationIds[0]],b);
    }

    public override void show(Block b)
    {
        //It has already been built, so we don't need to build it again
        if(b.GameObject.transform.childCount > 0)return;
        
        if(b.decorationIds == null || b.decorationIds.Length != 3)
        {
            b.decorationIds = new int[3];
        }

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
        //destroy all children of the block's game object
        foreach(Transform child in b.GameObject.transform)
        {
            Destroy(child.gameObject);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
