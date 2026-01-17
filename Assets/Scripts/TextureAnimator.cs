using UnityEngine;
using System.Collections;

public class TextureAnimator : MonoBehaviour
{
    public Texture2D[] frames;
    public float framesPerSecond = 10f;
    
    private Renderer rend;
    private int currentFrame = 0;
    private float timer;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / framesPerSecond)
        {
            timer -= 1f / framesPerSecond;
            currentFrame = (currentFrame + 1) % frames.Length;
            
            // Apply to main texture
            if(rend != null) rend.material.mainTexture = frames[currentFrame];
        }
    }
}
