using UnityEngine;

public abstract class LevelCallback : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public abstract void show(Block block);

    public abstract void hide(Block hide);
}
