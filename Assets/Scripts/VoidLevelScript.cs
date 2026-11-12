using System.Diagnostics.Contracts;
using UnityEngine;

public class VoidLevelScript : LevelCallback
{
    public GameObject floor;

    void InstantiateObject(GameObject o, Block b)
    {
        GameObject obj = Instantiate(o);
        obj.transform.localPosition = new Vector3(0f,0f,0.6f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.transform.SetParent(b.GameObject.transform, false);
        obj.SetActive(true);
    }

    void CreateFloor(Block b)
    {
        InstantiateObject(floor, b);
    }

    public override void show(Block b)
    {
        if(b.isDecorated)return;
        
        b.isDecorated = true;
        b.decorationIds = new int[3];

        CreateFloor(b);
    }

    public override void hide(Block b)
    {
        
    }
}
