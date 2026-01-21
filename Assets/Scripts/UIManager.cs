using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

[RequireComponent(typeof(VideoPlayer))] 
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Assets (Managed by GameManager)")]
    [HideInInspector] public Texture2D titleTexture;
    [HideInInspector] public Texture2D menuBackground;
    [HideInInspector] public Texture2D gameOverBackground; 
    [HideInInspector] public Texture2D optionsBackground; // New
    [HideInInspector] public Texture2D controlsBackground; // New
    [HideInInspector] public Texture2D uiButtonTexture; // New
    [HideInInspector] public Texture2D topScorersBoxTexture; // New
    [HideInInspector] public Texture2D lifeIcon;
    [HideInInspector] public Texture2D extraLifeIcon;
    [HideInInspector] public Texture2D bombIcon;
    
    // PowerUp Icons
    [HideInInspector] public Texture2D shieldIcon;
    [HideInInspector] public Texture2D speedIcon;
    [HideInInspector] public Texture2D timeStopIcon;
    [HideInInspector] public Texture2D ammoIcon; 
    [HideInInspector] public Texture2D turretIcon; // New
    [HideInInspector] public Texture2D goImage; 

    [Header("Config")]
    public float heartIconSize = 40f;
    
    // Shared State
    public bool showOptions = false;
    public bool showControls = false; 
    
    // Video State 
    public VideoPlayer videoPlayer;
    public RenderTexture videoTexture;
    
    // Black Texture 
    public Texture2D blackTexture;

    // Settings Values
    float masterVol = 1.0f;
    float musicVol = 1.0f;
    float sfxVol = 1.0f;
    
    // Debug
    // private bool loggedVideoStatus = false; // Unused

    private void Awake()
    {
        if (Instance == null)
        {
             Instance = this;
        }
        else if (Instance != this)
        {
             Destroy(gameObject);
             return;
        }
    }

    void OnGUI()
    {
        try {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            // DISCO BLACKOUT (High Priority Overlay)
            if (gm.blackScreenAlpha > 0f)
            {
                if (blackTexture == null) InitBlackTexture();
                
                Color backup = GUI.color;
                GUI.color = new Color(0, 0, 0, gm.blackScreenAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
                GUI.color = backup;
            }

            if (showControls)
            {
                DrawControls();
                return;
            }

            if (showOptions)
            {
                DrawOptions();
                return; 
            }

            if (gm.CurrentState == GameManager.GameState.MENU)
            {
                DrawMenu();
            }
            else if (gm.CurrentState == GameManager.GameState.PLAYING)
            {
                DrawHUD(gm);
                if (gm.IsIntroSequence || gm.showGoImage) DrawIntro(gm);
            }
            else if (gm.CurrentState == GameManager.GameState.PAUSED)
            {
                DrawPause(gm);
            }
            else if (gm.CurrentState == GameManager.GameState.GAMEOVER)
            {
                // Ensure VideoPlayer exists for Game Over if needed
                // SAFE CHECK for nulls
                bool hasVideoAsset = false;
                if (!object.ReferenceEquals(gm.gameOverVideo, null)) hasVideoAsset = true;
                else if (!object.ReferenceEquals(gm.gameOverBackgroundAsset, null)) hasVideoAsset = true;

                if (videoPlayer == null && hasVideoAsset)
                {
                    videoPlayer = GetComponent<VideoPlayer>();
                    if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
                }

                DrawGameOver(gm);
            }
        }
        catch (System.Exception e)
        {
             // Only log unique errors to avoid spamming 60fps
             if (e.Message != lastErrorMessage)
             {
                 lastErrorMessage = e.Message;
                 Debug.LogError($"[UIManager OnGUI Crash] {e.Message} \n {e.StackTrace}");
             }
        }
    }
    
    private string lastErrorMessage = "";

    // ===================================================================================
    //                                  SHARED HELPERS
    // ===================================================================================

    private Texture2D lastFailedCursor = null;

    private void SafeSetCursor(Texture2D texture, Vector2 hotspot, CursorMode mode)
    {
        if (texture == null) 
        {
            Cursor.SetCursor(null, Vector2.zero, mode);
            return;
        }

        // Prevent spamming the same bad texture
        if (texture == lastFailedCursor) return; 

        try
        {
            Cursor.SetCursor(texture, hotspot, mode);
        }
        catch (System.Exception)
        {
            // Silently fail to avoid console spam as requested
            lastFailedCursor = texture;
        }
    }

    public void PrepareVideo(VideoClip clip)
    {
        if (clip == null) return;
        
        // 1. Ensure Component exists
        if (videoPlayer == null) 
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        // 2. Ensure Enabled
        if (!videoPlayer.enabled) videoPlayer.enabled = true;

        // 3. Ensure Render Texture
        if (videoTexture == null || videoTexture.width != Screen.width || videoTexture.height != Screen.height)
        {
            if (videoTexture != null) videoTexture.Release();
            videoTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            videoTexture.Create();
        }

        // 4. Configure Critical Settings (Only set if different to avoid overhead/restarts)
        // bool configChanged = false; // Unused

        // Logic: Time Scale 0 requires Freerun. 
        if (videoPlayer.timeReference != VideoTimeReference.Freerun) 
        { 
            videoPlayer.timeReference = VideoTimeReference.Freerun; 
            // configChanged = true; 
        }

        // Logic: Must render to texture
        if (videoPlayer.renderMode != VideoRenderMode.RenderTexture) 
        { 
            videoPlayer.renderMode = VideoRenderMode.RenderTexture; 
            // configChanged = true; 
        }

        // Logic: Bind texture
        if (videoPlayer.targetTexture != videoTexture) 
        { 
            videoPlayer.targetTexture = videoTexture; 
            // Note: Changing target texture usually doesn't require stop/play, but good to ensure
        }

        // Logic: Looping
        if (!videoPlayer.isLooping) 
        { 
            videoPlayer.isLooping = true; 
            // configChanged = true;
        }

        // 5. Assign Clip
        if (videoPlayer.clip != clip)
        {
            videoPlayer.Stop(); // Ensure clean state switch
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = clip;
            // configChanged = true; 
        }

        // 6. Play if needed
        // Always enforce playback if we have a clip and it's not playing
        if (videoPlayer.clip != null && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
    }

    public void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
    }
    
    public void InitBlackTexture()
    {
         if (blackTexture == null)
         {
             blackTexture = new Texture2D(1, 1);
             blackTexture.SetPixel(0, 0, Color.black);
             blackTexture.Apply();
         }
    }

    // Force Reload to fix persistent NULL issues
    public void ManualTextureReload(GameManager gm)
    {
        if (uiButtonTexture == null && gm.uiButtonTexture != null) uiButtonTexture = gm.uiButtonTexture;
        if (topScorersBoxTexture == null && gm.topScorersBoxTexture != null) topScorersBoxTexture = gm.topScorersBoxTexture;
    }

    private bool hasLoggedButtonTexture = false; // Prevent log spam
    private bool hasLoggedBoxTexture = false;

    private bool DrawCustomButton(Rect rect, string text)
    {
        if (uiButtonTexture != null)
        {
            if (!hasLoggedButtonTexture) 
            {
                Debug.Log($"[UIManager] DrawCustomButton: Using Custom Texture '{uiButtonTexture.name}'");
                hasLoggedButtonTexture = true;
            }

            // 1. Draw the visual background (Scale to fill the rect)
            GUI.DrawTexture(rect, uiButtonTexture, ScaleMode.StretchToFill); 
            
            // 2. Create a style that is purely text, no background / no border
            // We use label style as base because it has NO background by default
            GUIStyle invisibleBtn = new GUIStyle(GUI.skin.label);
            
            // Ensure alignment matches button expectatons
            invisibleBtn.alignment = TextAnchor.MiddleCenter;
            invisibleBtn.fontSize = 20;
            invisibleBtn.fontStyle = FontStyle.Bold;
            
            // Interaction Colors
            invisibleBtn.normal.textColor = Color.white; 
            invisibleBtn.hover.textColor = Color.yellow; 
            invisibleBtn.active.textColor = Color.red;

            // We use GUI.Button behavior but with label visuals
            return GUI.Button(rect, text, invisibleBtn);
        }
        else
        {
            if (!hasLoggedButtonTexture) 
            {
                 Debug.LogWarning("[UIManager] DrawCustomButton: Texture is NULL, using Default Button.");
                 hasLoggedButtonTexture = true;
            }
            return GUI.Button(rect, text);
        }
    }

    // ===================================================================================
    //                                  MENUS (Former Partial)
    // ===================================================================================

    public void DrawMenu()
    {
        GameManager gm = GameManager.Instance;
        
        // AGGRESSIVE RELOAD to fix "missing texture"
        ManualTextureReload(gm);

        if (!hasLoggedBoxTexture)
        {
             Debug.Log($"[UIManager] DrawMenu Runtime Box Check: {(topScorersBoxTexture != null ? topScorersBoxTexture.name : "NULL")}");
             hasLoggedBoxTexture = true;
        }
        
        // Ensure System Cursor is visible for menus
        Cursor.visible = true;
        
        
        // Default to normal cursor at start of menu frame
        if (gm.normalCursor != null) SafeSetCursor(gm.normalCursor, gm.normalCursorHotspot, CursorMode.Auto);
        else SafeSetCursor(null, Vector2.zero, CursorMode.Auto);

        // UNIFIED BACKGROUND HANDLING
        Texture2D textureToDraw = null;
        VideoClip videoToPlay = null;

        if (gm.menuBackgroundAsset != null)
        {
            if (gm.menuBackgroundAsset is Texture2D)
            {
                textureToDraw = gm.menuBackgroundAsset as Texture2D;
            }
            else if (gm.menuBackgroundAsset is VideoClip)
            {
                videoToPlay = gm.menuBackgroundAsset as VideoClip;
            }
        }

        if (videoToPlay != null)
        {
            PrepareVideo(videoToPlay);
            if (videoTexture != null)
            {
                 GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), videoTexture, ScaleMode.ScaleAndCrop);
            }
        }
        else if (textureToDraw != null)
        {
            StopVideo();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), textureToDraw, ScaleMode.ScaleAndCrop);
        }
        else
        {
            StopVideo();
            // Default Black if nothing assigned
            if (blackTexture != null)
            {
                 GUI.color = Color.black;
                 GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
                 GUI.color = Color.white;
            }
        }

        // Calculate Layout Areas
        float scoreW = 250;
        float scoreMargin = 50; // Margin on Right of Screen
        float scoreX = Screen.width - scoreW - scoreMargin;
        
        // The Top Scorers Block occupies from (scoreX) to (Screen.width).
        // User wants: "Top Scorers has a separation to its right... same margin to its left would delimit the section".
        // Top Scorers "Section" Left Edge = scoreX - scoreMargin.
        // The "Main Content Area" is everything to the left of that edge.
        float mainContentWidth = scoreX - scoreMargin;
        float centerX = mainContentWidth / 2; // Center of the Main Content Area

        // Offset based on GameManager Setting
        float buttonsCenterY = Screen.height / 2 + gm.menuVerticalOffset; 

        if (titleTexture != null)
        {
            float aspect = (float)titleTexture.width / titleTexture.height;
            
            // Width based on GameManager Setting (default 0.35 * Screen.width)
            float width = Screen.width * gm.menuTitleScale;
            float height = width / aspect;

            // Spacing based on GameManager Setting
            float titleY = buttonsCenterY - height - gm.menuTitleSpacing; 
            
            // Draw centered in MainContentWidth
            GUI.DrawTexture(new Rect(centerX - (width / 2), titleY, width, height), titleTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            headerStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(centerX - 150, Screen.height / 4, 300, 50), "ILERNA TOPDOWN", headerStyle);
        }

        // Name Input
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
        labelStyle.normal.textColor = Color.white;
        // Text changed from "PILOT NAME:" to "PLAYER NAME:"
        GUI.Label(new Rect(centerX - 100, buttonsCenterY - 90, 200, 25), "PLAYER NAME:", labelStyle);
        
        // Custom Style for Input to center text vertically
        GUIStyle inputStyle = new GUIStyle(GUI.skin.textField);
        inputStyle.alignment = TextAnchor.MiddleCenter;
        inputStyle.fontSize = 14;

        gm.playerName = GUI.TextField(new Rect(centerX - 100, buttonsCenterY - 60, 200, 30), gm.playerName, 15, inputStyle);

        // Buttons
        // Buttons shifted to use new centerX
        if (DrawCustomButton(new Rect(centerX - 100, buttonsCenterY, 200, 50), "PLAY GAME"))
        {
            gm.StartGame();
        }

        if (DrawCustomButton(new Rect(centerX - 100, buttonsCenterY + 60, 200, 50), "OPCIONES"))
        {
            showOptions = true;
        }

        if (DrawCustomButton(new Rect(centerX - 100, buttonsCenterY + 120, 200, 50), "EXIT"))
        {
            Application.Quit();
        }
        
        // High Scores (Top Right)
        float scoreY = gm.topScorersVerticalOffset;
        
        Rect scoreRect = new Rect(scoreX, scoreY, scoreW, 400);

        if (topScorersBoxTexture != null)
        {
             GUI.DrawTexture(scoreRect, topScorersBoxTexture, ScaleMode.StretchToFill);
        }
        else
        {
             // Fallback: Semi-transparent Black Box (Better than default Gray)
             if (blackTexture == null) InitBlackTexture();
             GUI.color = new Color(0, 0, 0, 0.5f);
             GUI.DrawTexture(scoreRect, blackTexture);
             GUI.color = Color.white;
        }

        // Header
        GUIStyle topScoreHeaderStyle = new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter, fontStyle=FontStyle.Bold, fontSize=18 };
        topScoreHeaderStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(scoreX, scoreY + 10, scoreW, 30), "TOP SCORERS", topScoreHeaderStyle);
        
        if (gm.highScoreTable != null)
        {
            float y = scoreY + 30;
            GUIStyle listStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            listStyle.normal.textColor = Color.white;

            for (int i = 0; i < gm.highScoreTable.entries.Count; i++)
            {
               var entry = gm.highScoreTable.entries[i];
               GUI.Label(new Rect(scoreX + 20, y, 150, 25), $"{i+1}. {entry.name}", listStyle);
               GUI.Label(new Rect(scoreX + 180, y, 60, 25), $"{entry.score}", listStyle);
               y += 25;
            }
        }
        
        // Social Links (Bottom Right)
        if (gm.socialLinks != null && gm.socialLinks.Count > 0)
        {
            float checkSize = gm.socialLinkSize;
            float spacing = gm.socialLinkSpacing;
            int count = gm.socialLinks.Count;
            
            // Ensure animation list matches count
            if (linkAnimScales == null) linkAnimScales = new List<float>();
            while (linkAnimScales.Count < count) linkAnimScales.Add(1.0f);
            while (linkAnimScales.Count > count) linkAnimScales.RemoveAt(linkAnimScales.Count - 1);

            float margin = gm.socialLinkBottomMargin; 
            float baseLinkY = Screen.height - checkSize - margin;
            // Calculate starting X based on BASE unscaled width to keep positions stable relative to anchor
            // Note: If we want them to expand in place, we center the expansion on the button center.
            float totalBaseWidth = (count * checkSize) + ((count - 1) * spacing);
            float startX = Screen.width - totalBaseWidth - margin;

            bool isHoveringLink = false;

            for (int i = 0; i < count; i++)
            {
                if (gm.socialLinks[i].icon != null)
                {
                    // Base Unscaled center position
                    float posX = startX + (i * (checkSize + spacing));
                    Rect baseRect = new Rect(posX, baseLinkY, checkSize, checkSize);
                    
                    // Logic Update (only on Repaint to avoid multi-tick per frame issues)
                    bool isHovered = baseRect.Contains(Event.current.mousePosition);
                    if (isHovered) isHoveringLink = true;

                    if (Event.current.type == EventType.Repaint)
                    {
                        float targetScale = isHovered ? 1.15f : 1.0f;
                        // Speed: Want 0.1s for 0.15 diff -> Speed ~ 1.5 units/sec
                        float animationSpeed = 2.5f; 
                        linkAnimScales[i] = Mathf.MoveTowards(linkAnimScales[i], targetScale, Time.unscaledDeltaTime * animationSpeed);
                    }

                    // Apply Current Scale
                    float currentScale = linkAnimScales[i];
                    float newSize = checkSize * currentScale;
                    float diff = (newSize - checkSize) / 2;
                    
                    Rect drawRect = new Rect(baseRect.x - diff, baseRect.y - diff, newSize, newSize);

                    if (GUI.Button(drawRect, gm.socialLinks[i].icon, GUIStyle.none))
                    {
                        if (!string.IsNullOrEmpty(gm.socialLinks[i].url))
                        {
                            Application.OpenURL(gm.socialLinks[i].url);
                        }
                    }
                }
            }

            // Cursor Logic - STRICT CHECK
            if (isHoveringLink)
            {
               if (gm.hoverCursor != null) SafeSetCursor(gm.hoverCursor, gm.hoverCursorHotspot, CursorMode.Auto); 
            }
            else
            {
               // Not hovering link, use Normal Cursor if available, otherwise default
               if (gm.normalCursor != null) 
                   SafeSetCursor(gm.normalCursor, gm.normalCursorHotspot, CursorMode.Auto);
               else
                   SafeSetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
        // If not checking social links but in menu, we should ensuring normal cursor is set?
        // Ideally we do this once per frame or at start of DrawMenu. 
        // But since this block is at the end of DrawMenu, it acts as the final decision for the frame IF social links are active.
        // If social links are NOT active (list empty), we still want normal cursor.
    }
    
    // State tracking for animations
    private List<float> linkAnimScales;

    public void DrawOptions()
    {
        if (optionsBackground != null)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), optionsBackground, ScaleMode.ScaleAndCrop);
        }
        else
        {
            if (blackTexture == null) InitBlackTexture();
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
            GUI.color = Color.white; 
        }

        float w = 400;
        float h = 400;
        float x = (Screen.width - w) / 2;
        float y = (Screen.height - h) / 2;
        
        GUI.Box(new Rect(x, y, w, h), "OPCIONES");

        float startX = x + 20;
        float startY = y + 40;
        float gap = 60;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        labelStyle.normal.textColor = Color.white;

        // Master
        GUI.Label(new Rect(startX, startY, w, 30), "Master Volume", labelStyle);
        float newMaster = GUI.HorizontalSlider(new Rect(startX, startY + 25, w - 40, 20), masterVol, 0.0001f, 1.0f);
        if (newMaster != masterVol)
        {
            masterVol = newMaster;
            if (SoundManager.Instance != null) SoundManager.Instance.SetMasterVolume(masterVol);
        }

        // Music
        GUI.Label(new Rect(startX, startY + gap, w, 30), "Music Volume", labelStyle);
        float newMusic = GUI.HorizontalSlider(new Rect(startX, startY + gap + 25, w - 40, 20), musicVol, 0.0001f, 1.0f);
        if (newMusic != musicVol)
        {
            musicVol = newMusic;
            if (SoundManager.Instance != null) SoundManager.Instance.SetMusicVolume(musicVol);
        }

        // SFX
        GUI.Label(new Rect(startX, startY + gap*2, w, 30), "SFX Volume", labelStyle);
        float newSFX = GUI.HorizontalSlider(new Rect(startX, startY + gap*2 + 25, w - 40, 20), sfxVol, 0.0001f, 1.0f);
        if (newSFX != sfxVol)
        {
            sfxVol = newSFX;
            if (SoundManager.Instance != null) SoundManager.Instance.SetSFXVolume(sfxVol);
        }
        
        // Laser Sight Toggle
        if (GameManager.Instance != null)
        {
             bool newLaser = GUI.Toggle(new Rect(startX, startY + gap*3 + 5, w - 40, 20), GameManager.Instance.showLaserSight, " Mira Láser");
             if (newLaser != GameManager.Instance.showLaserSight) GameManager.Instance.showLaserSight = newLaser;
        }

        // Controls
        if (DrawCustomButton(new Rect(x + 100, y + h - 110, 200, 40), "CONTROLES"))
        {
            showControls = true;
        }

        // Back
        if (DrawCustomButton(new Rect(x + 100, y + h - 60, 200, 40), "VOLVER"))
        {
            showOptions = false;
        }
    }



    public void DrawControls()
    {
        if (controlsBackground != null)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), controlsBackground, ScaleMode.ScaleAndCrop);
        }
        else
        {
             if (blackTexture == null) InitBlackTexture();
             GUI.color = new Color(0, 0, 0, 0.8f); 
             GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
             GUI.color = Color.white;
        }

        float w = 500;
        float h = 450;
        float x = (Screen.width - w) / 2;
        float y = (Screen.height - h) / 2;
        
        GUI.Box(new Rect(x, y, w, h), "CONTROLES");

        float startX = x + 30;
        float startY = y + 50;
        float gap = 40;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        labelStyle.normal.textColor = Color.white;
        
        string[] controls = {
            "MOVIMIENTO: W, A, S, D / Flechas",
            "DASH: Espacio",
            "DISPARO: Click Izquierdo",
            "SELECCIÓN POWER-UP: Rueda Ratón",
            "USAR HABILIDAD: Click Derecho (Slot Seleccionado)",
            "PAUSA: ESC"
        };

        for(int i=0; i<controls.Length; i++)
        {
            GUI.Label(new Rect(startX, startY + (i * gap), w - 60, 30), controls[i], labelStyle);
        }

        // Back Button - Centered relative to 500px box (500/2 - 200/2 = 150 offset)
        if (DrawCustomButton(new Rect(x + 150, y + h - 60, 200, 40), "VOLVER"))
        {
            showControls = false;
        }
    }

    // ===================================================================================
    //                                  HUD (Former Partial)
    // ===================================================================================


    public void DrawHUD(GameManager gm)
    {
        // Note: Cursor is handled by Software Rendering at the end of this method for alignment/size fix
        // We do NOT set hardware cursor here to avoid conflicts.


        GUIStyle hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        hudStyle.normal.textColor = Color.white;
        
        // Counter Color from GM
        Color countColor = (gm != null) ? gm.uiCounterColor : Color.cyan;

        int lives = gm.Player != null ? gm.Player.CurrentHealth : 0;
        int maxHearts = 20;
        if (lives > maxHearts) lives = maxHearts; 
        
        float heartSize = 20f; 
        float startX = 20; 
        float startY = 40; // Increased padding for WebGL (960x600)
        float spacing = 5;

        for (int i = 0; i < lives; i++)
        {
            // Single row mostly, but wrap at 10
            int row = i / 10;
            int col = i % 10;
            
            float xPos = startX + (col * (heartSize + spacing));
            float yPos = startY + (row * (heartSize + spacing));
            
            Texture2D icon = (i < 3) ? lifeIcon : extraLifeIcon;
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(xPos, yPos, heartSize, heartSize), icon);
            }

        }

        // Enemy Progress Bar
        float barW = 300;
        float barH = 4; 
        float barX = (Screen.width - barW) / 2;
        float barY = 45; // Aligned with Hearts (Y=40, Size=20 -> Center ~50. 45 is good)

        // Background (Black 60%)
        Color backup = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.6f);
        if (blackTexture == null) InitBlackTexture();
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), blackTexture);
        
        // Fill (White)
        float pct = 0f;
        if (gm.TotalEnemiesThisRound > 0) pct = (float)gm.EnemiesKilledThisRound / gm.TotalEnemiesThisRound;
        if (pct > 1f) pct = 1f;
        
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(barX, barY, barW * pct, barH), Texture2D.whiteTexture);
        GUI.color = backup;

        // Fraction Text
        GUIStyle fractionStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        fractionStyle.normal.textColor = Color.white;
        // Text aligned w/ bar
        GUI.Label(new Rect(barX + barW + 10, barY - 13, 100, 30), $"{gm.EnemiesKilledThisRound}/{gm.TotalEnemiesThisRound}", fractionStyle);


        // Power-Up Slots (6 Slots at Bottom Center)
        float slotSize = 40f; 
        float slotSpacing = 5f; 
        float totalWidth = (6 * slotSize) + (5 * slotSpacing);
        float powerUpStartX = (Screen.width - totalWidth) / 2;
        float powerUpStartY = Screen.height - slotSize - 20; // 20px padding from bottom

        // Helper to check selection
        bool isSelected(int index) => gm.selectedSlotIndex == index;

        // Slot 1: Speed (Index 0)
        Rect slot1 = new Rect(powerUpStartX, powerUpStartY, slotSize, slotSize);
        string speedText = "";
        Texture2D speedTex = null;
        if (gm.Player != null && gm.Player.moveSpeed > 8.5f)
        {
             speedTex = speedIcon;
             // Calculate count. Base ~8. Each boost ~1.5 or 2? 
             // Assuming boost is defined in Config. Fallback to (speed-8)/1.5 rounded
             // Ideally access GameManager.speedBoostAmount. But let's verify if available. 
             // If not, we can guess. Let's assume +2 for now or just generic X.
             // Actually user said "x3" if 3 powerups.
             // Let's assume (Current - 8) / 1.5f (from PowerUp logic).
             float boost = (gm.speedBoostAmount > 0) ? gm.speedBoostAmount : 1.5f;
             int count = Mathf.RoundToInt((gm.Player.moveSpeed - 8.0f) / boost);
             if (count > 0) speedText = $"x{count}";
        }

        DrawPowerUpSlot(slot1, speedTex, speedText, countColor, isSelected(0));

        // Slot 2: Shield (Index 1)
        Rect slot2 = new Rect(powerUpStartX + (slotSize + slotSpacing), powerUpStartY, slotSize, slotSize);
        string shieldText = "";
        Texture2D shieldTex = null;
        if (gm.Player != null && gm.Player.ShieldCharges > 0)
        {
            shieldTex = shieldIcon;
            shieldText = $"x{gm.Player.ShieldCharges}";
        }
        DrawPowerUpSlot(slot2, shieldTex, shieldText, countColor, isSelected(1));

        // Slot 3: Time Stop (Index 2)
        Rect slot3 = new Rect(powerUpStartX + (slotSize + slotSpacing) * 2, powerUpStartY, slotSize, slotSize);

        Texture2D timeTex = null;
        Color timeColor = Color.white;

        string timeText = "";
        if (gm.HasTimeStopStored)
        {
            timeTex = timeStopIcon; 
            timeColor = Color.yellow;
            if (gm.timeStopStoredCount > 1) timeText = $"x{gm.timeStopStoredCount}";
        }
        else if (gm.IsTimeFrozen)
        {
            timeTex = timeStopIcon;
            timeColor = Color.blue;
        }
        DrawPowerUpSlot(slot3, timeTex, timeText, countColor, isSelected(2));

        // Slot 4: Ammo (Index 3)
        Rect slot4 = new Rect(powerUpStartX + (slotSize + slotSpacing) * 3, powerUpStartY, slotSize, slotSize);
        Texture2D ammoTex = null;
        if (gm.Player != null && gm.Player.HasAmmoPowerUp)
        {
             ammoTex = ammoIcon;
        }
        DrawPowerUpSlot(slot4, ammoTex, "", Color.white, isSelected(3));

        // Slot 5: Turret (Index 4)
        Rect slot5 = new Rect(powerUpStartX + (slotSize + slotSpacing) * 4, powerUpStartY, slotSize, slotSize);
        Texture2D turretTex = null;
        string turretText = "";
        
        if (gm.HasTurretStored)
        {
             turretTex = turretIcon;
             if (gm.turretStoredCount > 0) turretText = $"x{gm.turretStoredCount}";
        }
        DrawPowerUpSlot(slot5, turretTex, turretText, countColor, isSelected(4));

        // Slot 6: Bomb (Index 5)
        Rect slot6 = new Rect(powerUpStartX + (slotSize + slotSpacing) * 5, powerUpStartY, slotSize, slotSize);

        Texture2D bombTex = null;
        string bombText = "";
        
        if (gm.isSpecialAbilityAvailable)
        {
             bombTex = bombIcon;
             if (gm.bombStoredCount > 0) bombText = $"x{gm.bombStoredCount}";
        }
        DrawPowerUpSlot(slot6, bombTex, bombText, countColor, isSelected(5));
        
        // Stats (Smaller Font)
        string stats = $"Kills: {gm.Score}\nRound: {gm.Round}/{20}\nTime: {gm.GameTime:F1}s";
        GUIStyle statsStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
        statsStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width - 250, 40, 230, 100), stats, statsStyle);

        // --- SOFTWARE CURSOR FIX ---
        // User requested a "small red dot" that aligns perfectly. 
        // WebGL hardware cursors can scale incorrectly. We use a software cursor here for 100% control.
        if (gm.gameCursor != null)
        {
            Cursor.visible = false; 
            float cursorSize = 20f; // Fixed small size for precision
            Vector2 mousePos = Event.current.mousePosition;
            // Center the cursor texture on the exact mouse point
            Rect cursorRect = new Rect(mousePos.x - cursorSize / 2, mousePos.y - cursorSize / 2, cursorSize, cursorSize);
            GUI.color = Color.white;
            GUI.DrawTexture(cursorRect, gm.gameCursor);
        }
    }

    private void DrawPowerUpSlot(Rect rect, Texture2D icon, string text, Color? textColor = null, bool isSelected = false)
    {
        // 1. Background Box (Black 60% Alpha)
        Color backup = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = backup;
        
        // Selection Border
        if (isSelected)
        {
            GUI.color = Color.white;
            // Simple outline manual drawing for GUI
            float border = 2f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, border), Texture2D.whiteTexture); // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - border, rect.width, border), Texture2D.whiteTexture); // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y, border, rect.height), Texture2D.whiteTexture); // Left
            GUI.DrawTexture(new Rect(rect.x + rect.width - border, rect.y, border, rect.height), Texture2D.whiteTexture); // Right
            GUI.color = backup;
        }

        // 2. Icon (100% Opacity) with Padding
        if (icon != null)
        {
            float padding = 5f;
            Rect iconRect = new Rect(rect.x + padding, rect.y + padding, rect.width - padding*2, rect.height - padding*2);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }

        // 3. Text (Overlay)
        if (!string.IsNullOrEmpty(text))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight };
            style.normal.textColor = textColor ?? Color.white;
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 2, rect.height - 2), text, style);
        }
    }
    


    // ===================================================================================
    //                                  FEEDBACK (Former Partial)
    // ===================================================================================

    public void DrawPause(GameManager gm)
    {
        Cursor.visible = true; // Ensure visibility
        
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        headerStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 3 - 50, 400, 60), "PAUSED", headerStyle);

        float centerX = Screen.width / 2 - 100;
        float centerY = Screen.height / 2 - 40;
        float gap = 60;

        if (DrawCustomButton(new Rect(centerX, centerY, 200, 50), "RESUME"))
        {
            gm.TogglePause();
        }

        if (DrawCustomButton(new Rect(centerX, centerY + gap, 200, 50), "OPCIONES")) 
        {
            showOptions = true;
        }

        if (DrawCustomButton(new Rect(centerX, centerY + gap * 2, 200, 50), "CONTROLES"))
        {
            showControls = true;
        }

        if (DrawCustomButton(new Rect(centerX, centerY + gap * 3, 200, 50), "MENU PRINCIPAL"))
        {
            gm.GoToMenu();
        }
        
        if (DrawCustomButton(new Rect(centerX, centerY + gap * 4, 200, 50), "EXIT GAME"))
        {
            Application.Quit();
        }
    }

    public void DrawGameOver(GameManager gm)
    {
        Cursor.visible = true; // Ensure visibility

        // PARANOID SAFETY BLOCK
        try
        {
            if (blackTexture == null)
            {
                blackTexture = new Texture2D(1, 1);
                blackTexture.SetPixel(0, 0, Color.black);
                blackTexture.Apply();
            }

            // 1. Determine what we want to show
            VideoClip targetClip = gm.gameOverVideo;
            Texture2D targetTex = gm.gameOverTexture;
            string targetUrl = "";
            bool useUrl = false;

            // Legacy/Generic Fallback
            // Use ReferenceEquals to be safe against fake nulls
            if (targetClip == null && targetTex == null && !object.ReferenceEquals(gm.gameOverBackgroundAsset, null))
            {
                 if (gm.gameOverBackgroundAsset is VideoClip) targetClip = gm.gameOverBackgroundAsset as VideoClip;
                 else if (gm.gameOverBackgroundAsset is Texture2D) targetTex = gm.gameOverBackgroundAsset as Texture2D;
                 else 
                 {
                     // It's a "DefaultAsset" or unknown. 
                     if (gm.gameOverBackgroundAsset != null) 
                     {
                         string n = gm.gameOverBackgroundAsset.name;
                         if (!string.IsNullOrEmpty(n))
                         {
                             if (!n.EndsWith(".mp4")) n += ".mp4";
                             targetUrl = System.IO.Path.Combine(Application.streamingAssetsPath, n).Replace("\\", "/");
                             useUrl = true;
                         }
                     }
                 }
            }

            // 2. Setup Video Player if needed
            bool videoReady = false;
            
            if (targetClip != null || (useUrl && !string.IsNullOrEmpty(targetUrl)))
            {
                if (videoPlayer == null)
                {
                    videoPlayer = GetComponent<VideoPlayer>();
                    if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
                }
                
                // Only touch VideoPlayer if it exists
                if (videoPlayer != null)
                {
                    // Safe Property Setting
                    if (videoPlayer.renderMode != VideoRenderMode.RenderTexture) videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                    if (!videoPlayer.isLooping) videoPlayer.isLooping = true;
                    if (videoPlayer.timeReference != VideoTimeReference.Freerun) videoPlayer.timeReference = VideoTimeReference.Freerun;
                    
                    // Assign Source
                    if (useUrl)
                    {
                         if(videoPlayer.source != VideoSource.Url) videoPlayer.source = VideoSource.Url;
                    }
                    else if (targetClip != null)
                    {
                         if(videoPlayer.source != VideoSource.VideoClip) videoPlayer.source = VideoSource.VideoClip;
                    }

                    // Assign Content
                    if (useUrl && videoPlayer.url != targetUrl) 
                    {
                        videoPlayer.url = targetUrl;
                        videoPlayer.Prepare();
                    }
                    else if (targetClip != null && videoPlayer.clip != targetClip)
                    {
                        // Paranoid check for mismatch
                        if(videoPlayer.clip != targetClip)
                        {
                            videoPlayer.clip = targetClip;
                            videoPlayer.Prepare();
                        }
                    }
                    
                    // Assign Target Texture
                    if (videoTexture == null) videoTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
                    if (videoPlayer.targetTexture != videoTexture) videoPlayer.targetTexture = videoTexture;

                    // Check playback
                    if (!videoPlayer.isPlaying) videoPlayer.Play();
                    
                    videoReady = videoPlayer.isPlaying && videoTexture != null;
                }
            }
            
            // 3. Draw
            if (videoReady)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), videoTexture, ScaleMode.ScaleAndCrop);
            }
            else if (targetTex != null)
            {
                 GUI.color = Color.white;
                 GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), targetTex, ScaleMode.ScaleAndCrop);
            }
            else
            {
                 GUI.color = new Color(0, 0, 0, gm.gameOverAlpha); // Fade in black
                 GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
            }
            
            // 4. Draw UI Overlay
            Color redText = new Color(1, 0, 0, gm.gameOverAlpha);
            Color whiteText = new Color(1, 1, 1, gm.gameOverAlpha);
            
            // GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 50, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            // headerStyle.normal.textColor = redText;
            // GUI.Label(new Rect(Screen.width / 2 - 250, Screen.height / 3 - 50, 500, 80), "HAS MUERTO", headerStyle);
            
            GUIStyle scoreStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            scoreStyle.normal.textColor = whiteText;
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 3 + 40, 300, 30), $"Score: {gm.Score}", scoreStyle);

            if (gm.gameOverAlpha > 0.8f)
            {
                float btnW = 200;
                float btnH = 50;
                float centerX = Screen.width / 2 - btnW / 2;
                float startY = Screen.height / 2;
                float gap = 20;

                if (DrawCustomButton(new Rect(centerX, startY, btnW, btnH), "REINTENTAR")) gm.StartGame();
                if (DrawCustomButton(new Rect(centerX, startY + btnH + gap, btnW, btnH), "MENÚ PRINCIPAL")) gm.GoToMenu();
                if (DrawCustomButton(new Rect(centerX, startY + (btnH + gap) * 2, btnW, btnH), "SALIR")) Application.Quit();
            }

        }
        catch (System.Exception e)
        {
             // Fallback minimal UI if crash, but log it properly
             GUI.color = Color.black;
             GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), Texture2D.whiteTexture);
             GUI.Label(new Rect(10, 10, 500, 100), "Error UI: " + e.Message);
             Debug.LogError($"[DrawGameOver] CRITICAL ERROR: {e.Message}\n{e.StackTrace}");
        }
    }

    public void DrawIntro(GameManager gm)
    {
        if (gm.countdownValue > 0)
        {
            GUIStyle countStyle = new GUIStyle(GUI.skin.label) { fontSize = 100, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            countStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), gm.countdownValue.ToString("0"), countStyle);
        }

        if (gm.showGoImage && goImage != null)
        {
             float shakeX = Random.Range(-5f, 5f);
             float shakeY = Random.Range(-5f, 5f);
             
             Color backup = GUI.color;
             GUI.color = new Color(1, 1, 1, gm.goImageAlpha);
             
             float size = Screen.height * 0.9f; 
             float x = (Screen.width - size) / 2 + shakeX;
             float y = (Screen.height - size) / 2 + shakeY;
             
             GUI.DrawTexture(new Rect(x, y, size, size), goImage, ScaleMode.ScaleToFit);
             
             GUI.color = backup;
        }
    }
}
