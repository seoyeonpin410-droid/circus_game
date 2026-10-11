using System;
using System.IO;
using System.Linq;
using CircusPrototype;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class CircusPrototypeBuilder
{
    const string Root = "Assets/Prototype";
    public const string ScenePath = Root + "/Scenes/CircusPrototype.unity";
    static TMP_FontAsset font;
    static Material red, gold, dark, green, pale, black;
    static readonly Color Ink = new Color(.055f, .045f, .065f);
    static readonly Color Cream = new Color(.95f, .88f, .72f);
    static readonly Color Gold = new Color(.82f, .64f, .32f);
    static readonly Color Red = new Color(.56f, .1f, .16f);

    [MenuItem("Circus/Build Prototype Scene")]
    public static string Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (File.Exists(ScenePath)) throw new InvalidOperationException("Prototype scene already exists; edit it in place.");
        foreach (string folder in new[] { "Materials", "Prefabs", "Scenes", "Audio" })
            Directory.CreateDirectory(Root + "/" + folder);
        AssetDatabase.Refresh();
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/NanumGothic-Regular.ttf");
        if (!sourceFont) throw new InvalidOperationException("Missing Korean font");
        font = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 9,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        font.name = "Circus Korean SDF";
        AssetDatabase.CreateAsset(font, Root + "/Fonts/Circus Korean SDF.asset");
        AssetDatabase.AddObjectToAsset(font.material, font);
        AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
        red = Mat("Velvet", Red); gold = Mat("Brass", Gold);
        dark = Mat("Arena", new Color(.16f, .12f, .14f));
        green = Mat("Rang Skin", new Color(.42f, .55f, .4f));
        pale = Mat("Ivory", Cream); black = Mat("Charcoal", Ink);

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game Scenes.unity");
        // Retain the artist's original scene; only the copy is changed.
        EditorSceneManager.SaveScene(scene, ScenePath, true);
        scene = EditorSceneManager.OpenScene(ScenePath);
        var building = GameObject.Find("건물");
        if (building)
        {
            building.transform.localScale *= .07f;
            var bounds = BoundsOf(building);
            building.transform.position += new Vector3(-bounds.center.x, -.7f - bounds.min.y, -bounds.center.z);
            foreach (var r in building.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        }
        var plane = GameObject.Find("Plane");
        plane.transform.position = new Vector3(0, -.35f, 0);
        plane.transform.localScale = Vector3.one * 6;
        plane.GetComponent<Renderer>().sharedMaterial = black;
        var arena = Primitive("Circus Ring", PrimitiveType.Cylinder, null, new Vector3(0, -.12f, 0), new Vector3(28, .12f, 28), dark);
        UnityEngine.Object.DestroyImmediate(arena.GetComponent<Collider>());
        arena.AddComponent<MeshCollider>().sharedMesh=arena.GetComponent<MeshFilter>().sharedMesh;
        var decor = new GameObject("Stage Dressing").transform;
        Circle("Outer Brass Ring", decor, 13.7f, .04f, gold, .10f);
        Circle("Inner Ring", decor, 5, .045f, gold, .04f);
        Circle("Center Medallion", decor, 1.9f, .055f, red, .13f);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6;
            var position = new Vector3(Mathf.Sin(angle) * 14.4f, 1.1f, Mathf.Cos(angle) * 14.4f);
            var pedestal = Primitive("Footlight " + i, PrimitiveType.Cube, decor, position, new Vector3(.5f, 2.2f, .5f), i % 2 == 0 ? red : pale);
            var lamp = new GameObject("Warm Footlight"); lamp.transform.SetParent(pedestal.transform, true);
            lamp.transform.position = position + Vector3.up * .6f;
            var light = lamp.AddComponent<Light>(); light.type = LightType.Point;
            light.color = new Color(1, .62f, .28f); light.intensity = 2; light.range = 5;
        }
        // Invisible stage boundaries keep movement on the presentation arena.
        for (int i = 0; i < 32; i++)
        {
            float angle = i * Mathf.PI * 2 / 32;
            var wall = new GameObject("Stage Boundary " + i);
            wall.transform.SetParent(decor);
            wall.transform.position = new Vector3(Mathf.Sin(angle) * 14, 2, Mathf.Cos(angle) * 14);
            wall.transform.rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
            var collider = wall.AddComponent<BoxCollider>(); collider.size = new Vector3(3.1f, 5, .4f);
        }
        var lightMain = GameObject.Find("Directional Light").GetComponent<Light>();
        lightMain.color = new Color(1, .84f, .68f); lightMain.intensity = 1.6f;
        lightMain.transform.rotation = Quaternion.Euler(52, -35, 0);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.42f, .36f, .4f);
        RenderSettings.fog = false;
        var volume = GameObject.Find("Global Volume"); if (volume) volume.SetActive(false);

        var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
        camera.transform.SetParent(null, true);
        camera.transform.position = new Vector3(0, 17, -14);
        camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true; camera.orthographicSize = 11.5f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Ink;
        var follow = camera.gameObject.AddComponent<CircusCamera>();
        var existingPlayer = GameObject.Find("Player");
        if (!existingPlayer) throw new InvalidOperationException("Artist player instance missing.");
        var performer = new GameObject("Muin Player");
        performer.layer = 8;
        performer.transform.position = new Vector3(0, .05f, -2);
        existingPlayer.transform.SetParent(performer.transform, false);
        existingPlayer.transform.localPosition = Vector3.zero;
        existingPlayer.name = "Artist Character";
        var renderBounds = BoundsOf(existingPlayer);
        if (renderBounds.size.y > .001f) existingPlayer.transform.localScale *= 1.8f / renderBounds.size.y;
        renderBounds = BoundsOf(existingPlayer);
        existingPlayer.transform.position += Vector3.up * (performer.transform.position.y - renderBounds.min.y);
        foreach (var collider in existingPlayer.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        var controller = performer.AddComponent<CharacterController>();
        controller.height = 1.8f; controller.radius = .4f; controller.center = Vector3.up * .9f;
        controller.stepOffset = .2f; controller.skinWidth = .035f;
        var player = performer.AddComponent<CircusPlayer>();
        var bell = Model("Assets/모델링/메인 캐릭터 모델링/캐릭터 무기/방울/방울.obj", "Artist Bell", existingPlayer.transform, .7f);
        bell.transform.localPosition = new Vector3(.55f, .55f, .2f);
        Palette(bell, "Assets/모델링/메인 캐릭터 모델링/캐릭터 무기/방울/방울.png", "Bell Palette");
        var wave = Line("Bell Wave", null, gold, .08f, 25);
        wave.enabled = false;
        Set(player, "visual", existingPlayer.transform); Set(player, "bell", bell.transform);
        Set(player, "aimCamera", camera); Set(player, "enemyMask", 1 << 9); Set(player, "bellWave", wave);
        Set(follow, "target", performer.transform);

        var projectileObject = Primitive("Rang Projectile", PrimitiveType.Sphere, null, Vector3.zero, Vector3.one * .32f, green);
        UnityEngine.Object.DestroyImmediate(projectileObject.GetComponent<Collider>());
        var projectile = projectileObject.AddComponent<CircusProjectile>();
        var projectilePrefab = PrefabUtility.SaveAsPrefabAsset(projectileObject, Root + "/Prefabs/RangProjectile.prefab").GetComponent<CircusProjectile>();
        UnityEngine.Object.DestroyImmediate(projectileObject);
        var closePrefab = CreateEnemy(false, projectilePrefab);
        var rangedPrefab = CreateEnemy(true, projectilePrefab);
        var session = new GameObject("Show Director").AddComponent<CircusSession>();
        Set(session, "player", player); Set(session, "closeEnemy", closePrefab); Set(session, "rangedEnemy", rangedPrefab);
        var spawners = new GameObject("Rang Spawners").transform;
        var spawnPoints = new Transform[10];
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            float angle = i * Mathf.PI * 2 / spawnPoints.Length;
            var spawn = new GameObject("Spawner " + (i + 1)); spawn.transform.SetParent(spawners);
            spawn.transform.position = new Vector3(Mathf.Sin(angle) * 9.5f, .06f, Mathf.Cos(angle) * 9.5f);
            spawnPoints[i] = spawn.transform;
            Circle("Spawn Medallion", spawn.transform, .65f, .015f, red, .04f);
        }
        SetArray(session, "spawnPoints", spawnPoints);
        var hud = CreateHud(); Set(session, "hud", hud);
        var layerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        layerSettings.FindProperty("layers").GetArrayElementAtIndex(8).stringValue = "Muin";
        layerSettings.FindProperty("layers").GetArrayElementAtIndex(9).stringValue = "Rang";
        layerSettings.ApplyModifiedPropertiesWithoutUndo();
        PlayerSettings.productName = "곡예 · Circus Prototype";
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = performer;
        SceneView.lastActiveSceneView?.Frame(new Bounds(Vector3.zero, new Vector3(30, 6, 30)), false);
        return "Prototype scene created, original art scene preserved.";
    }

    static CircusEnemy CreateEnemy(bool ranged, CircusProjectile projectile)
    {
        var root = new GameObject(ranged ? "Ranged Rang" : "Close Rang"); root.layer = 9;
        var model = new GameObject("Temporary Voxel Model").transform; model.SetParent(root.transform);
        Primitive("Coat", PrimitiveType.Cube, model, new Vector3(0, .8f, 0), new Vector3(.72f, .85f, .42f), ranged ? black : red);
        Primitive("Head", PrimitiveType.Cube, model, new Vector3(0, 1.5f, 0), Vector3.one * .52f, green);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Primitive("Arm", PrimitiveType.Cube, model, new Vector3(sign * .5f, .85f, .25f), new Vector3(.22f, .65f, .22f), green);
            Primitive("Leg", PrimitiveType.Cube, model, new Vector3(sign * .23f, .25f, 0), new Vector3(.24f, .5f, .32f), black);
            Primitive("Eye", PrimitiveType.Cube, model, new Vector3(sign * .14f, 1.56f, .269f), new Vector3(.1f, .08f, .035f), pale);
        }
        if (ranged)
        {
            Primitive("Hat Brim", PrimitiveType.Cube, model, new Vector3(0, 1.79f, 0), new Vector3(.75f, .08f, .65f), red);
            Primitive("Top Hat", PrimitiveType.Cube, model, new Vector3(0, 1.96f, 0), new Vector3(.45f, .3f, .4f), red);
        }
        foreach (var collider in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 9;
        var body = root.AddComponent<BoxCollider>(); body.center = Vector3.up; body.size = new Vector3(1, 2.2f, .7f);
        var enemy = root.AddComponent<CircusEnemy>();
        Set(enemy, "ranged", ranged); Set(enemy, "model", model); Set(enemy, "projectile", projectile);
        var warning = Line("Attack Telegraph", root.transform, ranged ? green : red, .04f, 2);
        warning.enabled = false; Set(enemy, "warning", warning);
        var canvas = new GameObject("Health", typeof(RectTransform), typeof(Canvas));
        canvas.transform.SetParent(root.transform); var ui = canvas.GetComponent<Canvas>(); ui.renderMode = RenderMode.WorldSpace;
        canvas.transform.localPosition = new Vector3(0, 2.35f, 0);
        canvas.transform.rotation = Quaternion.Euler(50, 0, 0); canvas.transform.localScale = Vector3.one * .008f;
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 12);
        ImageBox("Background", canvas.transform, Vector2.zero, new Vector2(100, 10), Ink);
        var fill = ImageBox("Fill", canvas.transform, Vector2.zero, new Vector2(96, 6), ranged ? new Color(.4f,.7f,.5f) : Red);
        fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        Set(enemy, "healthFill", fill);
        var result = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + (ranged ? "RangedRang" : "CloseRang") + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);
        return result.GetComponent<CircusEnemy>();
    }

    static CircusHud CreateHud()
    {
        var canvas = new GameObject("Circus UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var hud = canvas.AddComponent<CircusHud>();
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var menu = Panel("Main Menu", canvas.transform);
        ImageBox("Menu Scrim", menu, new Vector2(-490, 0), new Vector2(940, 1080), new Color(.045f,.03f,.045f,.96f));
        ImageBox("Velvet Edge", menu, new Vector2(-32, 0), new Vector2(16, 1080), Red);
        ImageBox("Brass Edge", menu, new Vector2(-20, 0), new Vector2(2, 1080), Gold);
        Label("DOKKAEBI CIRCUS  /  20 YEARS AFTER", menu, new Vector2(-540, 404), new Vector2(670, 55), 23, Gold);
        Label("곡예", menu, new Vector2(-535, 240), new Vector2(680, 240), 168, Cream);
        Label("도깨비 굴에 오신 것을 환영합니다", menu, new Vector2(-530, 82), new Vector2(680, 55), 31, Cream);
        Label("무인으로 무대에 올라, 랑을 소탕하세요.\n방울이 울리는 동안 공연은 계속됩니다.", menu, new Vector2(-530, -10), new Vector2(680, 95), 25, new Color(.68f,.63f,.58f));
        var start = Button("공연 시작", menu, new Vector2(-530, -155), new Vector2(640, 88), Red);
        var settings = Button("설정", menu, new Vector2(-700, -268), new Vector2(300, 68), new Color(.16f,.12f,.14f));
        var quit = Button("나가기", menu, new Vector2(-360, -268), new Vector2(300, 68), new Color(.16f,.12f,.14f));
        Label("W A S D 이동   ·   SPACE 점프\n마우스 조준   ·   좌클릭 방울 공격   ·   ESC 일시정지", menu, new Vector2(-530, -410), new Vector2(700, 115), 23, Gold);
        Label("무인 / MUIN", menu, new Vector2(520, -320), new Vector2(470, 60), 35, Cream);
        Label("두 개의 막, 하나의 무대", menu, new Vector2(520, -378), new Vector2(470, 45), 23, Gold);
        Set(hud, "menu", menu.gameObject); Set(hud,"startButton",start); Set(hud,"settingsButton",settings); Set(hud,"quitButton",quit);

        var gameplay = Panel("Show HUD", canvas.transform);
        var hpPanel = ImageBox("HP Panel", gameplay, new Vector2(-654, 438), new Vector2(550, 152), new Color(.055f,.045f,.065f,.92f));
        Label("무인 / MUIN", hpPanel.transform, new Vector2(-180, 44), new Vector2(180, 38), 22, Gold);
        var hpText = Label("체력 100 / 100", hpPanel.transform, new Vector2(55, 44), new Vector2(275,38), 23, Cream);
        ImageBox("HP Track", hpPanel.transform, new Vector2(0,-5), new Vector2(485,22), new Color(.2f,.14f,.17f));
        var hpFill = ImageBox("HP Fill", hpPanel.transform, new Vector2(0,-5), new Vector2(481,18), Red);
        hpFill.type = UnityEngine.UI.Image.Type.Filled; hpFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        Label("체력 10% 미만 · 감염", hpPanel.transform, new Vector2(0,-48), new Vector2(485,30), 18, new Color(.64f,.61f,.58f));
        var roundPanel = ImageBox("Round Panel", gameplay, new Vector2(0,444), new Vector2(620,140), new Color(.055f,.045f,.065f,.92f));
        var roundText = Label("제1막 / 2", roundPanel.transform, new Vector2(0,36), new Vector2(300,45), 32, Cream);
        var remaining = Label("남은 랑 08", roundPanel.transform, new Vector2(0,-26), new Vector2(590,45), 24, Gold);
        var score = Label("점수 0000", gameplay, new Vector2(632,466), new Vector2(325,50), 29, Cream);
        var pauseButton = Button("Ⅱ", gameplay, new Vector2(862,468), new Vector2(68,68), Red);
        var legend = ImageBox("Enemy Legend", gameplay, new Vector2(-685,-450), new Vector2(492,105), new Color(.055f,.045f,.065f,.92f));
        ImageBox("Close Icon", legend.transform, new Vector2(-199,10), new Vector2(26,26), Red);
        Label("근거리 랑",legend.transform,new Vector2(-80,10),new Vector2(190,36),22,Cream);
        var rangedLegend = Panel("Ranged Legend",legend.transform);
        ImageBox("Ranged Icon",rangedLegend,new Vector2(45,10),new Vector2(26,26),new Color(.4f,.7f,.5f));
        Label("원거리 랑",rangedLegend,new Vector2(153,10),new Vector2(175,36),22,Cream);
        Label("고정형 적 · 공격 예고선을 피하세요",legend.transform,new Vector2(0,-29),new Vector2(440,30),18,Gold);
        Label("WASD 이동   SPACE 점프   좌클릭 공격   ESC 정지",gameplay,new Vector2(367,-476),new Vector2(1090,40),23,Cream);
        var message = Label("", gameplay, new Vector2(0,290), new Vector2(1200,75), 35, Gold);
        Set(hud,"gameplay",gameplay.gameObject); Set(hud,"healthText",hpText); Set(hud,"healthFill",hpFill);
        Set(hud,"roundText",roundText); Set(hud,"remainingText",remaining); Set(hud,"scoreText",score);
        Set(hud,"rangedLegend",rangedLegend.gameObject); Set(hud,"pauseButton",pauseButton); Set(hud,"message",message);

        var pause = Overlay("Pause",canvas.transform);
        Label("막간",pause,new Vector2(0,140),new Vector2(700,100),70,Cream);
        Label("공연이 잠시 멈췄습니다",pause,new Vector2(0,55),new Vector2(800,50),26,Gold);
        var resume = Button("계속하기",pause,new Vector2(0,-55),new Vector2(550,82),Red);
        var pauseMenu = Button("메인 화면",pause,new Vector2(0,-163),new Vector2(550,70),new Color(.16f,.12f,.14f));
        Set(hud,"pause",pause.gameObject); Set(hud,"resumeButton",resume); Set(hud,"pauseMenuButton",pauseMenu);
        var ending = Overlay("Result",canvas.transform);
        var endingTitle = Label("공연 종료",ending,new Vector2(0,150),new Vector2(1300,110),65,Cream);
        var endingBody = Label("",ending,new Vector2(0,30),new Vector2(1000,120),29,Gold);
        var retry = Button("다시 공연",ending,new Vector2(-210,-140),new Vector2(380,78),Red);
        var endingMenu = Button("메인 화면",ending,new Vector2(210,-140),new Vector2(380,78),new Color(.16f,.12f,.14f));
        Set(hud,"ending",ending.gameObject); Set(hud,"endingTitle",endingTitle); Set(hud,"endingBody",endingBody);
        Set(hud,"retryButton",retry); Set(hud,"endingMenuButton",endingMenu);
        var options = Overlay("Settings",canvas.transform);
        Label("설정",options,new Vector2(0,220),new Vector2(800,100),65,Cream);
        Label("전체 음량",options,new Vector2(-245,74),new Vector2(260,50),29,Cream);
        var slider = Slider(options,new Vector2(125,74));
        var toggleObject = new GameObject("Fullscreen",typeof(RectTransform),typeof(UnityEngine.UI.Toggle));
        toggleObject.transform.SetParent(options,false);
        Rect(toggleObject.GetComponent<RectTransform>(),new Vector2(-235,-30),new Vector2(290,60));
        var box = ImageBox("Box",toggleObject.transform,new Vector2(-105,0),new Vector2(35,35),new Color(.3f,.24f,.22f)); box.raycastTarget = true;
        var check = ImageBox("Check",box.transform,Vector2.zero,new Vector2(23,23),Gold);
        var toggle = toggleObject.GetComponent<UnityEngine.UI.Toggle>(); toggle.targetGraphic = box; toggle.graphic = check;
        Label("전체 화면",toggleObject.transform,new Vector2(38,0),new Vector2(230,50),29,Cream);
        Label("두 막을 모두 마치면 공연이 종료됩니다.\n체력 10% 미만에서 감염 상태가 됩니다.",options,new Vector2(0,-140),new Vector2(1000,100),24,Gold);
        var close = Button("돌아가기",options,new Vector2(0,-268),new Vector2(500,78),Red);
        Set(hud,"settings",options.gameObject); Set(hud,"volumeSlider",slider); Set(hud,"fullscreenToggle",toggle); Set(hud,"settingsCloseButton",close);
        gameplay.gameObject.SetActive(false); pause.gameObject.SetActive(false); ending.gameObject.SetActive(false); options.gameObject.SetActive(false);
        return hud;
    }

    static Material Mat(string name, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.name = name;
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", .15f);
        AssetDatabase.CreateAsset(mat, Root + "/Materials/" + name + ".mat"); return mat;
    }
    static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent,false);
        obj.transform.localPosition = position; obj.transform.localScale = scale; obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
    }
    static Bounds BoundsOf(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds;
        foreach(var r in renderers) bounds.Encapsulate(r.bounds); return bounds;
    }
    static GameObject Model(string path, string name, Transform parent, float height)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        obj.name = name; obj.transform.SetParent(parent,false); obj.transform.localPosition = Vector3.zero;
        var b = BoundsOf(obj); obj.transform.localScale *= height / b.size.y; return obj;
    }
    static void Palette(GameObject obj,string texturePath,string name)
    {
        var mat = Mat(name,Color.white); mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        foreach(var r in obj.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
    }
    static LineRenderer Line(string name,Transform parent,Material mat,float width,int count)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent,false);
        line.sharedMaterial = mat; line.startWidth = line.endWidth = width; line.positionCount = count;
        line.useWorldSpace = true; line.shadowCastingMode = ShadowCastingMode.Off; return line;
    }
    static void Circle(string name,Transform parent,float radius,float y,Material mat,float width)
    {
        var line = Line(name,parent,mat,width,80); line.useWorldSpace = false; line.loop = true;
        for(int i=0;i<80;i++) { float a=i*Mathf.PI*2/80; line.SetPosition(i,new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius)); }
    }
    static RectTransform Panel(string name,Transform parent)
    {
        var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    static RectTransform Overlay(string name,Transform parent)
    {
        var panel = Panel(name,parent); var image = panel.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.04f,.025f,.04f,.94f); image.raycastTarget = true;
        ImageBox("Upper Rule",panel,new Vector2(0,350),new Vector2(1150,2),Gold);
        ImageBox("Lower Rule",panel,new Vector2(0,-370),new Vector2(1150,2),Gold); return panel;
    }
    static void Rect(RectTransform rect,Vector2 pos,Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f); rect.pivot = new Vector2(.5f,.5f); rect.anchoredPosition = pos; rect.sizeDelta = size;
    }
    static UnityEngine.UI.Image ImageBox(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
    {
        var image = new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        image.transform.SetParent(parent,false); Rect(image.rectTransform,pos,size); image.color = color; image.raycastTarget = false;
        // Filled UI needs a real sprite to render fillAmount correctly.
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); return image;
    }
    static TextMeshProUGUI Label(string text,Transform parent,Vector2 pos,Vector2 size,float fontSize,Color color)
    {
        var label = new GameObject(text.Length > 24 ? text.Substring(0,24) : text,typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent,false); Rect(label.rectTransform,pos,size); label.font = font; label.fontSize = fontSize;
        label.text = text; label.color = color; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal; return label;
    }
    static UnityEngine.UI.Button Button(string text,Transform parent,Vector2 pos,Vector2 size,Color color)
    {
        var image = ImageBox(text,parent,pos,size,color); image.raycastTarget = true;
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(1.15f,1.12f,1.08f); colors.pressedColor = new Color(.7f,.7f,.7f); button.colors = colors;
        Label(text,image.transform,Vector2.zero,size - new Vector2(20,8),30,Cream); return button;
    }
    static UnityEngine.UI.Slider Slider(Transform parent,Vector2 pos)
    {
        var root = new GameObject("Volume Slider",typeof(RectTransform),typeof(UnityEngine.UI.Slider)); root.transform.SetParent(parent,false);
        Rect(root.GetComponent<RectTransform>(),pos,new Vector2(500,44));
        var track = ImageBox("Track",root.transform,Vector2.zero,new Vector2(500,10),new Color(.3f,.24f,.22f)); track.raycastTarget = true;
        var fill = ImageBox("Fill",root.transform,Vector2.zero,new Vector2(500,10),Gold);
        var handle = ImageBox("Handle",root.transform,Vector2.zero,new Vector2(24,38),Cream); handle.raycastTarget = true;
        var slider = root.GetComponent<UnityEngine.UI.Slider>(); slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle; return slider;
    }
    static void Set(UnityEngine.Object target,string property,UnityEngine.Object value)
    { var so = new SerializedObject(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Set(UnityEngine.Object target,string property,bool value)
    { var so = new SerializedObject(target); so.FindProperty(property).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Set(UnityEngine.Object target,string property,int value)
    { var so = new SerializedObject(target); so.FindProperty(property).intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetArray(UnityEngine.Object target,string property,Transform[] values)
    { var so = new SerializedObject(target); var array = so.FindProperty(property); array.arraySize=values.Length; for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }

    [MenuItem("Circus/Polish Prototype Presentation")]
    public static string Polish()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var texture = new Texture2D(16,16);
        texture.SetPixels(Enumerable.Repeat(Color.white,256).ToArray()); texture.Apply();
        string whitePath = Root + "/Materials/UI White.png";
        File.WriteAllBytes(whitePath,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(whitePath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(whitePath); importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single; importer.filterMode=FilterMode.Bilinear; importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(whitePath);
        foreach(var image in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            image.sprite=sprite;
        foreach(var path in new[]{Root+"/Prefabs/CloseRang.prefab",Root+"/Prefabs/RangedRang.prefab"})
        {
            var prefab=PrefabUtility.LoadPrefabContents(path);
            foreach(var image in prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true))image.sprite=sprite;
            PrefabUtility.SaveAsPrefabAsset(prefab,path); PrefabUtility.UnloadPrefabContents(prefab);
        }
        var mainMenu=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Main Menu");
        var muinLabel=mainMenu.GetComponentsInChildren<TextMeshProUGUI>().First(t=>t.text.StartsWith("무인 "));
        muinLabel.text="무인 / MUIN";
        // Use the same supplied character and bell as a full-body menu showcase.
        var original=GameObject.Find("Artist Character");
        var preview=UnityEngine.Object.Instantiate(original); preview.name="Menu Character Showcase";
        foreach(var childCamera in preview.GetComponentsInChildren<Camera>()) UnityEngine.Object.DestroyImmediate(childCamera.gameObject);
        preview.transform.SetParent(null); preview.transform.position=new Vector3(60,0,0);
        preview.transform.rotation=Quaternion.Euler(0,180,0);
        foreach(var t in preview.GetComponentsInChildren<Transform>())t.gameObject.layer=10;
        var b=BoundsOf(preview); preview.transform.position+=new Vector3(60-b.center.x,-b.min.y,-b.center.z);
        var renderTexture=new RenderTexture(1024,1024,24); renderTexture.name="Menu Character Preview";
        AssetDatabase.CreateAsset(renderTexture,Root+"/Materials/Menu Character Preview.renderTexture");
        var previewCamera=new GameObject("Menu Preview Camera").AddComponent<Camera>();
        previewCamera.clearFlags=CameraClearFlags.SolidColor; previewCamera.backgroundColor=new Color(0,0,0,0);
        previewCamera.cullingMask=1<<10; previewCamera.targetTexture=renderTexture;
        previewCamera.orthographic=true; previewCamera.orthographicSize=1.35f;
        previewCamera.transform.position=new Vector3(60,1.4f,5); previewCamera.transform.LookAt(new Vector3(60,1,0));
        Camera.main.cullingMask &= ~(1<<10);
        var raw=new GameObject("Muin Portrait",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
        raw.transform.SetParent(mainMenu,false); Rect(raw.rectTransform,new Vector2(500,90),new Vector2(670,670));
        raw.texture=renderTexture; raw.raycastTarget=false; raw.transform.SetAsFirstSibling();
        var source=GameObject.Find("Muin Player").AddComponent<AudioSource>(); source.playOnAwake=false; source.volume=.6f;
        Set(GameObject.Find("Muin Player").GetComponent<CircusPlayer>(),"bellAudio",source);
        // Avoid enlarging a small Game view capture: use a fixed render resolution.
        var gameViewType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var gameView=EditorWindow.GetWindow(gameViewType);
        var sizesType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
        var single=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=single.GetProperty("instance").GetValue(null);
        var groupMethod=sizesType.GetMethod("GetGroup");
        var group=groupMethod.Invoke(sizes,new object[]{Enum.ToObject(groupMethod.GetParameters()[0].ParameterType,0)});
        var sizeType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
        var kind=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType");
        var fixedSize=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(kind,1),1600,900,"Circus 1600x900"});
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{fixedSize});
        int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        gameViewType.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(gameView,count-1);
        gameView.Show(); gameView.Focus();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        return "UI backgrounds, menu portrait, bell sound and render resolution updated.";
    }
}
