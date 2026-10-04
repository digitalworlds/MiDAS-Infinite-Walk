using UnityEngine;

/*
 * LevelCallback is an abstract class that defines the interface for handling
 * the display and hiding of blocks in the level. Level making scripts must 
 * inherit LevelCallback must implement the show and hide methods.
 */
public abstract class LevelCallback : MonoBehaviour
{
    public abstract void show(Block block);
    public abstract void hide(Block block);
}
