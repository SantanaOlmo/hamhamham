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
            DrawGameOver(gm);
        }
    }

    // ===================================================================================
    //                                  SHARED HELPERS
    // ===================================================================================

    public void PrepareVideo(VideoClip clip)
    {
        if (clip == null) return;
        
        if (videoPlayer == null) 
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
            
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        }

        // Setup Texture if needed
        if (videoTexture == null || videoTexture.width != Screen.width || videoTexture.height != Screen.height)
        {
            if (videoTexture != null) videoTexture.Release();
            videoTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
            videoTexture.Create();
        }

        if (videoPlayer.clip != clip)
        {
            videoPlayer.clip = clip;
            videoPlayer.targetTexture = videoTexture;
            videoPlayer.Play();
        }
        else if (!videoPlayer.isPlaying)
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

    // ===================================================================================
    //                                  MENUS (Former Partial)
    // ===================================================================================

    public void DrawMenu()
    {
        GameManager gm = GameManager.Instance;
        
        // VIDEO BACKGROUND HANDLING
        if (gm.menuVideo != null)
        {
            PrepareVideo(gm.menuVideo);
            if (videoTexture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), videoTexture, ScaleMode.ScaleAndCrop);
            }
        }
        else if (menuBackground != null)
        {
            // Fallback Static Image
            StopVideo();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), menuBackground, ScaleMode.ScaleAndCrop);
        }
        else
        {
            StopVideo();
        }

        float targetHeight = 300f; 
        float maxTitleWidth = 1200f;
        float buttonsCenterY = Screen.height / 2 + 100; 
        float centerX = Screen.width / 2 - 100;

        if (titleTexture != null)
        {
            float aspect = (float)titleTexture.width / titleTexture.height;
            float height = targetHeight;
            float width = height * aspect;
            
            if (width > maxTitleWidth)
            {
                width = maxTitleWidth;
                height = width / aspect;
            }

            float titleY = buttonsCenterY - height - 50; 
            GUI.DrawTexture(new Rect((Screen.width - width) / 2, titleY, width, height), titleTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            headerStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 4, 300, 50), "ILERNA TOPDOWN", headerStyle);
        }

        // Name Input
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
        labelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(centerX, buttonsCenterY - 90, 200, 25), "PILOT NAME:", labelStyle);
        
        gm.playerName = GUI.TextField(new Rect(centerX, buttonsCenterY - 60, 200, 30), gm.playerName, 15);

        // Buttons
        if (GUI.Button(new Rect(centerX, buttonsCenterY, 200, 50), "PLAY GAME"))
        {
            gm.StartGame();
        }

        if (GUI.Button(new Rect(centerX, buttonsCenterY + 60, 200, 50), "OPCIONES"))
        {
            showOptions = true;
        }

        if (GUI.Button(new Rect(centerX, buttonsCenterY + 120, 200, 50), "EXIT"))
        {
            Application.Quit();
        }
        
        // High Scores (Top Right)
        float scoreW = 250;
        float scoreX = Screen.width - scoreW - 50;
        float scoreY = 50;
        
        GUI.Box(new Rect(scoreX, scoreY, scoreW, 400), "TOP SCORERS");
        
        if (gm.highScoreTable != null)
        {
            float y = scoreY + 30;
            GUIStyle listStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            listStyle.normal.textColor = Color.white;

            for(int i = 0; i < gm.highScoreTable.entries.Count; i++)
            {
               var entry = gm.highScoreTable.entries[i];
               GUI.Label(new Rect(scoreX + 20, y, 150, 25), $"{i+1}. {entry.name}", listStyle);
               GUI.Label(new Rect(scoreX + 180, y, 60, 25), $"{entry.score}", listStyle);
               y += 25;
            }
        }
    }

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

        // Controls
        if (GUI.Button(new Rect(x + 100, y + h - 110, 200, 40), "CONTROLES"))
        {
            showControls = true;
        }

        // Back
        if (GUI.Button(new Rect(x + 100, y + h - 60, 200, 40), "VOLVER"))
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
        if (GUI.Button(new Rect(x + 150, y + h - 60, 200, 40), "VOLVER"))
        {
            showControls = false;
        }
    }

    // ===================================================================================
    //                                  HUD (Former Partial)
    // ===================================================================================

    public void DrawHUD(GameManager gm)
    {
        GUIStyle hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        hudStyle.normal.textColor = Color.white;
        
        // Counter Color from GM
        Color countColor = (gm != null) ? gm.uiCounterColor : Color.cyan;

        int lives = gm.Player != null ? gm.Player.CurrentHealth : 0;
        int maxHearts = 20;
        if (lives > maxHearts) lives = maxHearts; 
        
        float heartSize = 20f; 
        float startX = 20; 
        float startY = 20; // Same top padding as stats
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

        // Enemy Progress Bar (Thinner: 20% of 20 = 4)
        float barW = 300;
        float barH = 4; // Requested ~20% of previous size
        float barX = (Screen.width - barW) / 2;
        float barY = 20;

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
        // center text vertically relative to bar center
        // Bar Center Y = barY + barH/2 = 20 + 2 = 22
        // Label Height = 30. Center = Y + 15.
        // 22 = Y + 15 => Y = 7.
        // barY = 20. So Y = barY - 13.
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
        GUI.Label(new Rect(Screen.width - 250, 20, 230, 100), stats, statsStyle);
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
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        headerStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 3 - 50, 400, 60), "PAUSED", headerStyle);

        float centerX = Screen.width / 2 - 100;
        float centerY = Screen.height / 2 - 40;
        float gap = 60;

        if (GUI.Button(new Rect(centerX, centerY, 200, 50), "RESUME"))
        {
            gm.TogglePause();
        }

        if (GUI.Button(new Rect(centerX, centerY + gap, 200, 50), "OPCIONES")) 
        {
            showOptions = true;
        }

        if (GUI.Button(new Rect(centerX, centerY + gap * 2, 200, 50), "CONTROLES"))
        {
            showControls = true;
        }

        if (GUI.Button(new Rect(centerX, centerY + gap * 3, 200, 50), "MENU PRINCIPAL"))
        {
            gm.GoToMenu();
        }
        
        if (GUI.Button(new Rect(centerX, centerY + gap * 4, 200, 50), "EXIT GAME"))
        {
            Application.Quit();
        }
    }

    public void DrawGameOver(GameManager gm)
    {
        if (blackTexture == null) InitBlackTexture();
        
        Color backup = GUI.color;
        
        // VIDEO GAME OVER HANDLING
        if (gm.gameOverVideo != null)
        {
             // Use Video
             PrepareVideo(gm.gameOverVideo);
             if (videoTexture != null)
             {
                 GUI.color = new Color(1, 1, 1, gm.gameOverAlpha);
                 GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), videoTexture, ScaleMode.ScaleAndCrop);
             }
        }
        else if (gameOverBackground != null)
        {
             // Image Fallback
             StopVideo();
             GUI.color = new Color(1, 1, 1, gm.gameOverAlpha);
             GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), gameOverBackground, ScaleMode.ScaleAndCrop);
        }
        else
        {
             StopVideo();
             GUI.color = new Color(0, 0, 0, gm.gameOverAlpha);
             GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
        }
        
        GUI.color = backup;

        Color redText = Color.red;
        redText.a = gm.gameOverAlpha;
        
        Color whiteText = Color.white;
        whiteText.a = gm.gameOverAlpha;

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 50, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        headerStyle.normal.textColor = redText;
        GUI.Label(new Rect(Screen.width / 2 - 250, Screen.height / 3 - 50, 500, 80), "HAS MUERTO", headerStyle);
        
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

            if (GUI.Button(new Rect(centerX, startY, btnW, btnH), "REINTENTAR"))
            {
                gm.StartGame();
            }

            if (GUI.Button(new Rect(centerX, startY + btnH + gap, btnW, btnH), "MENÚ PRINCIPAL"))
            {
                gm.GoToMenu();
            }
            
            if (GUI.Button(new Rect(centerX, startY + (btnH + gap) * 2, btnW, btnH), "SALIR"))
            {
                Application.Quit();
            }
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
